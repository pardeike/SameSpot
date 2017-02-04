using Verse;
using Harmony;
using RimWorld;
using System.Linq;
using Harmony.ILCopying;
using System;

namespace SameSpot
{
	[StaticConstructorOnStartup]
	class Main
	{
		static Main()
		{
			var harmony = HarmonyInstance.Create("net.pardeike.rimworld.mod.camera+");
			var processor = new HarmonyProcessor(Priority.Normal, new string[0], new string[0]);
			processor.AddILProcessor(new MethodReplacer(
				AccessTools.Method(typeof(PawnDestinationManager), "DestinationIsReserved", new Type[] { typeof(IntVec3), typeof(Pawn) }),
				AccessTools.Method(typeof(BestOrderedGotoDestNearPatcher), "DestinationIsReserved")
			));
			processor.AddILProcessor(new MethodReplacer(
				AccessTools.Method(typeof(GenGrid), "Standable"),
				AccessTools.Method(typeof(BestOrderedGotoDestNearPatcher), "Standable")
			));

			var predicateClass = typeof(RCellFinder).GetNestedTypes(AccessTools.all)
				.FirstOrDefault(t => t.FullName.Contains("BestOrderedGotoDestNear"));
			var original = predicateClass.GetMethods(AccessTools.all).FirstOrDefault(m => m.ReturnType == typeof(bool));
			harmony.Patch(original, null, null, processor);
		}
	}

	public static class BestOrderedGotoDestNearPatcher
	{
		public static bool DestinationIsReserved(this PawnDestinationManager instance, IntVec3 c, Pawn searcher)
		{
			return false;
		}

		public static bool Standable(this IntVec3 c, Map map)
		{
			return true;
		}
	}
}