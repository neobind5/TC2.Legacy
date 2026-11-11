
using System.Numerics;

namespace TC2.Base.Components
{
	public static partial class PhysConveyor
	{
		[IComponent.Data(Net.SendType.Reliable, region_only: true)]
		public partial struct Data(): IComponent
		{
			[Editor.Picker.Position(relative: true, mark_modified: true)]
			public Vector2 offset;
			public Sound.Handle sound_load;
			public float strength = 1;
			public float dampening = 0.9f;
			public float volume = 1f;
			public float pitch = 1f;
			public float size = 1f;
			public float priority = 1f;
			public float dist_multiplier = 1f;
			public float sound_cooldown = 0.5f;
			public float max_strength = 1;
			public float fps = 60;
			[Save.Ignore, Net.Ignore] public float next_time;
		}

		public struct EditRPC: Net.IRPC<PhysConveyor.Data>
		{
			public float strength;

#if SERVER
			public void Invoke(Net.IRPC.Context rpc, ref PhysConveyor.Data data)
			{
				var sync = true;

                data.strength = Math.Clamp(strength, -data.max_strength, data.max_strength);

				if (sync)
				{
					data.Sync(rpc.entity);
				}
			}
#endif
		}

		[ISystem.Add(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void OnAdd(ISystem.Info info, Entity entity, ref Region.Data region,
		[Source.Owned] ref Body.Data body,
		[Source.Owned] ref Transform.Data transform,
		[Source.Owned] ref PhysConveyor.Data conveyor,
		[Source.Owned] ref Resizable.Data resizable,
		[Source.Owned] ref Animated.Renderer.Data renderer)
		{
			renderer.offset = renderer.offset/renderer.sprite.count;
			renderer.rect.Z *= renderer.sprite.count;
		}

		[ISystem.VeryLateUpdate(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void OnUpdate(ISystem.Info info, Entity entity, ref Region.Data region,
		[Source.Owned] ref Body.Data body,
		[Source.Owned] ref Transform.Data transform,
		[Source.Owned] ref PhysConveyor.Data conveyor,
		[Source.Owned] ref Resizable.Data resizable,
		[Source.Owned] ref Animated.Renderer.Data renderer)
		{
			bool debug = false;
			/*
			#if CLIENT
				debug = true;
			#endif
			*/

			float strength_scale = conveyor.strength/conveyor.max_strength;
			uint frame = conveyor.strength==0?0:(uint)(info.WorldTime*conveyor.fps*Math.Abs(strength_scale) % renderer.sprite.count);
			float angle = (float)Math.Atan2(resizable.a.Y-resizable.b.Y,resizable.a.X-resizable.b.X);
			if (conveyor.strength < 0)
			{
				frame = renderer.sprite.count - frame;
			}
			renderer.sprite.frame.x = frame;
			resizable.cap_a.frame.x = frame;
			resizable.cap_b.frame.x = frame;
			var time = info.WorldTime;
			if (time > conveyor.next_time)
			{
				Sound.Play(ref region, conveyor.sound_load, transform.position, conveyor.volume, conveyor.pitch, conveyor.size, conveyor.priority, conveyor.dist_multiplier);
				conveyor.next_time = time + conveyor.sound_cooldown;
			}


			if (body.HasArbiters())
			{
				
				var axis = transform.GetDirection().GetPerpendicular(float.IsNegative(transform.scale.GetParity()));
				Body.Arbiter.List arbiters = body.GetArbiters();
				foreach (Body.Arbiter arbiter in arbiters)
				{
					if (arbiter.GetEntity())
					{
						Entity other = arbiter.GetEntity();
						if (other.IsValid() && other.IsAlive() && other.HasComponent<Body.Data>())
						{
							Vector2 normal = arbiter.GetNormal().RotateByRad(angle);
							if (debug) App.WriteLine("Angle:");
							if (debug) App.WriteLine(angle/Math.PI*180);
							if (debug) App.WriteLine("Calc:");
							if (debug) App.WriteLine(Vec2f.RotateByRad(new Vec2f(0,-1), (float)Math.Atan2(normal.Y,normal.X)) * conveyor.strength);
							Vector2 arbiter_vel = arbiter.GetBody().GetVelocity();
							Body.Data other_body = arbiter.GetBody();
							other_body.SetVelocity(arbiter_vel * conveyor.dampening + (Vec2f.RotateByRad(new Vec2f(0,-1), (float)Math.Atan2(normal.Y,normal.X)) * conveyor.strength));
						}
					}
				}
			}
		}


		#if CLIENT
		public struct PhysConveyorGUI: IGUICommand
		{
            //public Interactable.Data interactable;
			public Faction.Data faction;
			public Entity entity;
			public PhysConveyor.Data conveyor;
			public Vector2 position;

			public void Draw()
			{
				using (GUI.Window window = GUI.Window.Interaction("Conveyor"u8, entity))
                {
                    this.StoreCurrentWindowTypeID();
					if (window.show)
                    {
						if (GUI.SliderFloat("Conveyor Strength"u8, ref conveyor.strength, min: -conveyor.max_strength, max: conveyor.max_strength, size: new Vector2(GUI.RmX, 48), vertical: false))
						{
							EditRPC rpc = new PhysConveyor.EditRPC();
							rpc.strength = conveyor.strength;
							rpc.Send(entity);
						}
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
			[Source.Owned] in Transform.Data transform,
			[Source.Owned] ref PhysConveyor.Data conveyor,
            [Source.Owned] ref Faction.Data faction
		)
		{
            if (interactable.IsActive())
            {
                PhysConveyorGUI gui = new PhysConveyorGUI()
                {
                    //interactable = interactable,
                    faction = faction,
                    entity = entity,
                    conveyor = conveyor,
                    position = body.GetPosition(),
                };

                gui.Submit();
            }
		}
		#endif
	}
}