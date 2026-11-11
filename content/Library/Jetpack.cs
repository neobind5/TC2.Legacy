
namespace TC2.Base.Components
{
	public static partial class Jetpack
	{

		[IComponent.Data(Net.SendType.Reliable, IComponent.Scope.Region)]
		public partial struct Data(): IComponent
		{
			[Flags]
			public enum Flags: uint
			{
				None = 0
			}

			public float strength = 5.60f;
			public Jetpack.Data.Flags flags;
			public Jetpack.Data.Flags flags_editable;
			public float particle_size_smoke = 1.00f;
			public float particle_size_boost = 1.00f;
			public float particle_speed_boost = 1.00f;
			public float emit_cooldown = 0.4f;
			[Save.Ignore, Net.Ignore] public float next_smoke = 0.00f;
			public float cooldown = 1.00f;
			public Sound.Handle sound_on_cooldown;
			public Sound.Handle sound_on_boost;
			public float duration = 0.6f;
			[Editor.Picker.Position(true, true)]
			public Vector2 offset;
			[Editor.Picker.Position(relative: true, mark_modified: true)]
			public Vector2 dir = new Vector2(0f,0f);
			[Save.Ignore, Net.Ignore] public float last_activation;
		}
		public static readonly Texture.Handle texture_smoke = "BiggerSmoke_Light";
		public static readonly Texture.Handle texture_explosion = "Explosion";
		[ISystem.Update.A(ISystem.Mode.Single, ISystem.Scope.Region), HasTag("wrecked", false, Source.Modifier.Owned)]
		public static void OnUpdate(ref Region.Data region, ref XorRandom random, ISystem.Info info, Entity entity,
		[Source.Owned] in Transform.Data transform, [Source.Owned] in Control.Data control, [Source.Owned] ref Body.Data body,
		[Source.Owned] ref Jetpack.Data jetpack)
		{
			float time = info.WorldTime;
			if (time == 0)
			{
				return;
			}
			// Return if on cooldown
			if (time <= jetpack.last_activation + jetpack.duration)
			{
				Vector2 force = jetpack.dir * jetpack.strength;
				body.AddForce(force);
				float lifetime = random.NextFloatRange(0.75f, 1.10f);
				#if CLIENT
				if (time >= jetpack.next_smoke)
				{
					jetpack.next_smoke = time + jetpack.emit_cooldown;
					Particle.Spawn(ref region, new Particle.Data()
					{
						texture = texture_explosion,
						lifetime = lifetime,
						pos = transform.LocalToWorld(jetpack.offset) + random.NextVector2(0.50f),
						color_a = ColorBGRA.White,
						color_b = ColorBGRA.White,
						scale = jetpack.particle_size_smoke,
						fps = (byte)(9 / lifetime),
						frame_count = 9,
						frame_count_total = 9,
						growth = random.NextFloatRange(0.50f, 1.10f),
						angular_velocity = random.NextFloatRange(-0.50f, 0.50f),
						rotation = random.NextFloat(MathF.PI),
						vel = -force * jetpack.particle_speed_boost,
						drag = 0.20f,
						lit = 1.00f,
					});
				}
				#endif
			}
			if (time <= jetpack.last_activation + jetpack.cooldown)
			{
				if (time >= jetpack.next_smoke)
				{
					jetpack.next_smoke = time + jetpack.emit_cooldown;
#if CLIENT
					Particle.Spawn(ref region, new Particle.Data()
					{
						texture = texture_smoke,
						lifetime = random.NextFloatRange(0.75f, 1.10f),
						pos = transform.LocalToWorld(jetpack.offset) + random.NextVector2(0.50f),
						vel = (new Vector2(0, -1) * random.NextFloatRange(0.50f, 1.50f)) + random.NextVector2(1.00f),
						fps = 16,
						frame_count = 64,
						frame_count_total = 64,
						frame_offset = random.NextByteRange(0, 64),
						scale = jetpack.particle_size_smoke,
						rotation = random.NextFloat(10.00f),
						angular_velocity = random.NextFloat(0.50f),
						growth = 0.30f,
						color_a = new Color32BGRA(170, 65, 60, 70),
						color_b = new Color32BGRA(0, 30, 30, 35),
					});
					Sound.Play(ref region, jetpack.sound_on_cooldown, transform.position);
#endif
				}
				return;
			}
			if (control.mouse.GetKeyDown(Mouse.Key.Right))
			{
				jetpack.dir = (control.mouse.position - transform.position).GetNormalized(out float len);
				jetpack.last_activation = time;
				Sound.Play(ref region, jetpack.sound_on_boost, transform.position);
			}
		}
	}
}