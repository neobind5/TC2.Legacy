
namespace TC2.Base.Components
{
	public static partial class HopperEdit
	{
		[Flags]
		public enum Flags: uint
		{
			None = 0,
			Pickup_Any = 1 << 0
		}
		[IComponent.Data(Net.SendType.Reliable, region_only: true)]
		public partial struct Data(): IComponent
		{
			public HopperEdit.Flags flags;

			[Editor.Picker.Position(relative: true, mark_modified: true)]
			public Vector2 offset;
			public Sound.Handle sound_load;
		}

#if SERVER
		[ISystem.VeryLateUpdate(ISystem.Mode.Single, ISystem.Scope.Region, interval: 0.12f)]
		public static void OnUpdate(ISystem.Info info, Entity entity, ref Region.Data region,
		[Source.Owned] ref Body.Data body, [Source.Owned] in Transform.Data transform,
		[Source.Owned] ref HopperEdit.Data hopper_edit,
		[Source.Owned, Pair.Component<HopperEdit.Data>] ref Inventory8.Data inventory)
		{
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

						if (dot >= 0.95f || hopper_edit.flags.HasFlag(HopperEdit.Flags.Pickup_Any))
						{

							//var dot = Vector2.Dot(normal, axis);
							//App.WriteLine(axis);

							//App.WriteLine(arbiter.GetNormal());

							ref var resource = ref ent_resource.GetComponent<Resource.Data>();
							if (!resource.IsNull())
							{
								if (Resource.Insert(ref inventory, ref resource, resource.quantity, null))
								{
									Sound.Play(ref region, hopper_edit.sound_load, transform.position);
									ent_resource.Delete();
									//arbiter.SetIgnored();
								}

								break;
							}
						}
						//App.WriteLine($"{arbiter.GetEntity()}");
					}
				}
			}
		}
#endif
	}
}
