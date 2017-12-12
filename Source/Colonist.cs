using Verse;
using RimWorld;
using UnityEngine;

namespace SameSpot
{
	public class Colonist
	{
		public Pawn pawn;
		public IntVec3 startPosition;
		public IntVec3 designation;

		public Colonist(Pawn pawn)
		{
			this.pawn = pawn;
			startPosition = pawn.Position;
			designation = IntVec3.Invalid;
		}

		public void UpdateOrderPos(IntVec3 pos)
		{
			var bestCell = RCellFinder.BestOrderedGotoDestNear(pos, pawn);
			if (bestCell.InBounds(pawn.Map))
				designation = bestCell;
		}

		public void DrawDesignation()
		{
			if (designation.IsValid)
			{
				var matrix = new Matrix4x4();
				matrix.SetTRS(designation.ToVector3() + new Vector3(0.5f, 0f, 0.5f), Quaternion.identity, Vector3.one);
				Graphics.DrawMesh(MeshPool.plane10, matrix, SameSpotMod.markerMaterial, 0);
			}
		}
	}
}
