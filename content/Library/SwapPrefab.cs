namespace TC2.Base.Components
{

    /*
    Swaps Prefabs automatically.
    Documentation:
    Tags:
        none - Nothing
        persist - Original prefab isn't deleted
        no_replacement - Skip generating the new prefab
        ignore_rotation - New prefab's rotation is set to 0 instead of cloning the original's transform

    Data:
        prefab - Typed as string ('prefab.' and '.hjson' are implicit and should not be included)
        offset - Offset at which to place the new prefab. Does not consider rotation, should be used with ignore_rotation.
    */
    public static partial class SwapPrefab
    {
        [Flags]
		public enum Flags: uint
		{
			None = 0,
			Persist = 1 << 0,
            No_Replacement = 1 << 1,
			Ignore_Rotation = 1 << 2
		}

        [IComponent.Data(Net.SendType.Unreliable, IComponent.Scope.Region)]
		public partial struct Data(): IComponent
		{
			public Prefab.Handle prefab;
            public Vector2 offset = new Vector2(0,0);
			public SwapPrefab.Flags flags;
            public bool triggered = false;
        }
#if SERVER
        [ISystem.AddFirst(ISystem.Mode.Single, ISystem.Scope.Region)]
        public static void OnPrefabInit(ref XorRandom random, Entity entity, ref Region.Data region,
        [Source.Owned] in Transform.Data transform, [Source.Owned] ref SwapPrefab.Data swap_prefab, [Source.Owned] in Body.Data body)
        {
            //App.WriteLine("test");
            if (!swap_prefab.triggered)
            {
                if (swap_prefab.flags.HasNone(SwapPrefab.Flags.No_Replacement))
                {
                    region.SpawnPrefab
                    (
                        prefab: swap_prefab.prefab,
                        position: transform.position + swap_prefab.offset,
                        rotation: swap_prefab.flags.HasAny(SwapPrefab.Flags.Ignore_Rotation) ? 0 : transform.rotation,
                        velocity: body.GetVelocity(),
                        faction_id:0
                    );
                }
                if (swap_prefab.flags.HasNone(SwapPrefab.Flags.Persist))
                {
                    entity.Delete();
                }
                swap_prefab.triggered = true;
            }
        }
#endif
    }
}