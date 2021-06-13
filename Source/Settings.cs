using UnityEngine;
using Verse;

namespace SameSpot
{
	public class SameSpotModSettings : ModSettings
	{
		public bool enableDragDrop = true;
		public bool hardcoreMode = false;
		public bool walkableMode = false;
		public int colonistsPerCell = 0;

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref enableDragDrop, "enableDragDrop", true);
			Scribe_Values.Look(ref hardcoreMode, "hardcoreMode", false);
			Scribe_Values.Look(ref walkableMode, "walkableMode", false);
			Scribe_Values.Look(ref colonistsPerCell, "colonistsPerCell", 0);
		}

		public void DoWindowContents(Rect inRect)
		{
			var list = new Listing_Standard { ColumnWidth = inRect.width / 2f };
			list.Begin(inRect);
			list.Gap(12f);
			list.CheckboxLabeled("Enable Drag'n Drop", ref enableDragDrop);
			list.CheckboxLabeled("SameSpot also for enemies", ref hardcoreMode);
			list.CheckboxLabeled("Make walkable also standable", ref walkableMode);
			_ = list.Label($"Maximum colonists per cell: {(colonistsPerCell == 0 ? "unlimited" : "" + colonistsPerCell)}");
			colonistsPerCell = (int)Mathf.Min(20, list.Slider(colonistsPerCell + 0.5f, 0, 21));
			list.End();
		}
	}
}
