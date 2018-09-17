using Harmony;
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
	class SameSpotMod : Mod
	{
		public static SameSpotModSettings Settings;

		public SameSpotMod(ModContentPack content) : base(content)
		{
			Settings = GetSettings<SameSpotModSettings>();

			var harmony = HarmonyInstance.Create("net.pardeike.rimworld.mod.samespot");
			harmony.PatchAll(Assembly.GetExecutingAssembly());
			FireStats.Trigger(true);
		}

		public override void DoSettingsWindowContents(Rect inRect)
		{
			Settings.DoWindowContents(inRect);
		}

		public override string SettingsCategory()
		{
			return "SameSpot";
		}
	}

	[StaticConstructorOnStartup]
	public static class Main
	{
		public static IntVec3 lastCell = IntVec3.Invalid;
		public static IntVec3 dragStart = IntVec3.Invalid;
		public static List<Colonist> draggedColonists = new List<Colonist>();

		public static Material markerMaterial = MaterialPool.MatFrom("SameSpotMarker");

		public static bool IsReserved(this PawnDestinationReservationManager instance, IntVec3 loc)
		{
			if (Find.Selector.SelectedObjects.Count == 1) return false;
			if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) return false;
			return instance.IsReserved(loc);
		}

		public static bool CanReserve(this PawnDestinationReservationManager instance, IntVec3 c, Pawn searcher, bool draftedOnly)
		{
			if (Find.Selector.SelectedObjects.Count == 1) return true;
			if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) return true;
			return instance.CanReserve(c, searcher);
		}

		public static bool Standable(this IntVec3 c, Map map)
		{
			return true;
		}

		public static List<Thing> GetThingList(this IntVec3 c, Map map)
		{
			return new List<Thing>();
		}
	}

	[HarmonyPatch(typeof(Game))]
	[HarmonyPatch("FinalizeInit")]
	static class Game_FinalizeInit_Patch
	{
		static void Postfix()
		{
			FireStats.Trigger(false);
		}
	}

	[HarmonyPatch(typeof(PawnUtility))]
	[HarmonyPatch("PawnBlockingPathAt")]
	static class PawnUtility_PawnBlockingPathAt_Patch
	{
		static bool Prefix(ref Pawn __result)
		{
			__result = null;
			return false;
		}
	}

	[HarmonyPatch(typeof(Pawn_PathFollower))]
	[HarmonyPatch("WillCollideWithPawnAt")]
	static class Pawn_PathFollower_WillCollideWithPawnAt_Patch
	{
		static bool Prefix(ref bool __result)
		{
			__result = false;
			return false;
		}
	}

	[HarmonyPatch(typeof(Pawn_PathFollower))]
	[HarmonyPatch("PawnCanOccupy")]
	static class Pawn_PathFollower_PawnCanOccupy_Patch
	{
		static bool Prefix(ref bool __result)
		{
			__result = true;
			return false;
		}
	}

	[HarmonyPatch(typeof(JobDriver_Goto))]
	[HarmonyPatch("TryMakePreToilReservations")]
	static class JobDriver_Goto_TryMakePreToilReservations_Patch
	{
		static bool Prefix(ref bool __result)
		{
			__result = true;
			return false;
		}
	}

	[HarmonyPatch]
	static class RCellFinder_BestOrderedGotoDestNear_Patch
	{
		static MethodBase TargetMethod()
		{
			var predicateClass = typeof(RCellFinder).GetNestedTypes(AccessTools.all)
				.FirstOrDefault(t => t.FullName.Contains(nameof(RCellFinder.BestOrderedGotoDestNear)));
			var predicateMethod = predicateClass.GetMethods(AccessTools.all)
				.FirstOrDefault(m => m.GetParameters().First().ParameterType == typeof(IntVec3) && m.ReturnType == typeof(bool));
			if (predicateMethod == null)
				Log.Error("Cannot find predicate method of " + nameof(RCellFinder.BestOrderedGotoDestNear));
			return predicateMethod;
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			return instructions

				.MethodReplacer(
					AccessTools.Method(typeof(PawnDestinationReservationManager), nameof(PawnDestinationReservationManager.CanReserve)),
					AccessTools.Method(typeof(Main), nameof(Main.CanReserve))
				)

				.MethodReplacer(
					AccessTools.Method(typeof(GenGrid), nameof(GenGrid.Standable)),
					AccessTools.Method(typeof(Main), nameof(Main.Standable))
				)

				.MethodReplacer(
					AccessTools.Method(typeof(GridsUtility), nameof(GridsUtility.GetThingList)),
					AccessTools.Method(typeof(Main), nameof(Main.GetThingList))
				);
		}
	}

	[HarmonyPatch(typeof(JobGiver_MoveToStandable))]
	[HarmonyPatch("TryGiveJob")]
	static class JobGiver_MoveToStandable_TryGiveJob_Patch
	{
		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			return instructions
				.MethodReplacer(
					AccessTools.Method(typeof(GridsUtility), nameof(GridsUtility.GetThingList)),
					AccessTools.Method(typeof(Main), nameof(Main.GetThingList))
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
					AccessTools.Method(typeof(Main), nameof(Main.IsReserved))
				)

				.MethodReplacer(
					AccessTools.Method(typeof(GenGrid), nameof(GenGrid.Standable)),
					AccessTools.Method(typeof(Main), nameof(Main.Standable))
				);
		}
	}

	[HarmonyPatch(typeof(SelectionDrawer))]
	[HarmonyPatch(nameof(SelectionDrawer.DrawSelectionOverlays))]
	static class SelectionDrawer_DrawSelectionOverlays_Patch
	{
		static void Postfix()
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
		static void Prefix()
		{
			if (SameSpotMod.Settings.enableDragDrop)
				if (Event.current.button == 0)
					if (Event.current.type == EventType.MouseDown)
						if (Event.current.clickCount == 1)
							MouseDown();
		}

		[HarmonyPriority(Priority.Last)]
		static void Postfix()
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
				&& pawn.IsColonistPlayerControlled
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
				Traverse.Create(selector).Method("SelectUnderMouse").GetValue();

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
					if (colonist.pawn.Map.pathGrid.Walkable(colonist.designation))
						if (colonist.startPosition != colonist.designation)
						{
							var job = new Job(JobDefOf.Goto, colonist.designation);
							if (colonist.pawn.jobs.IsCurrentJobPlayerInterruptible())
								colonist.pawn.jobs.TryTakeOrderedJob(job);
						}
				});

			Main.dragStart = IntVec3.Invalid;
			Main.lastCell = IntVec3.Invalid;
			Main.draggedColonists = new List<Colonist>();
			Event.current.Use();
		}
	}
}