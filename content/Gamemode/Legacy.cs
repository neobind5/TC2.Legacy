using System.Threading;
using TC2.Base.Components;

namespace TC2.Legacy
{
	public sealed partial class ModInstance: Mod
	{
		protected override void OnRegister(ref ModContext context)
		{
			Constants.Characters.abandon_cooldown = 0f;
			Constants.Characters.allow_switching = false;
			Constants.Characters.allow_custom_characters = false;
			Constants.Characters.character_count_max = 1;
			Constants.Inventories.weight_modifier = 0;
		}

		protected override void OnInitialize(ref ModContext context)
		{
			
		}
	}
}