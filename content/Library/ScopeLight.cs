namespace TC2.Base.Components
{
    public static partial class ScopeLight
    {
        [Flags]
		public enum Flags: uint
		{
			None = 0
		}

        [IComponent.Data(Net.SendType.Unreliable, IComponent.Scope.Region)]
		public partial struct Data(): IComponent
		{
            public float intensity = 0; // Light intensity when held, 0 when pocketed or not held.
            //public bool is_held = false;
			public ScopeLight.Flags flags;
        }

        [ISystem.VeryLateUpdate(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void Update
        (
            ISystem.Info info,
            Entity entity,
            ref XorRandom random,
            [Source.Owned] ref ScopeLight.Data scope_light,
            [Source.Owned] in Transform.Data transform, 
            [Source.Owned] in Control.Data control,
            [Source.Owned] ref Body.Data body,
            [Source.Owned, Pair.Component<ScopeLight.Data>] ref Light.Data light
        )
		{
            //App.WriteLine("test");
            //#if SERVER
			var ent_parent = entity.GetParent(Relation.Type.Child);
            if (ent_parent.IsValid())
            {
                //App.WriteLine("parent found");
            }
            light.intensity = ent_parent.IsValid() ? scope_light.intensity : 0;
            //#endif
		}
//#if CLIENT
/*
        [ISystem.GUI(ISystem.Mode.Single, ISystem.Scope.Region), HasTag("local", true, Source.Modifier.Parent)]
		public static void OnGUI(ISystem.Info info, Entity entity,
		[Source.Parent] in Interactor.Data interactor, [Source.Owned] ref ScopeLight.Data scope_light, [Source.Owned] in Transform.Data transform, [Source.Owned] in Control.Data control)
		{
			scope_light.is_held = true;
		}

		[ISystem.GUI(ISystem.Mode.Single, ISystem.Scope.Region), HasTag("local", true, Source.Modifier.Stored)]
		public static void OnGUI_Stored(ISystem.Info info, Entity entity,
		[Source.Owned] ref ScopeLight.Data scope_light, [Source.Owned] in Transform.Data transform, [Source.Owned] in Control.Data control)
		{
			scope_light.is_held = false;
		}
*/
//#endif
    }
}