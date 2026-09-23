namespace TC2.Base.Components
{
	public static partial class FreeformWallRenderer
    {
        public struct Data: IComponent
        {
            public string texture;
            public Vec2f[] points;
        }

		[IComponent.Data(Net.SendType.Unreliable, IComponent.Scope.Region)]
        public partial struct Test
        {
            public static void TestDebug(ref FreeformWallRenderer.Data test_debug)
            {
                App.WriteLine("test");
            }
            [ISystem.Event<Interactable.InteractEvent>(ISystem.Mode.Single, ISystem.Scope.Region)]
            public static void OnInteract(ISystem.Info info, Entity entity, ref Region.Data region, ref XorRandom random, [Source.Owned] ref Interactable.InteractEvent data,
            [Source.Owned] in Transform.Data transform, [Source.Owned] ref Interactable.Data interactable, [Source.Owned] ref FreeformWallRenderer.Data test_debug)
            {
                TestDebug(ref test_debug);
            }
        }
    }
}