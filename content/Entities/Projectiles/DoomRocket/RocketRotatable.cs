
namespace TC2.Base.Components
{
	public static partial class RocketRotatable
	{
		public static readonly Texture.Handle texture_smoke = "BiggerSmoke_Light";

		[IComponent.Data(Net.SendType.Unreliable, IComponent.Scope.Region)]
		public partial struct Data(): IComponent
		{
			public float mass = 1.00f;
			public float force;
			public bool flip = false;
			public float x = 1;
			public float y = 1;
			public float fuel_time = 1.00f;
			public float smoke_amount = 1.00f;
			public float delay;
			public Vector2 velocity;

			[Net.Ignore, Save.Ignore, Asset.Ignore] public float smoke_accumulator = 0.00f;
		}

		[ISystem.LateUpdate(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void UpdateBody(ISystem.Info info, Entity entity, [Source.Owned] ref RocketRotatable.Data rocket_rotatable, 
		[Source.Owned] ref Body.Data body, [Source.Owned] ref Transform.Data transform)
		{
			if (rocket_rotatable.delay <= 0.00f)
			{
				if (rocket_rotatable.fuel_time > 0.00f)
				{
					var dir = transform.GetDirection();
					dir.X *= rocket_rotatable.x;
					dir.Y *= rocket_rotatable.y;
					if (rocket_rotatable.flip)
					{
						dir = new Vector2(dir.Y,dir.X);
					}
					body.AddForce(dir * (rocket_rotatable.force));
				}

				rocket_rotatable.fuel_time = Maths.Max(rocket_rotatable.fuel_time - App.fixed_update_interval_s, 0.00f);
			}
			else
			{
				rocket_rotatable.delay -= info.DeltaTime;
			}
		}

		[ISystem.LateUpdate(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void UpdateProjectile(ISystem.Info info, Entity entity, [Source.Owned] ref RocketRotatable.Data rocket_rotatable, 
		[Source.Owned] ref Projectile.Data projectile, [Source.Owned] ref Transform.Data transform)
		{
			if (rocket_rotatable.delay <= 0.00f)
			{
				if (rocket_rotatable.fuel_time > 0.00f)
				{
					//var dir = projectile.velocity.GetNormalized(out var vel);
					//if (projectile.rotation != 0.00f)
					//{
					//	dir = dir.RotateByRad(-projectile.rotation);
					//}

					var dir = transform.GetDirection();
					var step = dir * ((rocket_rotatable.force / rocket_rotatable.mass) * App.fixed_update_interval_s);

					projectile.velocity += step;
					rocket_rotatable.fuel_time = Maths.Max(rocket_rotatable.fuel_time - App.fixed_update_interval_s, 0.00f);
				}
			}
			else
			{
				rocket_rotatable.delay -= info.DeltaTime;
			}
		}

#if CLIENT
		[ISystem.VeryEarlyUpdate(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void UpdateSmokeBody(ISystem.Info info, ref Region.Data region, ref XorRandom random, Entity entity, 
		[Source.Owned] ref RocketRotatable.Data rocket_rotatable, [Source.Owned] in Body.Data body, [Source.Owned] in Transform.Data transform)
		{
			if (rocket_rotatable.smoke_amount > 0.00f && rocket_rotatable.fuel_time > 0.00f && rocket_rotatable.delay <= 0.00f)
			{
				var modifier = Maths.Clamp(rocket_rotatable.fuel_time, 0.00f, 1.00f);

				rocket_rotatable.smoke_accumulator += rocket_rotatable.smoke_amount;

				while (rocket_rotatable.smoke_accumulator >= 1.00f)
				{
					Particle.Spawn(ref region, new Particle.Data()
					{
						texture = texture_smoke,
						pos = transform.position,
						lifetime = random.NextFloatRange(10.00f, 15.00f),
						fps = random.NextByteRange(1, 3),
						frame_count = 64,
						frame_count_total = 64,
						frame_offset = random.NextByteRange(0, 64),
						scale = random.NextFloatRange(0.10f, 0.15f) * modifier,
						angular_velocity = random.NextFloatRange(-0.70f, 0.70f),
						force = new Vector2(random.NextFloatRange(4.00f, 8.00f), -random.NextFloatRange(0.10f, 0.50f)),
						rotation = random.NextFloat(10.00f),
						growth = random.NextFloatRange(0.10f, 0.20f),
						color_a = new Color32BGRA(140, 220, 220, 220).WithAlphaMult(modifier * random.NextFloatRange(0.70f, 1.00f)),
						color_b = new Color32BGRA(000, 150, 150, 150),
						drag = 0.15f,
						vel = -body.GetVelocity() * 0.25f
					});

					rocket_rotatable.smoke_accumulator -= 1.00f;
				}

				//Particle.Spawn(ref region, particle);
			}
		}

		[ISystem.VeryEarlyUpdate(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void UpdateSmokeProjectile(ISystem.Info info, ref Region.Data region, ref XorRandom random, Entity entity, 
		[Source.Owned] ref RocketRotatable.Data rocket_rotatable, [Source.Owned] in Projectile.Data projectile, [Source.Owned] in Transform.Data transform)
		{
			const bool cull = false;

			if (rocket_rotatable.smoke_amount > 0.00f && rocket_rotatable.fuel_time > 0.00f && rocket_rotatable.delay <= 0.00f && projectile.elapsed >= 0.15f)
			{
				var modifier = Maths.Clamp(rocket_rotatable.fuel_time, 0.00f, 1.00f);

				rocket_rotatable.smoke_accumulator += rocket_rotatable.smoke_amount;

				var dir = projectile.velocity.GetNormalized(out var speed); // transform.GetDirection();
				var is_visible = !cull || Camera.IsVisible(Camera.CullType.Rect3x, transform.position);
				
				//var speed = projectile.velocity.Length();
				// .GetNormalized(out var vel);
				//if (projectile.rotation != 0.00f) dir = dir.RotateByRad(-projectile.rotation);

				while (rocket_rotatable.smoke_accumulator >= 1.00f)
				{
					if (is_visible)
					{
						Particle.Spawn(ref region, new Particle.Data()
						{
							texture = texture_smoke,
							pos = transform.position - (dir * speed * App.fixed_update_interval_s * 1.00f),
							lifetime = random.NextFloatRange(10.00f, 15.00f),
							fps = random.NextByteRange(1, 3),
							frame_count = 64,
							frame_count_total = 64,
							frame_offset = random.NextByteRange(0, 64),
							scale = random.NextFloatRange(0.15f, 0.20f),
							angular_velocity = random.NextFloatRange(-0.70f, 0.70f),
							force = (new Vector2(random.NextFloatRange(4.00f, 8.00f), -random.NextFloatRange(0.10f, 0.50f))) - (dir * 4) + rocket_rotatable.velocity,
							//rotation = random.NextFloat(10.00f),
							growth = random.NextFloatRange(0.10f, 0.20f),
							color_a = new Color32BGRA(140, 220, 220, 220).WithAlphaMult(modifier * random.NextFloatRange(0.70f, 1.00f)),
							color_b = new Color32BGRA(000, 150, 150, 150),
							drag = random.NextFloatRange(0.06f, 0.08f),
							stretch = new Vector2(2, 1),
							face_dir_ratio = 0.59f,
							vel = (-dir * speed * 0.40f) + random.NextUnitVector2Range(0.50f, 1.50f),
						});
					}

					rocket_rotatable.smoke_accumulator -= 1.00f;
				}

				//Particle.Spawn(ref region, particle);
			}
		}

		[ISystem.VeryEarlyUpdate(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void UpdateLight(ISystem.Info info, Entity entity, [Source.Owned] in RocketRotatable.Data rocket_rotatable, 
		[Source.Owned, Pair.Component<RocketRotatable.Data>] ref Light.Data light)
		{
			var modifier = Maths.Clamp(rocket_rotatable.fuel_time * 1.50f, 0.00f, 1.00f);
			light.intensity = modifier;
		}
#endif
	}
}
