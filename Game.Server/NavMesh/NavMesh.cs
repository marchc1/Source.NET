using Source.Common;
using Source.Common.Formats.BSP;
using Source.Engine;

using System.Numerics;

namespace Game.Server.NavMesh;

public partial class NavMesh
{
	enum EditModeType
	{
		Normal,
		PlacePainting,
		CreatingArea,
		CreatingLadder,
		DragSelecting,
		ShiftingXY,
		ShiftingZ
	}

	// List<NavAreaVector> Grid;
	float GridCellSize;
	int GridSizeX;
	int GridSizeY;
	float MinX;
	float MinY;
	uint AreaCount;
	bool IsLoaded;
	bool IsOutOfDate;
	bool IsAnalyzed;
	const int HASH_TABLE_SIZE = 256;
	readonly NavArea[] HashTable = new NavArea[HASH_TABLE_SIZE];
	string PlaceName;
	uint PlaceCount;
	EditModeType EditMode;
	bool IsEditing;
	uint NavPlace;
	Vector3 EditCursorPos;
	NavArea MarkedArea;
	NavArea SelectedArea;
	NavArea LastSelectedArea;
	NavCornerType MarkedCorner;
	Vector3 Anchor;
	bool IsPlacePainting;
	bool SplitAlongX;
	float SplitEdge;
	bool ClimbableSurface;
	Vector3 SurfaceNormal;
	Vector3 LadderAnchor;
	Vector3 LadderNormal;
	NavLadder SelectedLadder;
	NavLadder LastSelectedLadder;
	NavLadder MarkedLadder;
	// CountdownTimer ShowAreaInfoTimer;
	// NavAreaVector SelectedSet;
	// NavAreaVector DragSelectionSet;
	bool ContinuouslySelecting;
	bool ContinuouslyDeselecting;
	int DragSelectionVolumeZMax;
	int DragSelectionVolumeZMin;
	// NavMode CurrentMode;
	NavDirType GenerationDir;
	// NavLadderVector Ladders;
	// todo finish this..

	public static NavMesh? Instance;

	public NavMesh() { }

	void Reset() { }

	NavArea GetMarkedArea() {
		throw new NotImplementedException();
	}

	void DestroyNavigationMesh(bool incremental) { }

	void Update() { }

	void FireGameEvent(IGameEvent gameEvent) { }

	void AllocateGrid(float minX, float maxX, float minY, float maxY) { }

	void AddNavArea(NavArea area) { }

	void RemoveNavArea(NavArea area) { }

	void OnServerActivate() { }

	void TestAllAreasForBlockedStatus() { }

	void OnRoundRestart() { }

	void OnRoundRestartPreEntity() { }

	void BuildTransientAreaList() { }

	void GridToWorld(int gridX, int gridY, Vector3 pos) { }

	public NavArea GetNavArea(Vector3 pos, float beneathLimit = 120.0f) {
		throw new NotImplementedException();
	}

	NavArea GetNavArea(BaseEntity pEntity, int nFlags, float flBeneathLimit) {
		throw new NotImplementedException();
	}

	NavArea GetNearestNavArea(Vector3 pos, bool anyZ, float maxDist, bool checkLOS, bool checkGround, int team) {
		throw new NotImplementedException();
	}

	NavArea GetNearestNavArea(BaseEntity pEntity, int nFlags, float maxDist) {
		throw new NotImplementedException();
	}

	NavArea GetNavAreaByID(uint id) {
		throw new NotImplementedException();
	}

	NavLadder GetLadderByID(uint id) {
		throw new NotImplementedException();
	}

	uint GetPlace(Vector3 pos) {
		throw new NotImplementedException();
	}

	void LoadPlaceDatabase() { }

	string PlaceToName(NavPlace place) {
		throw new NotImplementedException();
	}

	public NavPlace NameToPlace(ReadOnlySpan<char> name) {
		return Nav.AnyPlace; // todo
	}

	NavPlace PartialNameToPlace(ReadOnlySpan<char> name) {
		throw new NotImplementedException();
	}

	void PrintAllPlaces() { }

	bool GetGroundHeight(Vector3 pos, float height, Vector3 normal) {
		throw new NotImplementedException();
	}

	bool GetSimpleGroundHeight(Vector3 pos, float height, Vector3 normal) {
		throw new NotImplementedException();
	}

	void DrawDanger() { }

	void DrawPlayerCounts() { }

	void DrawFuncNavAvoid() { }

	void DrawFuncNavPrefer() { }

	void DrawFuncNavPrerequisite() { }

	void IncreaseDangerNearby(int teamID, float amount, NavArea startArea, Vector3 pos, float maxRadius, float dangerLimit) { }

	void CommandNavMarkWalkable() { }

	void DestroyLadders() { }

	void StripNavigationAreas() { }

	public HidingSpot CreateHidingSpot() => new();

	void DestroyHidingSpots() { }

	void OnAreaBlocked(NavArea area) { }

	void OnAreaUnblocked(NavArea area) { }

	void UpdateBlockedAreas() { }

	// void RegisterAvoidanceObstacle(INavAvoidanceObstacle obstruction) { }

	// void UnregisterAvoidanceObstacle(INavAvoidanceObstacle obstruction) { }

	void OnAvoidanceObstacleEnteredArea(NavArea area) { }

	void OnAvoidanceObstacleLeftArea(NavArea area) { }

	void UpdateAvoidanceObstacleAreas() { }

	void BeginVisibilityComputations() { }

	void EndVisibilityComputations() { }

	bool IsEditMode(EditModeType mode) => EditMode == mode;

	EditModeType GetEditMode() => EditMode;

	uint GetSubVersionNumber() => 0;

	NavArea CreateArea() => new();

	void DestroyArea(NavArea area) { }

	int ComputeHashKey(uint id) => (int)(id & 0xFF);

	int WorldToGridX(float wx) {
		int x = (int)((wx - MinX) / GridCellSize);

		if (x < 0)
			x = 0;
		else if (x >= GridSizeX)
			x = GridSizeX - 1;

		return x;
	}

	int WorldToGridY(float wy) {
		int y = (int)((wy - MinY) / GridCellSize);

		if (y < 0)
			y = 0;
		else if (y >= GridSizeY)
			y = GridSizeY - 1;

		return y;
	}

	static Mask GetGenerationTraceMask() => Mask.NPCSolidBrushOnly;
}