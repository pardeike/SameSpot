using Verse;
using Harmony;
using RimWorld;
using System.Linq;
using System;
using System.Reflection;
using UnityEngine;
using System.Collections.Generic;
using Verse.AI;

namespace SameSpot
{
	[StaticConstructorOnStartup]
	public static class SameSpotMod
	{
		public static IntVec3 lastCell = IntVec3.Invalid;
		public static IntVec3 dragStart = IntVec3.Invalid;
		public static List<Colonist> draggedColonists = new List<Colonist>();

		public static Material markerMaterial = MaterialPool.MatFrom("SameSpotMarker");

		static SameSpotMod()
		{
			var harmony = HarmonyInstance.Create("net.pardeike.rimworld.mod.samespot");
			harmony.PatchAll(Assembly.GetExecutingAssembly());
		}

		public static bool IsReserved(this PawnDestinationReservationManager instance, IntVec3 loc)
		{
			if (Find.Selector.SelectedObjects.Count == 1) return false;
			if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) return false;
			return instance.IsReserved(loc);
		}

		public static bool CanReserve(this PawnDestinationReservationManager instance, IntVec3 c, Pawn searcher)
		{
			if (Find.Selector.SelectedObjects.Count == 1) return true;
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

	[HarmonyPatch(typeof(SelectionDrawer))]
	[HarmonyPatch("DrawSelectionOverlays")]
	static class SelectionDrawer_DrawSelectionOverlays_Patch
	{
		static void Postfix()
		{
			if (SameSpotMod.dragStart.IsValid)
				SameSpotMod.draggedColonists.Do(colonist => colonist.DrawDesignation());
		}
	}

	[HarmonyPatch(typeof(MainTabsRoot))]
	[HarmonyPatch("HandleLowPriorityShortcuts")]
	static class MainTabsRoot_HandleLowPriorityShortcuts_Patch
	{
		[HarmonyPriority(Priority.First)]
		static void Prefix()
		{
			if (Event.current.button == 0)
				if (Event.current.type == EventType.MouseDown)
					if (Event.current.clickCount == 1)
						MouseDown();
		}

		[HarmonyPriority(Priority.Last)]
		static void Postfix()
		{
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
			return pawn.drafter != null
				&& pawn.IsColonistPlayerControlled
				&& pawn.Downed == false
				&& pawn.jobs.IsCurrentJobPlayerInterruptible();
		}

		static List<Colonist> SelectedColonists()
		{
			return Find.Selector
				.SelectedObjects.OfType<Pawn>()
				.Where(UsefulColonist)
				.Select(colonist => new Colonist(colonist)).ToList();
		}

		static IEnumerable<Pawn> ColonistsAt(IntVec3 cell)
		{
			return Find.VisibleMap.thingGrid.ThingsListAtFast(cell).OfType<Pawn>().Where(UsefulColonist);
		}

		static void MouseDown()
		{
			if (SameSpotMod.dragStart.IsValid == false)
			{
				var mouseCell = UI.MouseCell();

				var colonistsUnderMouse = ColonistsAt(mouseCell);
				if (colonistsUnderMouse.Any())
				{
					SameSpotMod.dragStart = mouseCell;
					SameSpotMod.lastCell = mouseCell;

					var selector = Find.Selector;
					selector.dragBox.active = false;

					if (colonistsUnderMouse.Any(colonist => selector.IsSelected(colonist)) == false)
						Traverse.Create(selector).Method("SelectUnderMouse").GetValue();

					Event.current.Use();
				}
			}
		}

		static void MouseDrag()
		{
			var mouseCell = UI.MouseCell();
			if (SameSpotMod.dragStart.IsValid && mouseCell != SameSpotMod.lastCell)
			{
				if (SameSpotMod.draggedColonists.Count == 0)
					SameSpotMod.draggedColonists = SelectedColonists();

				if (SameSpotMod.draggedColonists.Count > 0)
				{
					SameSpotMod.draggedColonists.Do(colonist =>
					{
						var newPosition = colonist.startPosition + mouseCell - SameSpotMod.dragStart;
						colonist.UpdateOrderPos(newPosition);
					});

					SameSpotMod.lastCell = mouseCell;
				}

				Event.current.Use();
			}
		}

		static void MouseUp()
		{
			if (SameSpotMod.dragStart.IsValid)
			{
				SameSpotMod.draggedColonists.Do(colonist =>
				{
					if (colonist.pawn.Map.pathGrid.Walkable(colonist.designation))
						if (colonist.startPosition != colonist.designation)
						{
							var job = new Job(JobDefOf.Goto, colonist.designation);
							if (colonist.pawn.jobs.IsCurrentJobPlayerInterruptible())
								colonist.pawn.jobs.TryTakeOrderedJob(job);
						}
				});

				SameSpotMod.dragStart = IntVec3.Invalid;
				SameSpotMod.lastCell = IntVec3.Invalid;
				SameSpotMod.draggedColonists = new List<Colonist>();
				Event.current.Use();
			}
		}
	}
}