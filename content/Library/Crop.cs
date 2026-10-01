using System.Numerics;

namespace TC2.Base.Components
{
    public static partial class Crop
    {
        [Flags]
		public enum Flags: uint
		{
			None = 0
		}

        [IComponent.Data(Net.SendType.Unreliable, IComponent.Scope.Region)]
		public partial struct Data(): IComponent
		{
			public Crop.Flags flags;
			public Prefab.Handle prefab;
            public Vector2 offset = new Vector2(0,0);
            public int stages = 1;
            public float time_seconds = 1;
            public float time_extra = 0;
            public float time = 0;
            public bool initialized = false;
        }
#if SERVER
        [ISystem.EarlyUpdate(ISystem.Mode.Single, ISystem.Scope.Region, interval: 0.1f)]
        public static void OnUpdate(ref XorRandom random, Entity entity, ref Region.Data region,
        [Source.Owned] in Transform.Data transform, [Source.Owned] ref Crop.Data crop, [Source.Owned] in Body.Data body)
        {
            if (!crop.initialized)
            {
                crop.time -= random.NextFloatExtra(0, crop.time_extra);
                crop.initialized = true;
            }
            if (crop.time < crop.time_seconds)
            {
                crop.time += 0.1f;
                return;
            }
            region.SpawnPrefab
            (
                prefab: crop.prefab,
                position: transform.position + crop.offset,
                rotation: transform.rotation,
                velocity: body.GetVelocity(),
                faction_id: 0
            );
            entity.Delete();
        }
#endif
    }
}