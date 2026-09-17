
namespace TC2.Base.Components
{
	public static partial class DoomRocketFix
	{
		[IComponent.Data(Net.SendType.Reliable, IComponent.Scope.Region)]
		public partial struct Data(): IComponent
		{
			public float speed_step = 0.50f;
			public float speed;
			public float rot_speed = 0.80f;
		}

		[IComponent.Data(Net.SendType.Unreliable, IComponent.Scope.Region)]
		public partial struct State: IComponent
		{
			[Save.Ignore, Net.Ignore]
			public float current_motor_speed;

			[Save.Ignore, Net.Ignore]
			public float current_motor_force;

			[Save.Ignore, Net.Ignore]
			public float target_motor_speed;
		}

		[ISystem.EarlyUpdate(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void UpdateControls(ISystem.Info info, Entity entity, [Source.Owned] ref Transform.Data transform, 
		[Source.Owned] ref DoomRocketFix.Data doom_rocket_fix, [Source.Owned] ref DoomRocketFix.State doom_rocket_state, [Source.Owned] in Control.Data control)
		{
			var speed = 0.00f;

			if (control.keyboard.GetKey(Keyboard.Key.MoveRight)) speed -= doom_rocket_fix.speed;
			if (control.keyboard.GetKey(Keyboard.Key.MoveLeft)) speed += doom_rocket_fix.speed;

			//if (control.keyboard.GetKeyDown(Keyboard.Key.Q)) doom_rocket.gear = Maths.Clamp(doom_rocket.gear - 1, 0, 4);
			//if (control.keyboard.GetKeyDown(Keyboard.Key.E)) doom_rocket.gear = Maths.Clamp(doom_rocket.gear + 1, 0, 4);

			var ratio = 1.00f; // * MathF.CopySign(1.00f, transform.scale.X); // + (doom_rocket.gear * doom_rocket.gear_mod);
			if (control.keyboard.GetKey(Keyboard.Key.LeftShift)) speed *= 2.00f;

			doom_rocket_state.target_motor_speed = (speed != 0.00f ? speed : 0.00f) * ratio;
			//doom_rocket_state.target_motor_force = (speed != 0.00f ? doom_rocket.force : doom_rocket.brake) / ratio;
		}

		public static void OnUpdate(ref Region.Data region, ref XorRandom random, ISystem.Info info, Entity entity,
		[Source.Owned] in Transform.Data transform, [Source.Owned] in Control.Data control, [Source.Owned] ref Body.Data body,
		[Source.Owned] ref DoomRocketFix.Data doom_rocket_fix)
		{
			var speed = 0.00f;
			var rot = body.GetRotation();

			if (control.keyboard.GetKey(Keyboard.Key.MoveRight)) speed -= doom_rocket_fix.rot_speed;
			if (control.keyboard.GetKey(Keyboard.Key.MoveLeft)) speed += doom_rocket_fix.rot_speed;

			if (speed != 0)
			{
				body.AddTorque(speed);
			}
		}

#if CLIENT
		public struct DoomRocketFixGUI: IGUICommand
		{
			public Entity ent_doom_rocket;

			public Transform.Data transform;

			public void Draw()
			{
				using (var window = GUI.Window.Interaction("Rocket"u8, this.ent_doom_rocket))
				{
					this.StoreCurrentWindowTypeID(order: -100);
					if (window.show)
					{
						ref var player = ref Client.GetPlayer();
						ref var region = ref Client.GetRegion();

						using (GUI.Group.New(size: new Vector2(GUI.RmX, GUI.RmY)))
						{

						}
					}
				}
			}
		}

		[ISystem.EarlyGUI(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void OnGUI(Entity entity, [Source.Owned] in Transform.Data transform, 
		[Source.Owned] in DoomRocketFix.Data doom_rocket, [Source.Owned] in Interactable.Data interactable)
		{
			if (interactable.IsActive())
			{
				var gui = new DoomRocketFixGUI()
				{
					ent_doom_rocket = entity,

					transform = transform
				};
				gui.Submit();
			}
		}
#endif
	}
}
