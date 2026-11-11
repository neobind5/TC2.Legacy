using System.Globalization;
using System.Numerics;

namespace TC2.Base.Components
{
	public static partial class ItemFilter
	{
        [IComponent.Data(Net.SendType.Unreliable, IComponent.Scope.Region)]
		public partial struct Data(): IComponent
		{
            [Flags]
			public enum Flags: uint
			{
				None = 0,
				Is_Blacklist = 1 << 0,
				Is_Disabled = 1 << 1
			}
            [Save.TrimEmpty]
			public ItemFilter.Data.Flags flags;
			public FixedArray16<IMaterial.Handle> materials;
            
            public bool IsResourceValid
            (
                Resource.Data resource
            )
            {
                bool is_blacklist = this.flags.HasFlag(Data.Flags.Is_Blacklist);
                foreach (IMaterial.Handle filter_material in this.materials)
                {
                    if (resource.material == filter_material) return !is_blacklist;
                }
                return is_blacklist;
            }
            public bool IsMaterialValid
            (
                [Source.Owned] ref ItemFilter.Data filter,
                IMaterial.Handle material
            )
            {
                bool is_blacklist = this.flags.HasFlag(Data.Flags.Is_Blacklist);
                foreach (IMaterial.Handle filter_material in this.materials)
                {
                    if (material == filter_material) return !is_blacklist;
                }
                return is_blacklist;
            }
        }

        public struct EditRPC: Net.IRPC<ItemFilter.Data>
		{
            public ItemFilter.Data.Flags flags;
            public FixedArray16<IMaterial.Handle> materials;

#if SERVER
			public void Invoke(Net.IRPC.Context rpc, ref ItemFilter.Data data)
			{
                data.flags = flags;
                data.materials = materials;
                data.Sync(rpc.entity);
			}
#endif
		}
    }
}