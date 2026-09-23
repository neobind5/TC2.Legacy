
namespace TC2.Base.Components
{
	public static partial class AutoPump
	{
		[Flags]
		public enum Flags: uint
		{
			None = 0,
		}

		[IComponent.Data(Net.SendType.Unreliable, IComponent.Scope.Region)]
		public partial struct Data(): IComponent
		{
			public AutoPump.Flags flags;

			public float efficiency = 0.35f;
			public float produce_interval = 3.00f;

			public float amount = 1.00f;
			public float amount_extra;

			[Save.Ignore] public IMaterial.Handle h_material_water_cached;
			[Save.Ignore] public ILocation.Handle h_location_cached;
			[Save.Ignore] public float amount_multiplier_cached;

			[Save.Ignore, Net.Ignore] public float t_next_produce;
		}

		[ISystem.Modified(ISystem.Mode.Single, ISystem.Scope.Region), HasTag("initialized", true, Source.Modifier.Owned)]
		public static void OnModified(ISystem.Info info, ref Region.Data region, Entity entity,
		[Source.Owned] ref AutoPump.Data autopump)
		{
			autopump.h_location_cached = region.GetLocationHandle();

			ref var location_data = ref autopump.h_location_cached.GetData();
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

				autopump.amount_multiplier_cached = amount_multiplier_tmp;

				//var h_water = IMaterial.Handle.None;
				//if (location_data.statistics.pollution > 0.70f) = autopump.h_material_water_cached = "water.clean";
				autopump.h_material_water_cached = "water";
			}
		}

		[ISystem.Update.A(ISystem.Mode.Single, ISystem.Scope.Region, interval: 0.779f), HasTag("wrecked", false, Source.Modifier.Owned)]
		public static void OnUpdate(ISystem.Info info, ref Region.Data region, ref XorRandom random, Entity entity,
		[Source.Owned] ref Body.Data body, [Source.Owned] in Transform.Data transform,
		[Source.Owned] ref AutoPump.Data autopump,
		[Source.Owned, Pair.Component<AutoPump.Data>] ref Inventory1.Data inventory)
		{
			var time = info.WorldTime;
			if (time >= autopump.t_next_produce)
			{
				autopump.t_next_produce = time + autopump.produce_interval;

#if SERVER
				var resource_water = new Resource.Data(autopump.h_material_water_cached, random.NextFloatExtra(autopump.amount, autopump.amount_extra) * autopump.amount_multiplier_cached * autopump.efficiency * autopump.produce_interval);
				if (inventory.Deposit(ref resource_water, resource_water.quantity))
				{

				}
#endif
			}
		}
	}
}