
using System.Numerics;

namespace TC2.Base.Components
{
	public static partial class RenderRotateA
	{
		[Flags]
		public enum Flags: uint
		{
			None = 0,
			Paused = 1 << 0
		}

		[IComponent.Data(Net.SendType.Unreliable, IComponent.Scope.Region)]
		public partial struct Data(): IComponent
		{
			//[Editor.Picker.Position(relative: true)] public Vector2 offset;
			public RenderRotateA.Flags flags;
			public float speed;
			public float rot;
		}
		
//#if SERVER
		[ISystem.Update.A(ISystem.Mode.Single, ISystem.Scope.Region), HasTag("wrecked", false, Source.Modifier.Owned)]
		public static void OnUpdate
		(
			ISystem.Info info,
			ref Region.Data region,
			ref XorRandom random,
			Entity entity,
			[Source.Owned] ref Body.Data body,
			[Source.Owned] in Transform.Data transform,
			[Source.Owned] in RenderRotateA.Data data,
			[Source.Owned, Pair.Component<RenderRotateA.Data>] ref Animated.Renderer.Data renderer
		)
		{
			renderer.rotation = renderer.rotation + data.speed * info.DeltaTime; // % MathF.Tau;
			//renderer.offset = data.offset;
		}
//#endif
	}
}