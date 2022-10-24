using Brrainz;
using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Verse;
using Verse.AI;

namespace SameSpot
{
	[HarmonyPatch(typeof(GenGrid), nameof(GenGrid.Standable))]
	static class GenGrid_Standable_Patch
	{
		[HarmonyPriority(-10000)]
		public static void Postfix(IntVec3 c, Map map, ref bool __result)
		{
			if (__result)
				return;
			if (SameSpotMod.Settings.walkableMode)
				__result = GenGrid.Walkable(c, map);
		}
	}

	[HarmonyPatch(typeof(PawnUtility), nameof(PawnUtility.PawnBlockingPathAt))]
	static class PawnUtility_PawnBlockingPathAt_Patch
	{
		public static bool Prefix(ref Pawn __result, Pawn forPawn)
		{
			if (forPawn.IsColonist || forPawn.IsColonyMech || SameSpotMod.Settings.hardcoreMode)
			{
				__result = null;
				return false;
			}
			return true;
		}

		public static void Postfix(ref Pawn __result, IntVec3 c, Pawn forPawn)
		{
			if (__result != null || SameSpotMod.Settings.colonistsPerCell == 0)
				return;

			var map = forPawn.Map;
			var otherPawns = map.thingGrid.ThingsListAtFast(c).OfType<Pawn>().Where(pawn => pawn != forPawn && (pawn.IsColonist || pawn.IsColonyMech || SameSpotMod.Settings.hardcoreMode));
			if (otherPawns.Count() >= SameSpotMod.Settings.colonistsPerCell)
				__result = otherPawns.First();
		}
	}

	[HarmonyPatch(typeof(Pawn_PathFollower), nameof(Pawn_PathFollower.WillCollideWithPawnAt))]
	static class Pawn_PathFollower_WillCollideWithPawnAt_Patch
	{
		public static bool Prefix(Pawn ___pawn, ref bool __result)
		{
			if (___pawn.IsColonist || ___pawn.IsColonyMech || SameSpotMod.Settings.hardcoreMode)
			{
				__result = false;
				return false;
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(Pawn_PathFollower), nameof(Pawn_PathFollower.PawnCanOccupy))]
	static class Pawn_PathFollower_PawnCanOccupy_Patch
	{
		[HarmonyPriority(-10000)]
		public static void Postfix(Pawn ___pawn, IntVec3 c, ref bool __result)
		{
			if (__result)
				return;

			if (___pawn.IsColonist || ___pawn.IsColonyMech || SameSpotMod.Settings.hardcoreMode)
				if (c.CustomStandable(___pawn.Map))
				__result = true;
		}
	}

	[HarmonyPatch(typeof(JobDriver_Goto), nameof(JobDriver_Goto.TryMakePreToilReservations))]
	static class JobDriver_Goto_TryMakePreToilReservations_Patch
	{
		public static bool Prefix(JobDriver_Goto __instance, ref bool __result)
		{
			if (SameSpotMod.Settings.colonistsPerCell == 0 && (__instance.pawn.IsColonist || __instance.pawn.IsColonyMech || SameSpotMod.Settings.hardcoreMode))
			{
				__result = true;
				return false;
			}
			return true;
		}
	}

	[HarmonyPatch]
	static class RCellFinder_BestOrderedGotoDestNear_Patch
	{
		public static MethodBase TargetMethod()
		{
			return typeof(RCellFinder)
				.GetNestedTypes(AccessTools.all)
				.SelectMany(t => AccessTools.GetDeclaredMethods(t))
				.First(m => m.Name.Contains($"<{nameof(RCellFinder.BestOrderedGotoDestNear)}"));
		}

		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			return instructions

				.MethodReplacer(
					AccessTools.Method(typeof(PawnDestinationReservationManager), nameof(PawnDestinationReservationManager.CanReserve)),
					AccessTools.Method(typeof(Main), nameof(Main.CustomCanReserve))
				)

				.MethodReplacer(
					AccessTools.Method(typeof(GenGrid), nameof(GenGrid.Standable)),
					AccessTools.Method(typeof(Main), nameof(Main.CustomStandable))
				)

				.MethodReplacer(
					AccessTools.Method(typeof(GridsUtility), nameof(GridsUtility.GetThingList)),
					AccessTools.Method(typeof(Main), nameof(Main.GetThingList))
				);
		}
	}

	[HarmonyPatch(typeof(JobGiver_MoveToStandable), nameof(JobGiver_MoveToStandable.TryGiveJob))]
	static class JobGiver_MoveToStandable_TryGiveJob_Patch
	{
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			return instructions
				.MethodReplacer(
					AccessTools.Method(typeof(GridsUtility), nameof(GridsUtility.GetThingList)),
					AccessTools.Method(typeof(Main), nameof(Main.GetThingList))
				);
		}
	}

	[HarmonyPatch(typeof(JoyGiver_InteractBuildingInteractionCell), nameof(JoyGiver_InteractBuildingInteractionCell.TryGivePlayJob))]
	static class JoyGiver_InteractBuildingInteractionCell_TryGivePlayJob_Patch
	{
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			return instructions

				.MethodReplacer(
					AccessTools.Method(typeof(PawnDestinationReservationManager), nameof(PawnDestinationReservationManager.IsReserved), new Type[] { typeof(IntVec3) }),
					AccessTools.Method(typeof(Main), nameof(Main.CustomIsReserved))
				)

				.MethodReplacer(
					AccessTools.Method(typeof(GenGrid), nameof(GenGrid.Standable)),
					AccessTools.Method(typeof(Main), nameof(Main.CustomStandable))
				);
		}
	}

	[HarmonyPatch(typeof(SelectionDrawer))]
	[HarmonyPatch(nameof(SelectionDrawer.DrawSelectionOverlays))]
	static class SelectionDrawer_DrawSelectionOverlays_Patch
	{
		public static void Postfix()
		{
			if (SameSpotMod.Settings.enableDragDrop)
				if (Main.dragStart.IsValid)
					Main.draggedColonists.Do(colonist => colonist.DrawDesignation());
		}
	}

	[HarmonyPatch(typeof(MainTabsRoot))]
	[HarmonyPatch(nameof(MainTabsRoot.HandleLowPriorityShortcuts))]
	static class MainTabsRoot_HandleLowPriorityShortcuts_Patch
	{
		[HarmonyPriority(Priority.First)]
		public static void Prefix()
		{
			if (SameSpotMod.Settings.enableDragDrop)
				if (Event.current.button == 0)
					if (Event.current.type == EventType.MouseDown)
						if (Event.current.clickCount == 1)
							MouseDown();
		}

		[HarmonyPriority(-10000)]
		public static void Postfix()
		{
			if (SameSpotMod.Settings.enableDragDrop == false)
				return;

			var currentEvent = Event.current;
			if (currentEvent.button != 0)
				return;

			var eventType = currentEvent.type;
			if (eventType == EventType.MouseDrag)
				MouseDrag();
			else if (eventType == EventType.MouseUp)
				MouseUp();
		}

		static bool UsefulColonist(Pawn pawn)
		{
			return pawn.drafter != null && pawn.jobs != null
				&& (pawn.IsColonist && pawn.IsColonistPlayerControlled || pawn.IsColonyMech && pawn.IsColonyMechPlayerControlled)
				&& pawn.Downed == false
				&& pawn.drafter.Drafted
				&& pawn.jobs.IsCurrentJobPlayerInterruptible();
		}

		static List<Colonist> SelectedColonists()
		{
			return Find.Selector
				.SelectedObjects.OfType<Pawn>()
				.Where(UsefulColonist)
				.Select(colonist => new Colonist(colonist)).ToList();
		}

		static IEnumerable<Pawn> ColonistsAt(Map map, IntVec3 cell)
		{
			if (cell.InBounds(map) == false)
				return new List<Pawn>();

			var things = map.thingGrid.ThingsListAtFast(cell);
			if (things == null)
				return new List<Pawn>();

			return things.OfType<Pawn>().Where(UsefulColonist);
		}

		static void MouseDown()
		{
			if (Main.dragStart.IsValid)
				return;

			var map = Find.CurrentMap;
			if (map == null)
				return;

			var mouseCell = UI.MouseCell();
			var colonistsUnderMouse = ColonistsAt(map, mouseCell);
			if (colonistsUnderMouse.Any() == false)
				return;

			Main.dragStart = mouseCell;
			Main.lastCell = mouseCell;

			var selector = Find.Selector;
			selector.dragBox.active = false;

			if (colonistsUnderMouse.Any(colonist => selector.IsSelected(colonist)) == false)
				selector.SelectUnderMouse();

			Event.current.Use();
		}

		static void MouseDrag()
		{
			if (Main.dragStart.IsValid == false)
				return;

			var mouseCell = UI.MouseCell();
			if (mouseCell == Main.lastCell)
				return;

			if (Main.draggedColonists.Count == 0)
				Main.draggedColonists = SelectedColonists();

			if (Main.draggedColonists.Count > 0)
			{
				Main.draggedColonists.Do(colonist =>
				{
					var newPosition = colonist.startPosition + mouseCell - Main.dragStart;
					colonist.UpdateOrderPos(newPosition);
				});

				Main.lastCell = mouseCell;
			}

			Event.current.Use();
		}

		static void MouseUp()
		{
			if (Main.dragStart.IsValid == false)
				return;

			Main.draggedColonists.Do(colonist =>
				{
					if (GenGrid.Walkable(colonist.designation, colonist.pawn.Map))
						if (colonist.startPosition != colonist.designation)
						{
							var job = JobMaker.MakeJob(JobDefOf.Goto, colonist.designation);
							if (colonist.pawn.jobs.IsCurrentJobPlayerInterruptible())
								_ = colonist.pawn.jobs.TryTakeOrderedJob(job);
						}
				});

			Main.dragStart = IntVec3.Invalid;
			Main.lastCell = IntVec3.Invalid;
			Main.draggedColonists = new List<Colonist>();
			Event.current.Use();
		}
	}
}
