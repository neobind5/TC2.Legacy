
namespace TC2.Base.Components
{
	public static partial class FancyMine
	{
		[Flags]
		public enum Flags: uint
		{
			None = 0,
		}

		[IComponent.Data(Net.SendType.Unreliable, IComponent.Scope.Region)]
		public partial struct Data(): IComponent
		{
			public FancyMine.Flags flags;

			public float efficiency = 0.35f;
			public float produce_interval = 3.00f;

			public float amount = 1.00f;
			public float amount_extra;

			[Save.Ignore] public IMaterial.Handle h_material_niklajzner_cached;
			[Save.Ignore] public IMaterial.Handle h_material_black_coal_cached;
			[Save.Ignore] public IMaterial.Handle h_material_copper_cached;
			[Save.Ignore] public IMaterial.Handle h_material_himlkarzenus_cached;
			[Save.Ignore] public IMaterial.Handle h_material_gold_cached;
			[Save.Ignore] public IMaterial.Handle h_material_saltpeter_cached;
			[Save.Ignore] public ILocation.Handle h_location_cached;
			[Save.Ignore] public float amount_multiplier_cached;

			[Save.Ignore, Net.Ignore] public float t_next_produce;
		}

		[ISystem.Modified(ISystem.Mode.Single, ISystem.Scope.Region), HasTag("initialized", true, Source.Modifier.Owned)]
		public static void OnModified(ISystem.Info info, ref Region.Data region, Entity entity,
		[Source.Owned] ref FancyMine.Data fancymine)
		{
			fancymine.h_location_cached = region.GetLocationHandle();

			ref var location_data = ref fancymine.h_location_cached.GetData();
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

				fancymine.amount_multiplier_cached = amount_multiplier_tmp;

				fancymine.h_material_niklajzner_cached = "niklajzner";
				fancymine.h_material_black_coal_cached = "black_coal";
				fancymine.h_material_copper_cached = "copper";
				fancymine.h_material_himlkarzenus_cached = "himlkarzenus";
				fancymine.h_material_gold_cached = "gold";
				fancymine.h_material_saltpeter_cached = "saltpeter";
			}
		}

		[ISystem.Update.A(ISystem.Mode.Single, ISystem.Scope.Region, interval: 0.779f), HasTag("wrecked", false, Source.Modifier.Owned)]
		public static void OnUpdate(ISystem.Info info, ref Region.Data region, ref XorRandom random, Entity entity,
		[Source.Owned] ref Body.Data body, [Source.Owned] in Transform.Data transform,
		[Source.Owned] ref FancyMine.Data fancymine,
		[Source.Owned, Pair.Component<FancyMine.Data>] ref Inventory8.Data inventory)
		{
			var time = info.WorldTime;
			if (time >= fancymine.t_next_produce)
			{
				fancymine.t_next_produce = time + fancymine.produce_interval;

#if SERVER
				var niklajzner = new Resource.Data(fancymine.h_material_niklajzner_cached, random.NextFloatExtra(fancymine.amount, fancymine.amount_extra) * fancymine.amount_multiplier_cached);
				if (inventory.Deposit(ref niklajzner, niklajzner.quantity))
				{

				}
				var black_coal = new Resource.Data(fancymine.h_material_black_coal_cached, random.NextFloatExtra(fancymine.amount, fancymine.amount_extra) * fancymine.amount_multiplier_cached);
				if (inventory.Deposit(ref black_coal, black_coal.quantity))
				{

				}
				var copper = new Resource.Data(fancymine.h_material_copper_cached, random.NextFloatExtra(fancymine.amount, fancymine.amount_extra) * fancymine.amount_multiplier_cached * fancymine.efficiency);
				if (inventory.Deposit(ref copper, copper.quantity))
				{

				}
				var himlkarzenus = new Resource.Data(fancymine.h_material_himlkarzenus_cached, random.NextFloatExtra(fancymine.amount, fancymine.amount_extra) * fancymine.amount_multiplier_cached * fancymine.efficiency);
				if (inventory.Deposit(ref himlkarzenus, himlkarzenus.quantity))
				{

				}
				var gold = new Resource.Data(fancymine.h_material_gold_cached, random.NextFloatExtra(fancymine.amount, fancymine.amount_extra) * fancymine.amount_multiplier_cached * fancymine.efficiency * fancymine.efficiency * fancymine.efficiency);
				if (inventory.Deposit(ref gold, gold.quantity))
				{

				}
				var saltpeter = new Resource.Data(fancymine.h_material_saltpeter_cached, random.NextFloatExtra(fancymine.amount, fancymine.amount_extra) * fancymine.amount_multiplier_cached * fancymine.efficiency);
				if (inventory.Deposit(ref saltpeter, saltpeter.quantity))
				{

				}
#endif
			}
		}
	}
}