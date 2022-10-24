using Brrainz;
using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using Verse;

namespace SameSpot
{
	class SameSpotMod : Mod
	{
		public static SameSpotModSettings Settings;
		public static Harmony harmony;

		public SameSpotMod(ModContentPack content) : base(content)
		{
			Settings = GetSettings<SameSpotModSettings>();

			harmony = new Harmony("net.pardeike.rimworld.mod.samespot");
			harmony.PatchAll();

			CrossPromotion.Install(76561197973010050);
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

		public static readonly Material markerMaterial = MaterialPool.MatFrom("SameSpotMarker");

		[MethodImpl(MethodImplOptions.NoInlining)]
		public static bool CustomStandable(this IntVec3 c, Map map)
		{
			if (SameSpotMod.Settings.walkableMode)
				return GenGrid.Walkable(c, map);

			var edifice = c.GetEdifice(map);
			return edifice == null || (edifice as Building_Door) != null;
		}

		public static bool CustomIsReserved(this PawnDestinationReservationManager instance, IntVec3 loc)
		{
			if (SameSpotMod.Settings.colonistsPerCell == 0) return false;
			var count = instance.reservedDestinations.SelectMany(pair => pair.Value.list).Count(res => res.obsolete == false && res.target == loc);
			return count >= SameSpotMod.Settings.colonistsPerCell;
		}

		public static bool CustomCanReserve(this PawnDestinationReservationManager instance, IntVec3 c, Pawn searcher, bool draftedOnly)
		{
			_ = draftedOnly;
			if (SameSpotMod.Settings.colonistsPerCell == 0) return true;
			var count = instance.reservedDestinations.SelectMany(pair => pair.Value.list).Count(res => res.obsolete == false && res.claimant != searcher && res.target == c);
			return count < SameSpotMod.Settings.colonistsPerCell;
		}

		public static List<Thing> GetThingList(this IntVec3 c, Map map)
		{
			_ = c;
			_ = map;
			return new List<Thing>();
		}
	}
}