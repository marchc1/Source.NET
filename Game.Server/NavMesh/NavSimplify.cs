using static Game.Server.NavMesh.Nav;
using static Game.Server.NavMesh.NavSimplify;


using Source.Common.Commands;

using System.Numerics;
using Source.Common.Mathematics;

namespace Game.Server.NavMesh;

static class NavSimplify
{
	public static bool ReduceToComponentAreas(NavArea area, bool addToSelectedSet) {
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

		if (sizeX > GenerationStepSize) {
			Vector3 nwCorner = area.GetCorner(NavCornerType.NorthWest);
			splitEdge = RoundToUnits(nwCorner.X, GenerationStepSize);

			if (splitEdge < nwCorner.X + minSplitSize)
				splitEdge += GenerationStepSize;

			splitAlongX = false;

			didSplit = area.SplitEdit(splitAlongX, splitEdge, out first, out second);
		}

		if (sizeY > GenerationStepSize) {
			Vector3 nwCorner = area.GetCorner(NavCornerType.NorthWest);
			splitEdge = RoundToUnits(nwCorner.Y, GenerationStepSize);

			if (splitEdge < nwCorner.Y + minSplitSize)
				splitEdge += GenerationStepSize;

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
	void RemoveNodes() {
		foreach (NavArea area in NavArea.TheNavAreas)
			area.ResetNodes();

		NavNode.CleanupGeneration();
	}

	void GenerateNodes(in Extent bounds) {
		SimplifyGenerationExtent = bounds;
		SeedIdx = 0;

		Assert(GenerationMode == GenerationModeType.Simplify);
		while (SampleStep()) {
			// do nothing
		}
	}

	void SimplifySelectedAreas() {
		GenerationMode = GenerationModeType.Simplify;

		bool savedSplitPlaceOnGround = nav_split_place_on_ground.GetBool();
		nav_split_place_on_ground.SetValue(1);

		float savedCoplanarSlopeDisplacementLimit = nav_coplanar_slope_limit_displacement.GetFloat();
		nav_coplanar_slope_limit_displacement.SetValue(Math.Min(0.5f, savedCoplanarSlopeDisplacementLimit));

		float savedCoplanarSlopeLimit = nav_coplanar_slope_limit.GetFloat();
		nav_coplanar_slope_limit.SetValue(Math.Min(0.5f, savedCoplanarSlopeLimit));

		int savedGrid = nav_snap_to_grid.GetInt();
		nav_snap_to_grid.SetValue(1);

		StripNavigationAreas();
		SetMarkedArea(null);

		NavAreaCollector collector = new();
		ForAllSelectedAreas(collector.Invoke);

		ClearWalkableSeeds();

		Extent bounds = default;

		bounds.Lo.Init(float.MaxValue, float.MaxValue, float.MaxValue);
		bounds.Hi.Init(-float.MaxValue, -float.MaxValue, -float.MaxValue);

		for (int i = 0; i < collector.Areas.Count; ++i) {
			Extent areaExtent = default;

			NavArea area = collector.Areas[i];
			area.GetExtent(ref areaExtent);
			areaExtent.Lo.Z -= HalfHumanHeight;
			areaExtent.Hi.Z += 2 * HalfHumanHeight;
			bounds.Encompass(areaExtent);

			Vector3 center = area.GetCenter();
			center.X = SnapToGrid(center.X);
			center.Y = SnapToGrid(center.Y);

			{
				if (FindGroundForNode(center, out Vector3 normal)) {
					AddWalkableSeed(center, normal);
					center.Z += HalfHumanHeight;
					bounds.Encompass(center);
				}
			}

			RemoveNodes();
			GenerateNodes(bounds);
			ClearWalkableSeeds();

			for (int j = 0; j < collector.Areas.Count; ++j)
				ReduceToComponentAreas(collector.Areas[j], true);

			foreach (NavArea navArea in SelectedSet) {
				Vector3 corner = area.GetCorner(NavCornerType.NorthEast);
				if (FindGroundForNode(corner, out Vector3 normal)) {
					area.Node[(int)NavCornerType.NorthEast] = NavNode.GetNode(corner);
					NavNode? node = area.Node[(int)NavCornerType.NorthEast];
					if (node != null) {
						area.Node[(int)NavCornerType.NorthWest] = node!.GetConnectedNode(NavDirType.West);
						area.Node[(int)NavCornerType.SouthEast] = node!.GetConnectedNode(NavDirType.South);
						if (area.Node[(int)NavCornerType.SouthEast] != null) {
							area.Node[(int)NavCornerType.SouthWest] = area.Node[(int)NavCornerType.SouthEast]!.GetConnectedNode(NavDirType.West);

							if (area.Node[(int)NavCornerType.NorthWest] != null && area.Node[(int)NavCornerType.SouthWest] != null)
								area.AssignNodes(area);
						}
					}
				}
			}

			bool allValid = area.Node[(int)NavCornerType.NorthEast] != null && area.Node[(int)NavCornerType.NorthWest] != null && area.Node[(int)NavCornerType.SouthEast] != null && area.Node[(int)NavCornerType.SouthWest] != null;

			Assert(allValid);
			if (!allValid)
				Warning($"Area {area.GetID()} didn't get any nodes!\n");

			MergeGeneratedAreas();
			SquareUpAreas();
			MarkJumpAreas();
			SplitAreasUnderOverhangs();
			MarkStairAreas();
			StichAndRemoveJumpAreas();
			HandleObstacleTopAreas();
			FixUpGeneratedAreas();
			ClearSelectedSet();

			foreach (NavArea newArea in NavArea.TheNavAreas) {
				if (newArea.HasNodes())
					AddToSelectedSet(newArea);
			}

			RemoveNodes();

			GenerationMode = GenerationModeType.None;
			nav_split_place_on_ground.SetValue(savedSplitPlaceOnGround);
			nav_coplanar_slope_limit_displacement.SetValue(savedCoplanarSlopeDisplacementLimit);
			nav_coplanar_slope_limit.SetValue(savedCoplanarSlopeLimit);
			nav_snap_to_grid.SetValue(savedGrid);
		}
	}

	[ConCommand("nav_simplify_selected", "Chops all selected areas into their component 1x1 areas and re-merges them together into larger areas", FCvar.Cheat)]
	static void nav_simplify_selected() {
		if (!Util.IsCommandIssuedByServerAdmin() || engine.IsDedicatedServer())
			return;

		int selectedSetSize = Instance!.GetSelectedSetSize();
		if (selectedSetSize == 0) {
			Msg("nav_simplify_selected only works on the selected set\n");
			return;
		}

		Instance!.SimplifySelectedAreas();

		Msg($"{selectedSetSize} areas simplified - {Instance!.GetSelectedSetSize()} remain\n");
	}
}