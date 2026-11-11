using System.Globalization;
using System.Numerics;

namespace TC2.Base.Components
{
	public static partial class InserterFilter
	{
        [IComponent.Data(Net.SendType.Unreliable, IComponent.Scope.Region), IComponent.With<ItemFilter.Data>]
		public partial struct Data(): IComponent
		{
            [Flags]
			public enum Flags: uint
			{
				None = 0,
				Disable_Edits = 1 << 0,
			}
            [Save.TrimEmpty]
			public InserterFilter.Data.Flags flags;

			[Editor.Picker.Position(relative: true, mark_modified: true)]
			public Vector2 offset = new Vector2(0f,0f);
			public Sound.Handle sound_drop;

            [Editor.Picker.Radius(mark_modified: true)]
            public float search_radius = 0.35f;
        }

        [ISystem.VeryLateUpdate(ISystem.Mode.Single, ISystem.Scope.Region, interval: 0.20f), HasTag("wrecked", false, Source.Modifier.Owned)]
		public static void OnUpdate
		(
			ISystem.Info info,
			ref Region.Data region,
			ref XorRandom random,
			Entity entity,
			[Source.Owned] ref Body.Data body,
			[Source.Owned] in Transform.Data transform,
			[Source.Owned] ref InserterFilter.Data inserter,
            [Source.Owned] ref Faction.Data faction,
            [Source.Owned] ref ItemFilter.Data filter,
		    [Source.Any] ref Storage.Data storage
		)
		{
            if (filter.flags.HasFlag(ItemFilter.Data.Flags.Is_Disabled)) return;
            //bool swap_sides = inserter.flags.HasFlag(InserterFilter.Data.Flags.Swap_Sides);
            Vector2 pos = body.GetPosition();
            FixedArray64<OverlapResult> buffer;
            #if SERVER
            Span<OverlapResult> hits = FixedArray.CreateSpan64NoInit(out buffer);
            if (storage.inv_storage.TryGetHandle(out Inventory.Handle h_inventory))
            {
                if (region.TryOverlapPointAll(pos + inserter.offset, inserter.search_radius, ref hits, 0uL, Physics.Layer.Resource, Physics.Layer.None, Physics.Layer.None, Physics.QueryFlag.Dynamic | Physics.QueryFlag.Static, Physics.Layer.None))
                {
                    hits.SortByDistance();
                    foreach (OverlapResult hit in hits)
                    {
                        // Entity validation
                        Entity target_entity = hit.entity;
                        if (!target_entity.IsValid() || !target_entity.IsAlive() || target_entity == entity)
                        {
                            continue;
                        }
                        //App.WriteLine("collide");
                        // Resource Validation
                        ref var resource = ref target_entity.GetComponent<Resource.Data>();
                        if (resource.IsNull()) continue;
                        if (!filter.IsResourceValid(resource)) continue;
                        if (resource.quantity <= 0) continue;
                        Resource.Insert(h_inventory, ref resource, resource.quantity, null);
                        if (resource.quantity <= 0) target_entity.Delete();
                    }
                }
            }
            #endif
        }

        public struct EditRPC: Net.IRPC<InserterFilter.Data>
		{
			public InserterFilter.Data.Flags flags;

#if SERVER
			public void Invoke(Net.IRPC.Context rpc, ref InserterFilter.Data data)
			{
				bool sync = true;

                data.flags = flags;

				if (sync)
				{
					data.Sync(rpc.entity);
				}
			}
#endif
		}

#if CLIENT
		public struct InserterFilterGUI: IGUICommand
		{
            //public Interactable.Data interactable;
			public Faction.Data faction;
			public Entity entity;
			public InserterFilter.Data inserter;
			public Vector2 position;
			public ItemFilter.Data filter;

			public void Draw()
			{
				ref var region = ref entity.GetRegion();
				float radius = inserter.search_radius;
                float scale = region.AsCommon().GetWorldToCanvasScale();

				AABB rect_canvas = GUI.GetCanvasRect().Pad(new Vec4f(16));

                Vector2 c_pos = region.AsCommon().WorldToCanvas(position + inserter.offset);
                c_pos = rect_canvas.ClipPoint(c_pos);

                GUI.DrawCircle(c_pos, radius * scale, new Color32BGRA(4294967040u));
				GUI.DrawCircleFilled(c_pos, radius * scale, new Color32BGRA(4294967040u).WithAlphaMult(0.33f));

                using (GUI.Window window = GUI.Window.Interaction("Funnel"u8, entity))
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
                        if (GUI.DrawIconButton("Disable"u8, is_disabled ? new Sprite(GUI.tex_icons_widget, 16, 16, 0, 3) : new Sprite(GUI.tex_icons_widget, 16, 16, 1, 3), scale: 2, size: new Vector2(96, 48), enabled: valid_faction))
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
                        if (GUI.DrawIconButton("Blacklist"u8, new Sprite(GUI.tex_icons_widget, 16, 16, 10, 3), scale: 2, size: new Vector2(96, 48), color_icon: is_blacklist?Color32BGRA.White.WithColorMult(0.25f):Color32BGRA.White, enabled: valid_faction))
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
                            using (GUI.ID<InserterFilter.EditRPC, Keyboard.Key>.Push(j))
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
			[Source.Owned] ref InserterFilter.Data inserter,
            [Source.Owned] ref Faction.Data faction,
            [Source.Owned] ref ItemFilter.Data filter
		)
		{
            if (interactable.IsActive() && !inserter.flags.HasFlag(InserterFilter.Data.Flags.Disable_Edits))
            {
                InserterFilterGUI gui = new InserterFilterGUI()
                {
                    //interactable = interactable,
                    faction = faction,
                    entity = entity,
                    inserter = inserter,
                    position = body.GetPosition(),
                    filter = filter
                };

                gui.Submit();
            }
		}
#endif
    }
}