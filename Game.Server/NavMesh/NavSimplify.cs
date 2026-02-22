using Source.Common.Commands;

using System.Numerics;

namespace Game.Server.NavMesh;

static class NavSimplify
{
	static bool ReduceToComponentAreas(NavArea area, bool addToSelectedSet) {
		if (area == null)
			return false;

		bool splitAlongX;
		float splitEdge;

		const float minSplitSize = 2.0f;

		float sizeX = area.GetSizeX();
		float sizeY = area.GetSizeY();

		NavArea? first = null;
		NavArea? second = null;
		NavArea? third = null;
		NavArea? fourth = null;

		bool didSplit = false;

		if (sizeX > Nav.GenerationStepSize) {
			Vector3 nwCorner = area.GetCorner(NavCornerType.NorthWest);
			splitEdge = Nav.RoundToUnits(nwCorner.X, Nav.GenerationStepSize);

			if (splitEdge < nwCorner.X + minSplitSize)
				splitEdge += Nav.GenerationStepSize;

			splitAlongX = false;

			didSplit = area.SplitEdit(splitAlongX, splitEdge, out first, out second);
		}

		if (sizeY > Nav.GenerationStepSize) {
			Vector3 nwCorner = area.GetCorner(NavCornerType.NorthWest);
			splitEdge = Nav.RoundToUnits(nwCorner.Y, Nav.GenerationStepSize);

			if (splitEdge < nwCorner.Y + minSplitSize)
				splitEdge += Nav.GenerationStepSize;

			splitAlongX = true;

			if (didSplit) {
				_ = first!.SplitEdit(splitAlongX, splitEdge, out third, out fourth);
				didSplit = second!.SplitEdit(splitAlongX, splitEdge, out first, out second);
			}
			else
				didSplit = area.SplitEdit(splitAlongX, splitEdge, out first, out second);
		}

		if (!didSplit)
			return false;

		if (addToSelectedSet) {
			if (first != null) NavMesh.Instance!.AddToSelectedSet(first);
			if (second != null) NavMesh.Instance!.AddToSelectedSet(second);
			if (third != null) NavMesh.Instance!.AddToSelectedSet(third);
			if (fourth != null) NavMesh.Instance!.AddToSelectedSet(fourth);
		}

		if (first != null) ReduceToComponentAreas(first, addToSelectedSet);
		if (second != null) ReduceToComponentAreas(second, addToSelectedSet);
		if (third != null) ReduceToComponentAreas(third, addToSelectedSet);
		if (fourth != null) ReduceToComponentAreas(fourth, addToSelectedSet);

		return true;
	}
}

public partial class NavMesh
{
	void RemoveNodes() { }

	void GenerateNodes(in Extent bounds) { }

	void SimplifySelectedAreas() { }

	[ConCommand("nav_simplify_selected", "Chops all selected areas into their component 1x1 areas and re-merges them together into larger areas", FCvar.Cheat)]
	static void nav_simplify_selected() {
		if (!Util.IsCommandIssuedByServerAdmin() || engine.IsDedicatedServer())
			return;

		int selectedSetSize = Instance!.GetSelecteSetSize();
		if (selectedSetSize == 0) {
			Msg("nav_simplify_selected only works on the selected set\n");
			return;
		}

		Instance!.SimplifySelectedAreas();

		Msg($"{selectedSetSize} areas simplified - {Instance!.GetSelecteSetSize()} remain\n");
	}
}