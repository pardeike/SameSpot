using HarmonyLib;
using Verse;

namespace SameSpot
{
	[StaticConstructorOnStartup]
	static class ModCompatibility
	{
		static ModCompatibility()
		{
			LongEventHandler.QueueLongEvent(() =>
			{
				var m_Standable = SymbolExtensions.GetMethodInfo(() => GenGrid.Standable(default, null));
				var m_CustomStandable = SymbolExtensions.GetMethodInfo(() => Main.CustomStandable(default, null));
				var info = Harmony.GetPatchInfo(m_Standable);
				info.Prefixes.Do(patch =>
				{
					var prefix = patch.PatchMethod;
					if (prefix.DeclaringType.Namespace != typeof(SameSpotMod).Namespace)
					{
						Log.Message($"Applying postfix {prefix} to {m_CustomStandable}");
						_ = SameSpotMod.harmony.Patch(m_CustomStandable, prefix: new HarmonyMethod(prefix, patch.priority));
					}
				});
				info.Postfixes.Do(patch =>
				{
					var postfix = patch.PatchMethod;
					if (postfix.DeclaringType.Namespace != typeof(SameSpotMod).Namespace)
					{
						Log.Message($"Applying postfix {postfix} to {m_CustomStandable}");
						_ = SameSpotMod.harmony.Patch(m_CustomStandable, postfix: new HarmonyMethod(postfix, patch.priority));
					}
				});
			},
			"SameSpot", true, null, false);
		}
	}
}
