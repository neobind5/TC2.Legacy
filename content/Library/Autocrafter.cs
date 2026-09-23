
using System.Numerics;

namespace TC2.Base.Components
{
	public static partial class Autocrafter
	{
		[Flags]
		public enum Flags: uint
		{
			None = 0,
			Force_Drop = 1 << 0
		}

		public partial struct Item()
		{
			public IMaterial.Handle material;
			public float count;
		}
		public partial struct Recipe()
		{
			[Flags]
			public enum Flags: ushort
			{
				None = 0,
				Force_Drop = 1 << 0
			}
			public Flags flags;
			public FixedArray16<Autocrafter.Item> requirements;
			public FixedArray16<Autocrafter.Item> products;
		}

		[IComponent.Data(Net.SendType.Unreliable, IComponent.Scope.Region)]
		public partial struct Data(): IComponent
		{
			public Autocrafter.Flags flags;

			[Save.TrimEmpty]
			public FixedArray32<Autocrafter.Recipe> recipes; // Hard limit of 32 recipes to make management easier
			public float cooldown;
			[Editor.Picker.Position(relative: true, mark_modified: true)]
			public Vector2 drop_offset = new Vector2(0f,0f);
			public Sound.Handle sound_load;
			[Save.Ignore, Net.Ignore] public float next_time;
		}
		
#if SERVER
		[ISystem.Update.A(ISystem.Mode.Single, ISystem.Scope.Region), HasTag("wrecked", false, Source.Modifier.Owned)]
		public static void OnUpdate
		(
			ISystem.Info info,
			ref Region.Data region,
			ref XorRandom random,
			Entity entity,
			[Source.Owned] ref Body.Data body,
			[Source.Owned] in Transform.Data transform,
			[Source.Owned] ref Autocrafter.Data autocrafter,
			[Source.Any] ref Storage.Data storage
		)
		{
			var time = info.WorldTime;
			if (time <= autocrafter.next_time)
			{
				return;
			}
			else
			{
				autocrafter.next_time = time + autocrafter.cooldown;
			}

			bool produced = false;

			if (storage.inv_storage.TryGetHandle(out var h_inventory))
			{
				var inventory_span = h_inventory.GetReadOnlySpan();
				for (var i = 0; i < autocrafter.recipes.Length; i++)
				{
					Recipe recipe = autocrafter.recipes[i];
					bool valid = true; // Assume recipe is possible until proven otherwise
					for (var j = 0; j < recipe.requirements.Length; j++)
					{
						Item requirement = recipe.requirements[j];
						if (requirement.material.IsNull() || requirement.count <= 0)
						{
							if (j==0)
							{
								valid = false;
							}
							break;
						}
						float count_total = 0f;
						for (var l = 0; l < inventory_span.Length; l++)
						{
							var resource = inventory_span[l];

							ref var material = ref resource.material.GetData();
							if (resource.material == requirement.material)
							{
								count_total += resource.quantity;
								if (count_total >= requirement.count)
								{
									break;
								}
							}
						}
						if (count_total + 0.0001f < requirement.count)
						{
							valid = false;
							break;
						}
					}
					if (valid)
					{
						produced = true;
						var pos = body.GetPosition();
						for (var x = 0; x < recipe.requirements.Length; x++)
						{
							ref Item requirement = ref recipe.requirements[x];
							var resource = new Resource.Data(requirement.material, requirement.count);
							h_inventory.Withdraw(ref resource, ref resource.quantity);
						}
						for (var x = 0; x < recipe.products.Length; x++)
						{
							ref Item requirement = ref recipe.products[x];
							var resource = new Resource.Data(requirement.material, requirement.count);
							if (!autocrafter.flags.HasFlag(Autocrafter.Flags.Force_Drop) && !recipe.flags.HasFlag(Recipe.Flags.Force_Drop))
							{
								h_inventory.Deposit(ref resource);
							}
							if (resource.quantity > 0)
							{
								Resource.Spawn
								(
									region: ref region,
                                    material: requirement.material,
                                    world_position: pos + autocrafter.drop_offset,
                                    amount: resource.quantity,
                                    max_distance: 2.5f,
                                    flags: Resource.SpawnFlags.Merge,
                                    velocity: body.GetVelocity(),
                                    angular_velocity: body.GetAngularVelocity()
								);
							}
						}
						h_inventory.Sync();
					}
				}
				if (produced)
				{
					Sound.Play(ref region, autocrafter.sound_load, transform.position);
				}
			}
		}
#endif
	}
}