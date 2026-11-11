
namespace TC2.Base.Components
{
	public static partial class Grinder
	{
		[Flags]
		public enum Flags: uint
		{
			None = 0,
			Pickup_Any = 1 << 0,
			No_Entity_Damage = 1 << 1,
			No_Item_Pickup = 1 << 2,
			Deactive = 1 << 2,
		}

		public partial struct Item()
		{
			public IMaterial.Handle material;
			public float amount;
		}

		public partial struct Conversion()
		{
			public IMaterial.Handle material; // Input resource
			public FixedArray8<Grinder.Item> products; // Items produced by this
			public float efficiency; // Percentage of item consumed during this
			public float mult = 1; // Extra mult
			public bool enabled = true; // If this conversion is enabled
		}


		[IComponent.Data(Net.SendType.Reliable, region_only: true)]
		public partial struct Data(): IComponent
		{
			public Grinder.Flags flags;

			[Editor.Picker.Position(relative: true, mark_modified: true)]
			public Vector2 drop_offset;

			[Editor.Picker.Position(relative: true, mark_modified: true)]
			public Vector2 offset;
			[Editor.Picker.Position(relative: true, mark_modified: true)]
			public Vector2 box_top_right;
			//public Sound.Handle sound_load_hit;
			public Sound.Handle sound_load_crush;
			public Sound.Handle sound_load_depot;
			public FixedArray16<Grinder.Conversion> conversions; // Items produced by this
			public float damage = 50;
			public float cooldown = 1;
			public float next_hit;
		}

#if SERVER
		[ISystem.VeryLateUpdate(ISystem.Mode.Single, ISystem.Scope.Region, interval: 0.12f)]
		public static void OnUpdate(ISystem.Info info, ref XorRandom random, Entity entity, ref Region.Data region,
		[Source.Owned] ref Body.Data body, [Source.Owned] in Transform.Data transform,
		[Source.Owned] ref Grinder.Data grinder,
		[Source.Owned, Pair.Component<Grinder.Data>] ref Inventory16.Data inventory)
		{
			if (body.HasArbiters())
			{
				var pos = body.GetPosition();
				var box_pos = grinder.offset + pos;
				var bottom_left = box_pos - grinder.box_top_right;
				var top_right = box_pos + grinder.box_top_right;
				var axis = transform.GetDirection().GetPerpendicular(float.IsNegative(transform.scale.GetParity()));
				//region.DrawDebugDir(transform.position, axis * 10, Color32BGRA.Green);

				foreach (var arbiter in body.GetArbiters())
				{
					var other = arbiter.GetEntity();
					var other_pos = arbiter.GetContactsPosition();
					bool is_inside = bottom_left.X <= other_pos.X && other_pos.X <= top_right.X && bottom_left.Y >= other_pos.Y && other_pos.Y >= top_right.Y;

					//if (arbiter.GetState() == Body.Arbiter.State.Begin && arbiter.GetLayer().HasAny(Physics.Layer.Resource))
					//if (arbiter.GetLayer().HasAny(Physics.Layer.Resource))
					//var normal = arbiter.GetNormal();
					//var dot = Vector2.Dot(normal, axis);
					if (is_inside || grinder.flags.HasFlag(Grinder.Flags.Pickup_Any))
					{
						if (arbiter.GetLayer().HasAny(Physics.Layer.Resource))
						{
							ref var resource = ref other.GetComponent<Resource.Data>();
							if (!resource.IsNull())
							{
								bool do_convert = false;
								foreach (Grinder.Conversion conversion in grinder.conversions)
								{
									if (resource.material == conversion.material)
									{
										do_convert = true;
										var conv_resource = new Resource.Data(resource.material, resource.quantity * (1-conversion.efficiency));
										if (Resource.Insert(ref inventory, ref conv_resource, conv_resource.quantity, null))
										{
										}
										if (conv_resource.quantity > 0)
										{
											Resource.Spawn
											(
												region: ref region,
												material: resource.material,
												world_position: pos + grinder.drop_offset,
												amount: conv_resource.quantity,
												max_distance: 2.5f,
												flags: Resource.SpawnFlags.Merge,
												velocity: body.GetVelocity(),
												angular_velocity: body.GetAngularVelocity()
											);
										}
										foreach (Grinder.Item item in conversion.products)
										{
											var product_resource = new Resource.Data(item.material, (resource.quantity * (conversion.efficiency)) * item.amount * conversion.mult);
											if (Resource.Insert(ref inventory, ref product_resource, product_resource.quantity, null))
											{
											}
											if (product_resource.quantity > 0)
											{
												Resource.Spawn
												(
													region: ref region,
													material: item.material,
													world_position: pos + grinder.drop_offset,
													amount: product_resource.quantity,
													max_distance: 2.5f,
													flags: Resource.SpawnFlags.Merge,
													velocity: body.GetVelocity() + new Vector2(random.NextFloatRange(0f, 16f) - 8, -random.NextFloatRange(5f, 16f)),
													angular_velocity: body.GetAngularVelocity()
												);
											}
										}
										Sound.Play(ref region, grinder.sound_load_crush, transform.position);
										break;
									}
								}
								if (!do_convert)
								{
									if (Resource.Insert(ref inventory, ref resource, resource.quantity, null))
									{
									}
									if (resource.quantity > 0)
									{
										Resource.Spawn
										(
											region: ref region,
											material: resource.material,
											world_position: pos + grinder.drop_offset,
											amount: resource.quantity,
											max_distance: 2.5f,
											flags: Resource.SpawnFlags.Merge,
											velocity: body.GetVelocity() + new Vector2(random.NextFloatRange(0f, 16f) - 8, -random.NextFloatRange(5f, 16f)),
											angular_velocity: body.GetAngularVelocity()
										);
									}
									Sound.Play(ref region, grinder.sound_load_depot, transform.position);
								}
								other.Delete();
							}
						}
						if (!grinder.flags.HasFlag(Grinder.Flags.No_Entity_Damage))
						{
							/*
							EXAMPLE:
							Damage.Hit(ent_attacker: entity, ent_owner: entity, ent_target: ent_target,
									position: pos_contact, velocity: -arbiter.GetNormal(), normal: arbiter.GetNormal(),
									damage_integrity: damage, damage_durability: damage, damage_terrain: damage,
									target_material_type: material, damage_type: fire.damage_type_contact,
									yield: 1.00f, size: fire.radius * 0.50f, impulse: 0.00f, 
									flags: Damage.Flags.No_Loot_Pickup | Damage.Flags.No_Loot_Drop | Damage.Flags.No_Loot_Notification);
							*/
							Damage.Hit
							(
								ent_attacker: entity,
								ent_owner: entity,
								ent_target: arbiter.GetEntity(),
								position: other_pos,
								velocity: new Vector2(random.NextFloatRange(0f, 8f) - 4, -random.NextFloatRange(2f, 8f)),
								normal: arbiter.GetNormal(),
								damage_integrity: grinder.damage,
								damage_durability: grinder.damage,
								damage_terrain: grinder.damage,
								target_material_type: arbiter.GetMaterial(),
								damage_type: Damage.Type.Handsaw,
								yield: 1.00f,
								size: 0.500f,
								impulse: grinder.next_hit
							);
							grinder.next_hit = info.WorldTime + grinder.cooldown;
						}
					}
				}
			}
		}
#endif
	}
}
