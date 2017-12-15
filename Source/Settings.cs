using UnityEngine;
using Verse;

namespace SameSpot
{
	public class SameSpotModSettings : ModSettings
	{
		public bool enableDragDrop = true;

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref enableDragDrop, "enableDragDrop", true);
		}

		public void DoWindowContents(Rect inRect)
		{
			var list = new Listing_Standard { ColumnWidth = (inRect.width - 34f) / 2f };
			list.Begin(inRect);
			list.Gap(12f);
			list.CheckboxLabeled("Enable Drag'n Drop", ref enableDragDrop);
			list.End();
		}
	}
}