
namespace TC2.Base.Components
{
	public static partial class ResourceGenerator8
	{
		[Flags]
		public enum Flags: uint
		{
			None = 0,
		}

		public partial struct Item()
		{
			[Flags]
			public enum Flags: byte
			{
				None = 0,
			}

			public IMaterial.Handle material;
			public Chance chance = Chance.Max;

            // Min amount of this resource
			public float min;
            // Max amount of this resource
			public float max;
		}

		[IComponent.Data(Net.SendType.Unreliable, IComponent.Scope.Region)]
		public partial struct Data(): IComponent
		{
			public ResourceGenerator8.Flags flags;

			public float efficiency = 0.35f;
			public float produce_interval = 3.00f;

			[Save.TrimEmpty]
			public FixedArray4<ResourceGenerator8.Item> items;
			[Save.Ignore] public ILocation.Handle h_location_cached;
			[Save.Ignore] public float amount_multiplier_cached;

			[Save.Ignore, Net.Ignore] public float t_next_produce;
		}

		[ISystem.Modified(ISystem.Mode.Single, ISystem.Scope.Region), HasTag("initialized", true, Source.Modifier.Owned)]
		public static void OnModified(ISystem.Info info, ref Region.Data region, Entity entity,
		[Source.Owned] ref ResourceGenerator8.Data resource_generator)
		{
			resource_generator.h_location_cached = region.GetLocationHandle();

			ref var location_data = ref resource_generator.h_location_cached.GetData();
			if (location_data.IsNotNull())
			{
				var amount_multiplier_tmp = 1.00f;

				if (location_data.geography.HasAny(IMap.Geography.Damp))
				{
					amount_multiplier_tmp += location_data.geography.GetCount(IMap.Geography.Swamps | IMap.Geography.Lakes | IMap.Geography.Coastal, 0.14f);
					amount_multiplier_tmp *= 1.23f;
					amount_multiplier_tmp += location_data.geography.GetCount(IMap.Geography.Warm | IMap.Geography.Hot, 0.09f);
				}
				else if (location_data.geography.HasAny(IMap.Geography.Dry))
				{
					amount_multiplier_tmp += location_data.geography.GetCount(IMap.Geography.Swamps | IMap.Geography.Lakes | IMap.Geography.Coastal, 0.05f);
					amount_multiplier_tmp *= 0.88f;
					amount_multiplier_tmp -= location_data.geography.GetCount(IMap.Geography.Hot | IMap.Geography.Windy, 0.07f);
				}
				amount_multiplier_tmp -= location_data.geography.GetCount(IMap.Geography.Mountains | IMap.Geography.Urban, 0.06f);
				amount_multiplier_tmp.ClampMinRef(0.12f);

				resource_generator.amount_multiplier_cached = amount_multiplier_tmp;
			}
		}

		[ISystem.Update.A(ISystem.Mode.Single, ISystem.Scope.Region, interval: 0.779f), HasTag("wrecked", false, Source.Modifier.Owned)]
		public static void OnUpdate(ISystem.Info info, ref Region.Data region, ref XorRandom random, Entity entity,
		[Source.Owned] ref Body.Data body, [Source.Owned] in Transform.Data transform,
		[Source.Owned] ref ResourceGenerator8.Data resource_generator,
		[Source.Owned, Pair.Component<ResourceGenerator8.Data>] ref Inventory8.Data inventory)
		{
			var time = info.WorldTime;
			if (time >= resource_generator.t_next_produce)
			{
				resource_generator.t_next_produce = time + resource_generator.produce_interval;

#if SERVER
				int item_len = resource_generator.items.Length;
				for (int i=0;i<item_len;i++)
				{
					var resource = new Resource.Data(resource_generator.items[i].material, random.NextFloatExtra(resource_generator.items[i].max, resource_generator.items[i].min) * resource_generator.amount_multiplier_cached * resource_generator.efficiency * resource_generator.produce_interval);
					if (inventory.Deposit(ref resource, resource.quantity))
					{

					}
				}
#endif
			}
		}
	}
}