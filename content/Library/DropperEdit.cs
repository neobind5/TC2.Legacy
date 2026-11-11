
namespace TC2.Base.Components
{
	public static partial class DropperEdit
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
			public DropperEdit.Flags flags;

			[Editor.Picker.Position(relative: true, mark_modified: true)]
			public Vector2 offset;
			public Sound.Handle sound_load;
            public float amount_max = 90;
			public float cooldown = 1;
			[Asset.Ignore, Save.Ignore, Net.Ignore] public float next_time;
		}

#if SERVER
		[ISystem.Update.A(ISystem.Mode.Single, ISystem.Scope.Region), HasTag("wrecked", false, Source.Modifier.Owned)]
		public static void OnUpdate(ISystem.Info info, Entity entity, ref Region.Data region,
		[Source.Owned] ref Body.Data body, [Source.Owned] in Transform.Data transform,
		[Source.Owned] ref DropperEdit.Data dropper_edit,
		[Source.Any] ref Storage.Data storage)
		{
			float time = info.WorldTime;
			if (time <= dropper_edit.next_time)
			{
				return;
			}
			else
			{
				dropper_edit.next_time = time + dropper_edit.cooldown;
			}
			if (storage.inv_storage.TryGetHandle(out Inventory.Handle h_inventory))
            {
				float total = 0;
				Span<Resource.Data> resources = h_inventory.GetSpan();
				for (int i=0; i<resources.Length;i++)
				{
					Resource.Data resource = resources[i];
					float allocated = dropper_edit.amount_max - total;
					float num = Math.Min(resource.quantity, allocated);
					total += num;

					Resource.Spawn
					(
						region: ref region,
						material: resource.material,
						world_position: body.GetPosition() + dropper_edit.offset,
						amount: num,
						max_distance: 1.5f,
						flags: Resource.SpawnFlags.Merge,
						velocity: body.GetVelocity(),
						angular_velocity: body.GetAngularVelocity()
					);
					h_inventory.Withdraw(resource: ref resource, ref num);
				}
			}
		}
#endif
	}
}
