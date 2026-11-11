using System.Globalization;
using System.Numerics;

namespace TC2.Base.Components
{
	public static partial class Extractor
	{
        [IComponent.Data(Net.SendType.Unreliable, IComponent.Scope.Region), IComponent.With<ItemFilter.Data>]
		public partial struct Data(): IComponent
		{
            [Flags]
			public enum Flags: uint
			{
				None = 0
			}
            [Save.TrimEmpty]
			public Extractor.Data.Flags flags;
            public float amount_max = 150;
            public float cooldown = 2;
            public float eject_velocity = 5;

            [Editor.Picker.Radius(mark_modified: true)]
            public float search_radius_min = 2.5f;
            public float search_radius_max = 10;

			[Editor.Picker.Position(relative: true, mark_modified: true)]
			public Vector2 drop_offset = new Vector2(0f,0f);
			public Sound.Handle sound_drop;
            public float search_radius = 10;
			[Asset.Ignore, Save.Ignore, Net.Ignore] public float next_time;
        }

        [ISystem.Update.A(ISystem.Mode.Single, ISystem.Scope.Region), HasTag("wrecked", false, Source.Modifier.Owned)]
		public static void OnUpdate
		(
			ISystem.Info info,
			ref Region.Data region,
			ref XorRandom random,
			Entity entity,
			[Source.Owned] ref Body.Data body,
			[Source.Owned] in Transform.Data transform,
			[Source.Owned] ref Extractor.Data extractor,
            [Source.Owned] ref Faction.Data faction,
            [Source.Owned] ref ItemFilter.Data filter
		)
		{
            if (filter.flags.HasFlag(ItemFilter.Data.Flags.Is_Disabled)) return;
			float time = info.WorldTime;
			if (time <= extractor.next_time)
			{
				return;
			}
			else
			{
				extractor.next_time = time + extractor.cooldown;
			}
            Vector2 pos = body.GetPosition();
            //if (storage.inv_storage.TryGetHandle(out Inventory.Handle h_inventory))
            {
                FixedArray64<OverlapResult> buffer;
                Span<OverlapResult> hits = FixedArray.CreateSpan64NoInit(out buffer);
                
                #if SERVER
                if (region.TryOverlapPointAll(pos, extractor.search_radius, ref hits, 0uL, Physics.Layer.Storage | Physics.Layer.Building | Physics.Layer.Shipment, Physics.Layer.Organic | Physics.Layer.Item, Physics.Layer.None, Physics.QueryFlag.Dynamic | Physics.QueryFlag.Static, Physics.Layer.None))
                {
                    hits.SortByDistance();
                    float total = 0;
                    foreach (OverlapResult hit in hits)
                    {
                        Entity target_entity = hit.entity;
                        if (!target_entity.IsValid() || !target_entity.IsAlive() || target_entity == entity)
                        {
                            continue;
                        }
                        IFaction.Handle target_faction = target_entity.GetFactionHandle();
                        Inventory.Handle.List inventories = target_entity.GetInventories();
                        if (target_faction == faction.id || target_faction == 0)
                        {
                            foreach (Inventory.Handle inventory in inventories)
                            {
                                Span<Resource.Data> resources = inventory.GetSpan();
                                foreach (Resource.Data resource in resources)
                                {
                                    if (!filter.IsResourceValid(resource)) continue;
                                    if (resource.quantity == 0) continue;
                                    //App.WriteLine(amount);
                                    float allocated = extractor.amount_max - total;
                                    float num = Math.Min(resource.quantity, allocated);
                                    total += num;
                                    Resource.Data resource_clone = new Resource.Data(resource.material, num);
                                    if (resource.quantity > 0)
                                    {
                                        Resource.Spawn
                                        (
                                            region: ref region,
                                            material: resource.material,
                                            world_position: pos + extractor.drop_offset.RotateByRad(transform.rotation),
                                            amount: num,
                                            max_distance: 1.5f,
                                            flags: Resource.SpawnFlags.Merge,
                                            velocity: body.GetVelocity() + new Vector2(0,1).RotateByRad(transform.rotation) * extractor.eject_velocity,
                                            angular_velocity: body.GetAngularVelocity()
                                        );
                                    }
                                    inventory.Withdraw(resource: ref resource_clone, ref num);
                                    //h_inventory.Sync();
                                    inventory.Sync();
                                    if (total >= 50)
                                    {
                                        return;
                                    }
                                }
                            }
                        }
                    }
                }
                #endif
            }
        }

        public struct EditRPC: Net.IRPC<Extractor.Data>
		{
			public float search_radius;

#if SERVER
			public void Invoke(Net.IRPC.Context rpc, ref Extractor.Data data)
			{
				var sync = true;

                data.search_radius = Math.Clamp(search_radius, data.search_radius_min, data.search_radius_max);

				if (sync)
				{
					data.Sync(rpc.entity);
				}
			}
#endif
		}

#if CLIENT
		public struct ExtractorGUI: IGUICommand
		{
            //public Interactable.Data interactable;
			public Faction.Data faction;
			public Entity entity;
			public Extractor.Data extractor;
			public Vector2 position;
			public ItemFilter.Data filter;

			public void Draw()
			{
				ref var region = ref entity.GetRegion();
				float radius = extractor.search_radius;
                float scale = region.AsCommon().GetWorldToCanvasScale();

				AABB rect_canvas = GUI.GetCanvasRect().Pad(new Vec4f(16));

                Vector2 c_pos = region.AsCommon().WorldToCanvas(position);
                c_pos = rect_canvas.ClipPoint(c_pos);

				GUI.DrawCircle(c_pos, radius * scale, new Color32BGRA(4294967040u));
				GUI.DrawCircleFilled(c_pos, radius * scale, new Color32BGRA(4294967040u).WithAlphaMult(0.33f));

                using (GUI.Window window = GUI.Window.Interaction("Extractor"u8, entity))
                {
                    // Get Entity
                    Entity controlled_entity = Client.GetControlledEntity();
                    if (!controlled_entity.IsAlive()) return;
                    // Check faction validity
                    IFaction.Handle interactor_faction = controlled_entity.GetFactionHandle();
                    ref NPC.Data npc = ref controlled_entity.GetComponent<NPC.Data>();
                    if (!npc.IsNotNull()) return;
                    bool valid_faction = faction.id == interactor_faction;

                    this.StoreCurrentWindowTypeID();
					if (window.show)
                    {
                        
                        bool is_disabled = filter.flags.HasFlag(ItemFilter.Data.Flags.Is_Disabled);
                        if (GUI.DrawIconButton("Disable"u8, is_disabled ? new Sprite(GUI.tex_icons_widget, 16, 16, 0, 3) : new Sprite(GUI.tex_icons_widget, 16, 16, 1, 3), scale: 2, size: new Vector2(48, 48), enabled: valid_faction))
                        {
                            ItemFilter.EditRPC filter_rpc = new ItemFilter.EditRPC();
                            filter.flags.SetFlag(ItemFilter.Data.Flags.Is_Disabled, !is_disabled);
                            filter_rpc.flags = filter.flags;
                            filter_rpc.materials = filter.materials;
                            filter_rpc.Send(entity);
                        }
                        GUI.SameLine(spacing: 0);
                        if (GUI.DrawHoverTooltip
                        (
                            """
                            Toggle between enabling and disabling this machine.
                            
                            Disabling a machine stops it from functioning.
                            """u8
                        ))
                        GUI.SameLine(spacing: 0);
                        bool is_blacklist = filter.flags.HasFlag(ItemFilter.Data.Flags.Is_Blacklist);
                        if (GUI.DrawIconButton("Blacklist"u8, new Sprite(GUI.tex_icons_widget, 16, 16, 10, 3), scale: 2, size: new Vector2(48, 48), color_icon: is_blacklist?Color32BGRA.White.WithColorMult(0.25f):Color32BGRA.White, enabled: valid_faction))
                        {
                            ItemFilter.EditRPC filter_rpc = new ItemFilter.EditRPC();
                            filter.flags.SetFlag(ItemFilter.Data.Flags.Is_Blacklist, !is_blacklist);
                            filter_rpc.flags = filter.flags;
                            filter_rpc.materials = filter.materials;
                            filter_rpc.Send(entity);
                        }{}
                        if (GUI.DrawHoverTooltip
                        (
                            """
                            Toggle between whitelist and blacklist filtering.
                            
                            Whitelisting allows anything that fulfills any filter criteria.

                            Blacklisting disallows anything that fulfills any criteria.
                            """u8
                        )){}
                        GUI.SameLine(spacing: 0);
                        if (GUI.SliderFloat("Extraction Radius"u8, ref extractor.search_radius, min: extractor.search_radius_min, max: extractor.search_radius_max, size: new Vector2(GUI.RmX, 48), vertical: false, enabled: valid_faction))
                        {
                            EditRPC rpc = new Extractor.EditRPC();
                            //ItemFilter.EditRPC filter_rpc = new ItemFilter.EditRPC();
                            rpc.search_radius = extractor.search_radius;
                            //filter_rpc.materials = filter.materials;
                            rpc.Send(entity);
                            //filter_rpc.Send(entity);
                        }
                        if (GUI.DrawHoverTooltip
                        (
                            """
                            Configure the radius at which this machine grabs items.

                            Extractors will only pull items from valid storages in radius.

                            Closer storages get priority.
                            """u8
                        )){}
                        bool valid = true;
                        ref Entity held = ref npc.ent_pickup_target;
                        if (!(held.IsNotNull() && held.IsAlive()))
                        {
                            valid = false;
                        }
                        ref Resource.Data held_resource = ref held.GetComponent<Resource.Data>();
                        if (!held_resource.IsNotNull())
                        {
                            valid = false;
                        }
                        //GUI.Scrollbox scrollbox = GUI.Scrollbox.New("scroll"u8, size: new Vector2(GUI.RmX, GUI.RmY));
                        for (int i = 0; i < this.filter.materials.Length; i++)
						{
                            ref var material = ref this.filter.materials[i];
                            int j = i;
                            Sprite icon = material.GetIcon();
                            using (GUI.ID<Extractor.EditRPC, Keyboard.Key>.Push(j))
                            using (GUI.Group group = GUI.Group.New(size: new Vector2(48, 48), padding: new Vector2()))
                            {
                                if (GUI.DrawIconButton(material.GetName(), sprite: material.IsValid()?icon: new Sprite(GUI.tex_icons_widget, 16, 16, 6, 3), scale: 2, size: new Vector2(48, 48), enabled: valid_faction && (valid || material.IsValid())))
                                {
                                    ItemFilter.EditRPC filter_rpc = new ItemFilter.EditRPC();
                                    filter.materials[i] = valid?held_resource.material:IMaterial.Handle.Default;
                                    filter_rpc.flags = filter.flags;
                                    filter_rpc.materials = filter.materials;
                                    filter_rpc.Send(entity);
                                }
                                if (!valid && GUI.DrawHoverTooltip
                                (
                                    """
                                    Hold a resource and click a slot to insert it into the filter.

                                    Clicking an occupied filter slot with empty hands removes it from the filter.
                                    """u8
                                )){}
                                {
                                    
                                }
                                //GUI.SameLine(spacing: 4);
                                //if (GUI.DrawIconButton(
                                //    "clone"u8,
                                //    sprite: new Sprite(GUI.tex_icons_widget, 16, 16, 6, 3),
                                //    scale: 2,
                                //    size: new Vector2(48, 48),
                                //    color_icon: Color32BGRA.Orange
                                //))
                                //{
                                // // REMOVED
                                //}
                                //if (!material.IsValid()) continue;
                                //GUI.SameLine(spacing: 0);
                                //if (GUI.DrawIconButton("clear"u8, sprite: new Sprite(GUI.tex_icons_widget, 16, 16, 3, 7), scale: 2, size: new Vector2(48, 48), color_icon: Color32BGRA.Red,enabled: material.IsValid() && valid_faction))
                                //{
                                //    if (npc.IsNotNull() && faction.id == interactor_faction)
                                //    {
                                //        ItemFilter.EditRPC filter_rpc = new ItemFilter.EditRPC();
                                //        filter.materials[i] = IMaterial.Handle.Default;
                                //        filter_rpc.flags = filter.flags;
                                //        filter_rpc.materials = filter.materials;
                                //        filter_rpc.Send(entity);
                                //    }
                                //}
                            }
                            if (i%4!=3)
                            {
                                GUI.SameLine(spacing: 0);
                            }
                        }
                        //scrollbox.Dispose();
                    }
                }
            }
		}

		[ISystem.GUI(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void OnGUI
        (
			Entity entity,
		    [Source.Owned] in Interactable.Data interactable,
			[Source.Owned] ref Body.Data body,
			[Source.Owned] ref Extractor.Data extractor,
            [Source.Owned] ref Faction.Data faction,
            [Source.Owned] ref ItemFilter.Data filter
		)
		{
            if (interactable.IsActive())
            {
                ExtractorGUI gui = new ExtractorGUI()
                {
                    //interactable = interactable,
                    faction = faction,
                    entity = entity,
                    extractor = extractor,
                    position = body.GetPosition(),
                    filter = filter
                };

                gui.Submit();
            }
		}
#endif
    }
}