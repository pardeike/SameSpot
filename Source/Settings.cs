using UnityEngine;
using Verse;

namespace SameSpot
{
	public class SameSpotModSettings : ModSettings
	{
		public bool enableDragDrop = true;
		public bool hardcoreMode = false;
		public bool walkableMode = false;

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref enableDragDrop, "enableDragDrop", true);
			Scribe_Values.Look(ref hardcoreMode, "hardcoreMode", false);
			Scribe_Values.Look(ref walkableMode, "walkableMode", false);
		}

		public void DoWindowContents(Rect inRect)
		{
			var list = new Listing_Standard { ColumnWidth = (inRect.width - 34f) / 2f };
			list.Begin(inRect);
			list.Gap(12f);
			list.CheckboxLabeled("Enable Drag'n Drop", ref enableDragDrop);
			list.CheckboxLabeled("SameSpot also for enemies", ref hardcoreMode);
			list.CheckboxLabeled("Make walkable also standable", ref walkableMode);
			list.End();
		}
	}
}