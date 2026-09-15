
namespace TC2.Base.Components
{
	public static partial class Scrapyard
	{
		[Flags]
		public enum Flags: uint
		{
			None = 0,
		}

		[IComponent.Data(Net.SendType.Unreliable, IComponent.Scope.Region)]
		public partial struct Data(): IComponent
		{
			public Scrapyard.Flags flags;

			public float efficiency = 0.35f;
			public float produce_interval = 1.00f;

			public float amount = 1.00f;
			public float amount_extra;

			[Save.Ignore] public IMaterial.Handle h_material_scrap_concrete_cached;
			[Save.Ignore] public IMaterial.Handle h_material_scrap_rubble_cached;
			[Save.Ignore] public IMaterial.Handle h_material_scrap_mixed_cached;
			[Save.Ignore] public IMaterial.Handle h_material_scrap_ferrous_cached;
			[Save.Ignore] public IMaterial.Handle h_material_scrap_machine_cached;
			[Save.Ignore] public IMaterial.Handle h_material_scrap_cloth_cached;
			[Save.Ignore] public IMaterial.Handle h_material_scrap_smirgrafit_dusty_cached;
			[Save.Ignore] public IMaterial.Handle h_material_scrap_arcane_cached;
			[Save.Ignore] public ILocation.Handle h_location_cached;
			[Save.Ignore] public float amount_multiplier_cached;

			[Save.Ignore, Net.Ignore] public float t_next_produce;
		}

		[ISystem.Modified(ISystem.Mode.Single, ISystem.Scope.Region), HasTag("initialized", true, Source.Modifier.Owned)]
		public static void OnModified(ISystem.Info info, ref Region.Data region, Entity entity,
		[Source.Owned] ref Scrapyard.Data scrapyard)
		{
			scrapyard.h_location_cached = region.GetLocationHandle();

			ref var location_data = ref scrapyard.h_location_cached.GetData();
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

				scrapyard.amount_multiplier_cached = amount_multiplier_tmp;

				scrapyard.h_material_scrap_concrete_cached = "scrap.concrete";
				scrapyard.h_material_scrap_rubble_cached = "scrap.rubble";
				scrapyard.h_material_scrap_mixed_cached = "scrap.mixed";
				scrapyard.h_material_scrap_ferrous_cached = "scrap.ferrous";
				scrapyard.h_material_scrap_machine_cached = "scrap.machine";
				scrapyard.h_material_scrap_cloth_cached = "scrap.cloth";
				scrapyard.h_material_scrap_smirgrafit_dusty_cached = "scrap.smirgrafit.dusty";
				scrapyard.h_material_scrap_arcane_cached = "scrap.arcane";
			}
		}

		[ISystem.Update.A(ISystem.Mode.Single, ISystem.Scope.Region, interval: 0.779f), HasTag("wrecked", false, Source.Modifier.Owned)]
		public static void OnUpdate(ISystem.Info info, ref Region.Data region, ref XorRandom random, Entity entity,
		[Source.Owned] ref Body.Data body, [Source.Owned] in Transform.Data transform,
		[Source.Owned] ref Scrapyard.Data scrapyard,
		[Source.Owned, Pair.Component<Scrapyard.Data>] ref Inventory8.Data inventory)
		{
			var time = info.WorldTime;
			if (time >= scrapyard.t_next_produce)
			{
				scrapyard.t_next_produce = time + scrapyard.produce_interval;

#if SERVER
				var scrap_concrete = new Resource.Data(scrapyard.h_material_scrap_concrete_cached, random.NextFloatExtra(scrapyard.amount, scrapyard.amount_extra) * scrapyard.amount_multiplier_cached * 7.50f);
				if (inventory.Deposit(ref scrap_concrete, scrap_concrete.quantity))
				{

				}
				var scrap_rubble = new Resource.Data(scrapyard.h_material_scrap_rubble_cached, random.NextFloatExtra(scrapyard.amount, scrapyard.amount_extra) * scrapyard.amount_multiplier_cached * scrapyard.efficiency * 6.25f);
				if (inventory.Deposit(ref scrap_rubble, scrap_rubble.quantity))
				{

				}
				var scrap_mixed = new Resource.Data(scrapyard.h_material_scrap_mixed_cached, random.NextFloatExtra(scrapyard.amount, scrapyard.amount_extra) * scrapyard.amount_multiplier_cached * 5.00f);
				if (inventory.Deposit(ref scrap_mixed, scrap_mixed.quantity))
				{

				}
				var scrap_ferrous = new Resource.Data(scrapyard.h_material_scrap_ferrous_cached, random.NextFloatExtra(scrapyard.amount, scrapyard.amount_extra) * scrapyard.amount_multiplier_cached * 4.85f);
				if (inventory.Deposit(ref scrap_ferrous, scrap_ferrous.quantity))
				{

				}
				var scrap_machine = new Resource.Data(scrapyard.h_material_scrap_machine_cached, random.NextFloatExtra(scrapyard.amount, scrapyard.amount_extra) * scrapyard.amount_multiplier_cached * 3.85f);
				if (inventory.Deposit(ref scrap_machine, scrap_machine.quantity))
				{

				}
				var scrap_cloth = new Resource.Data(scrapyard.h_material_scrap_cloth_cached, random.NextFloatExtra(scrapyard.amount, scrapyard.amount_extra) * scrapyard.amount_multiplier_cached * 3.50f );
				if (inventory.Deposit(ref scrap_cloth, scrap_cloth.quantity))
				{

				}
				var scrap_smirgrafit_dusty = new Resource.Data(scrapyard.h_material_scrap_smirgrafit_dusty_cached, random.NextFloatExtra(scrapyard.amount, scrapyard.amount_extra) * scrapyard.amount_multiplier_cached * 0.50f);
				if (inventory.Deposit(ref scrap_smirgrafit_dusty, scrap_smirgrafit_dusty.quantity))
				{

				}
				var scrap_arcane = new Resource.Data(scrapyard.h_material_scrap_arcane_cached, random.NextFloatExtra(scrapyard.amount, scrapyard.amount_extra) * scrapyard.amount_multiplier_cached * 0.50f);
				if (inventory.Deposit(ref scrap_arcane, scrap_arcane.quantity))
				{

				}
#endif
			}
		}
	}
}