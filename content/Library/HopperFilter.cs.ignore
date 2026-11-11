
namespace TC2.Base.Components
{
	public static partial class HopperFilter
	{
		[IComponent.Data(Net.SendType.Reliable, region_only: true)]
		public partial struct Data(): IComponent
		{
			[Editor.Picker.Position(relative: true, mark_modified: true)]
			public Vector2 offset;
			public Sound.Handle sound_load;
			public float drop_cooldown;
			[Editor.Picker.Position(relative: true, mark_modified: true)]
			public Vector2 drop_offset = new Vector2(0f,0f);
			public Sound.Handle drop_sound_load;
			[Save.Ignore, Net.Ignore] public float next_drop_time;
		}

#if SERVER
		[ISystem.VeryLateUpdate(ISystem.Mode.Single, ISystem.Scope.Region, interval: 0.12f)]
		public static void OnUpdate(ISystem.Info info, Entity entity, ref Region.Data region,
		[Source.Owned] ref Body.Data body, [Source.Owned] in Transform.Data transform,
		[Source.Owned] ref HopperFilter.Data hopper_edit,
		[Source.Owned, Pair.Component<HopperFilter.Data>] ref Inventory8.Data inventory)
		{
			inventory.TryGetHandle(out var h_inventory);
			var inventory_span = h_inventory.GetReadOnlySpan();
			if (body.HasArbiters())
			{
				var axis = transform.GetDirection().GetPerpendicular(float.IsNegative(transform.scale.GetParity()));
				//region.DrawDebugDir(transform.position, axis * 10, Color32BGRA.Green);

				foreach (var arbiter in body.GetArbiters())
				{
					//if (arbiter.GetParent().id == 0ul)

					//if (arbiter.GetState() == Body.Arbiter.State.Begin && arbiter.GetLayer().HasAny(Physics.Layer.Resource))
					if (arbiter.GetLayer().HasAny(Physics.Layer.Resource))
					{
						var ent_resource = arbiter.GetEntity();
						var normal = arbiter.GetNormal();
						var dot = Vector2.Dot(normal, axis);

						if (dot >= 0.95f)
						{

							//var dot = Vector2.Dot(normal, axis);
							//App.WriteLine(axis);

							//App.WriteLine(arbiter.GetNormal());

							ref var resource = ref ent_resource.GetComponent<Resource.Data>();
							if (!resource.IsNull())
							{
								bool allowed = false;
								for (var l = 0; l < inventory_span.Length; l++)
								{
									var resource_inv = inventory_span[l];
									if (!resource_inv.IsNull())
									{
										ref var material = ref resource_inv.material.GetData();
										if (resource_inv.material == resource.material)
										{
											allowed = true;
											break;
										}
									}
								}
								if (allowed)
								{
									if (Resource.Insert(ref inventory, ref resource, resource.quantity, null))
									{
										Sound.Play(ref region, hopper_edit.sound_load, transform.position);
										arbiter.SetIgnored();
										ent_resource.Delete();
									}
									inventory.Sync();
									return;
								}
							}
						}
						//App.WriteLine($"{arbiter.GetEntity()}");
					}
				}
			}
			var time = info.WorldTime;
			if (time < hopper_edit.next_drop_time)
			{
				return;
			}
			hopper_edit.next_drop_time += hopper_edit.drop_cooldown;
			for (var l = 0; l < inventory_span.Length; l++)
			{
				var resource_inv = inventory_span[l];
				if (!resource_inv.IsNull())
				{
					ref var material = ref resource_inv.material.GetData();
					if (resource_inv.quantity > 1)
					{
						
						var pos = body.GetPosition();
						var amount = resource_inv.quantity - 1;
						inventory.Withdraw(ref resource_inv, ref amount);
						Resource.Spawn
						(
							region: ref region,
							material: resource_inv.material,
							world_position: pos + hopper_edit.drop_offset,
							amount: (resource_inv.quantity - 1)/2,
							max_distance: 2.5f,
							flags: Resource.SpawnFlags.Merge,
							velocity: body.GetVelocity(),
							angular_velocity: body.GetAngularVelocity()
						);
					}
				}
			}
			inventory.Sync();
		}
#endif
	}
}
