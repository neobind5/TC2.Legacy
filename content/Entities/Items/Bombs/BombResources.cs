namespace TC2.Base.Components
{
    public static partial class BombResources
	{
        // [Flags]
		// public enum Flags: byte
		// {
        //     None = 0,
        //     MinimumStacks = 1<<0
        // }

        public partial struct Item()
		{
			[Flags]
			public enum Flags: byte
			{
				None = 0,
			}

			public IMaterial.Handle material;
			public Chance chance = Chance.Max;
			public Lootable.Item.Flags flags;

            // Min amount of this resource
			public float min;
            // Max amount of this resource
			public float max;
            // Max amount of resource per stack
            public float split_size;
		}

		[IComponent.Data(Net.SendType.Unreliable, IComponent.Scope.Region, sync_table_capacity: 512)]
        public struct Data: IComponent
        {
			[Save.TrimEmpty]
			public FixedArray4<BombResources.Item> items;
            [Save.NewLine]
            public float min_speed = 2.00f;
            public float max_speed = 2.00f;
			public float merge_radius = 2.50f;
			public float spawn_radius = 1.50f;
            public Data()
			{
			}
        }
#if SERVER
        [ISystem.RemoveLast(ISystem.Mode.Single, ISystem.Scope.Region), HasTag("initialized", true, Source.Modifier.Owned)]
		public static void OnRemove(ref Region.Data region, ref XorRandom random,
		[Source.Owned] ref BombResources.Data bomb_resources,
		[Source.Owned] in Health.Data health, [Source.Owned] in Body.Data body)
		{
			//App.WriteLine("drop loot");
			var yield = Constants.Harvestable.global_yield_modifier * Constants.Materials.global_yield_modifier;
			if (yield > 0.01f && health.integrity <= 0.00f)
			{
				ref var items = ref bomb_resources.items;
				var pos = body.GetPosition();

				for (var i = 0; i < items.Length; i++)
				{
					ref var item = ref items[i];
					if (item.material && (item.chance.m_value == 0 || random.NextBool(item.chance)))
					{
						var amount = random.NextFloatRange(item.min, item.max) * yield;

                        if (item.split_size == 0)
                        {
                            item.split_size = amount;
                        }
                        float required_passes = (float)Math.Ceiling(amount/item.split_size);
                        for (var j = 0; j < required_passes; j++)
                        {
                            float amount_pass = amount - j*required_passes;
                            if (amount >= Resource.epsilon)
                            {
                                Resource.Spawn(region: ref region,
                                    material: item.material,
                                    world_position: pos + random.NextUnitVector2Range(bomb_resources.spawn_radius, bomb_resources.spawn_radius),
                                    amount: amount_pass,
                                    max_distance: bomb_resources.merge_radius,
                                    flags: Resource.SpawnFlags.None,
                                    velocity: body.GetVelocity() + random.NextUnitVector2Range(bomb_resources.min_speed, bomb_resources.max_speed),
                                    angular_velocity: body.GetAngularVelocity() + random.NextFloatRange(-2.50f, 2.50f));
                            }
                        }
						item = default;
					}
				}
			}
		}
#endif
    }
}