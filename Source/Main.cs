using Verse;
using Harmony;
using Verse.AI;
using RimWorld;
using System.Linq;

namespace SameSpot
{
	[StaticConstructorOnStartup]
	class Main
	{
		static Main()
		{
			Patcher.PatchAll(typeof(Main).Module);
		}
	}

	[HarmonyPatch]
	public static class Patch
	{
		public static void Prepare()
		{
			// the predicate we want to patch is some odd named inner class of BestOrderedGotoDestNear
			// so we look for it semi-manually
			//
			var predicateClass = typeof(RCellFinder).GetNestedTypes(AccessTools.all)
				.FirstOrDefault(t => t.FullName.Contains("BestOrderedGotoDestNear"));
			if (predicateClass != null)
			{
				var predicate = predicateClass.GetMethods(AccessTools.all).FirstOrDefault(m => m.ReturnType == typeof(bool));
				if (predicate != null)
					PatchedMethod.Patch(predicate, typeof(Patch).GetMethod("Prefix", AccessTools.all), null);
			}
		}

		public static bool Prefix(object instance, ref bool result, ref IntVec3 c)
		{
			var trv = Traverse.Create(instance);
			var map = trv.Field("map").GetValue<Map>();
			var searcher = trv.Field("searcher").GetValue<Pawn>();

			var selector = Find.Selector;
			var singleColonist = (selector.SelectedObjects.Count == 1 && selector.SelectedObjects[0] is Pawn);

			result = (
				// disabling DestinationIsReserved() for single selected colonist allows for multi-colonist-same-spot-cheese orders!
				(singleColonist || !map.pawnDestinationManager.DestinationIsReserved(c, searcher)) &&
				c.Standable(map) &&
				searcher.CanReach(c, PathEndMode.OnCell, Danger.Deadly, false, TraverseMode.ByPawn)
			);

			return false;
		}
	}
}