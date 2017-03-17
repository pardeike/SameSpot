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
	class Main
	{
		static Main()
		{
			var harmony = HarmonyInstance.Create("net.pardeike.rimworld.mod.samespot");
			harmony.PatchAll(Assembly.GetExecutingAssembly());
		}
	}

	[HarmonyPatch]
	static class BestOrderedGotoDestNearPatcher
	{
		static bool DestinationIsReserved(this PawnDestinationManager instance, IntVec3 c, Pawn searcher)
		{
			if (Find.Selector.SelectedObjects.Count() == 1) return false;
			if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) return false;
			return instance.DestinationIsReserved(c, searcher);
		}

		static bool Standable(this IntVec3 c, Map map)
		{
			return true;
		}

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
					AccessTools.Method(typeof(PawnDestinationManager), "DestinationIsReserved", new Type[] { typeof(IntVec3), typeof(Pawn) }),
					AccessTools.Method(typeof(BestOrderedGotoDestNearPatcher), "DestinationIsReserved")
				)

				.MethodReplacer(
					AccessTools.Method(typeof(GenGrid), "Standable"),
					AccessTools.Method(typeof(BestOrderedGotoDestNearPatcher), "Standable")
				);
		}
	}
}