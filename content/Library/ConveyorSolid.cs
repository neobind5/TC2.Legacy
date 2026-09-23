
using System.Numerics;

namespace TC2.Base.Components
{
	public static partial class ConveyorSolid
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
			[Save.Ignore, Net.Ignore] public float next_time;
		}

//#if SERVER
		[ISystem.VeryLateUpdate(ISystem.Mode.Single, ISystem.Scope.Region, interval: 0.12f)]
		public static void OnUpdate(ISystem.Info info, Entity entity, ref Region.Data region,
		[Source.Owned] ref Body.Data body, [Source.Owned] in Transform.Data transform,
		[Source.Owned] ref ConveyorSolid.Data conveyor_solid)
		{
			//bool played_sound = false;
			var time = info.WorldTime;
			if (time > conveyor_solid.next_time)
			{
				Sound.Play(ref region, conveyor_solid.sound_load, transform.position, conveyor_solid.volume, conveyor_solid.pitch, conveyor_solid.size, conveyor_solid.priority, conveyor_solid.dist_multiplier);
				conveyor_solid.next_time = time + conveyor_solid.sound_cooldown;
			}
			if (body.HasArbiters())
			{
				var axis = transform.GetDirection().GetPerpendicular(float.IsNegative(transform.scale.GetParity()));
				var dir = transform.GetDirection();

				foreach (var arbiter in body.GetArbiters())
				{
					var normal = arbiter.GetNormal();
					var dot = Vector2.Dot(normal, axis);

					if (dot >= 0.65f)
					{

						//var dot = Vector2.Dot(normal, axis);
						//App.WriteLine(axis);

						//App.WriteLine(arbiter.GetNormal());
						
						//if (!played_sound)
						//{
						//	played_sound = true;
						//}
						// Set velocity in direction of conveyor
						Vector2 arbiter_vel = arbiter.GetBody().GetVelocity();
						arbiter.GetBody().SetVelocity(arbiter_vel*conveyor_solid.dampening + dir*conveyor_solid.strength);
					}
				}
			}
		}
//#endif
	}
}