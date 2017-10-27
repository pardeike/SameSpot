using Verse;
using Harmony;
using RimWorld;
using System.Linq;
using System;
using System.Reflection;
using UnityEngine;
using System.Collections.Generic;

namespace SameSpot
{
	[StaticConstructorOnStartup]
	static class SameSpotMod
	{
		static SameSpotMod()
		{
			var harmony = HarmonyInstance.Create("net.pardeike.rimworld.mod.samespot");
			harmony.PatchAll(Assembly.GetExecutingAssembly());
		}

		public static bool IsReserved(this PawnDestinationReservationManager instance, IntVec3 loc)
		{
			if (Find.Selector.SelectedObjects.Count() == 1) return false;
			if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) return false;
			return instance.IsReserved(loc);
		}

		public static bool CanReserve(this PawnDestinationReservationManager instance, IntVec3 c, Pawn searcher)
		{
			if (Find.Selector.SelectedObjects.Count() == 1) return true;
			if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) return true;
			return instance.CanReserve(c, searcher);
		}

		public static bool Standable(this IntVec3 c, Map map)
		{
			return true;
		}
	}

	[HarmonyPatch]
	static class RCellFinder_BestOrderedGotoDestNear_Patch
	{
		static MethodBase TargetMethod()
		{
			var predicateClass = typeof(RCellFinder).GetNestedTypes(AccessTools.all)
				.FirstOrDefault(t => t.FullName.Contains("BestOrderedGotoDestNear"));
			return predicateClass.GetMethods(AccessTools.all).FirstOrDefault(m => m.ReturnType == typeof(bool));
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			return instructions

				.MethodReplacer(
					AccessTools.Method(typeof(PawnDestinationReservationManager), nameof(PawnDestinationReservationManager.CanReserve), new Type[] { typeof(IntVec3), typeof(Pawn) }),
					AccessTools.Method(typeof(SameSpotMod), nameof(SameSpotMod.CanReserve))
				)

				.MethodReplacer(
					AccessTools.Method(typeof(GenGrid), nameof(GenGrid.Standable)),
					AccessTools.Method(typeof(SameSpotMod), nameof(SameSpotMod.Standable))
				);
		}
	}

	[HarmonyPatch(typeof(JoyGiver_InteractBuildingInteractionCell))]
	[HarmonyPatch("TryGivePlayJob")]
	static class JoyGiver_InteractBuildingInteractionCell_TryGivePlayJob_Patch
	{
		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			return instructions

				.MethodReplacer(
					AccessTools.Method(typeof(PawnDestinationReservationManager), nameof(PawnDestinationReservationManager.IsReserved), new Type[] { typeof(IntVec3) }),
					AccessTools.Method(typeof(SameSpotMod), nameof(SameSpotMod.IsReserved))
				)

				.MethodReplacer(
					AccessTools.Method(typeof(GenGrid), nameof(GenGrid.Standable)),
					AccessTools.Method(typeof(SameSpotMod), nameof(SameSpotMod.Standable))
				);
		}
	}
}