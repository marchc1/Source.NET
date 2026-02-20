using Source;
using Source.Common;
using Source.Common.Commands;
using Source.Common.Formats.BSP;

using System.Numerics;

namespace Game.Server.NavMesh;

public partial class NavMesh
{
	static readonly ConVar nav_edit = new("0", FCvar.GameDLL | FCvar.Cheat, "Set to one to interactively edit the Navigation Mesh. Set to zero to leave edit mode.");
	static readonly ConVar nav_quicksave = new("1", FCvar.GameDLL | FCvar.Cheat, "Set to one to skip the time consuming phases of the analysis.  Useful for data collection and testing."); // TERROR: defaulting to 1, since we don't need the other data
	static readonly ConVar nav_show_approach_points = new("0", FCvar.GameDLL | FCvar.Cheat, "Show Approach Points in the Navigation Mesh.");
	static readonly ConVar nav_show_danger = new("0", FCvar.GameDLL | FCvar.Cheat, "Show current 'danger' levels.");
	static readonly ConVar nav_show_player_counts = new("0", FCvar.GameDLL | FCvar.Cheat, "Show current player counts in each area.");
	static readonly ConVar nav_show_func_nav_avoid = new("0", FCvar.GameDLL | FCvar.Cheat, "Show areas of designer-placed bot avoidance due to func_nav_avoid entities");
	static readonly ConVar nav_show_func_nav_prefer = new("0", FCvar.GameDLL | FCvar.Cheat, "Show areas of designer-placed bot preference due to func_nav_prefer entities");
	static readonly ConVar nav_show_func_nav_prerequisite = new("0", FCvar.GameDLL | FCvar.Cheat, "Show areas of designer-placed bot preference due to func_nav_prerequisite entities");
	static readonly ConVar nav_max_vis_delta_list_length = new("64", FCvar.Cheat);

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

	enum GenerationStateType
	{
		SampleWalkableSpace,
		CreateAreasFromSamples,
		FindHidingSpots,
		FindEncounterSpots,
		FindSniperSpots,
		FindEarliestOccupyTimes,
		FindLightIntensity,
		ComputeMeshVisibility,
		Custom,
		SaveNavMesh,
		NumGenerationStates
	}

	enum GenerationModeType
	{
		None,
		Full,
		Incremental,
		Simplify,
		AnalysisOnly
	}

	struct WalkableSeedSpot
	{
		public Vector3 Pos;
		public Vector3 Normal;
	}

	List<List<NavArea>> Grid = [];
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
	string?[] PlaceName;
	uint PlaceCount;
	EditModeType EditMode;
	bool IsEditing;
	uint NavPlace;
	Vector3 EditCursorPos;
	NavArea? MarkedArea;
	NavArea? SelectedArea;
	NavArea? LastSelectedArea;
	NavCornerType MarkedCorner;
	Vector3 Anchor;
	bool IsPlacePainting;
	bool SplitAlongX;
	float SplitEdge;
	bool ClimbableSurface;
	Vector3 SurfaceNormal;
	Vector3 LadderAnchor;
	Vector3 LadderNormal;
	NavLadder? SelectedLadder;
	NavLadder? LastSelectedLadder;
	NavLadder? MarkedLadder;
	CountdownTimer ShowAreaInfoTimer;
	readonly List<NavArea> SelectedSet = [];
	readonly List<NavArea> DragSelectionSet = [];
	bool ContinuouslySelecting;
	bool ContinuouslyDeselecting;
	bool IsDragDeselecting;
	int DragSelectionVolumeZMax;
	int DragSelectionVolumeZMin;
	NavNode? CurrentNode;
	NavDirType GenerationDir;
	readonly List<NavLadder> Ladders = [];
	GenerationStateType GenerationState;
	GenerationModeType GenerationMode;
	int GenerationIndex;
	int SampleTick;
	bool QuitWhenFinished;
	float GenerationStartTime;
	Extent SimplifyGenerationExtent;
	string? SpawnName;
	readonly List<WalkableSeedSpot> WalkableSeeds = [];
	int SeedIdx;
	int HostThreatModeRestoreValue;
	readonly List<NavArea> TransientAreas = [];
	readonly List<NavArea> AvoidanceObstacleAreas = [];
	// readonly List<INavAvoidanceObstacle> AvoidanceObstacles = [];
	readonly List<NavArea> BlockedAreas = [];
	readonly List<int> StoredSelectedSet = [];
	CountdownTimer UpdateBlockedAreasTimer = new();

	public static NavMesh? Instance;

	public NavMesh() {
		SpawnName = null;
		GridCellSize = 300.0f;
		EditMode = EditModeType.Normal;
		QuitWhenFinished = false;
		HostThreatModeRestoreValue = 0;
		PlaceCount = 0;
		PlaceName = null;

		LoadPlaceDatabase();

		// gameevents

		Reset();
	}

	void Reset() {
		DestroyNavigationMesh();
	}

	NavArea GetMarkedArea() {
		throw new NotImplementedException();
	}

	void DestroyNavigationMesh(bool incremental = false) {
		GenerationMode = GenerationModeType.None;
		CurrentNode = null;
		// ClearWalkableSeeds();

		IsAnalyzed = false;
		IsOutOfDate = false;
		IsEditing = false;
		NavPlace = Nav.UndefinedPlace;
		MarkedArea = null;
		SelectedArea = null;
		QuitWhenFinished = false;

		EditMode = EditModeType.Normal;

		LastSelectedArea = null;
		IsPlacePainting = false;

		ClimbableSurface = false;
		MarkedLadder = null;
		SelectedLadder = null;

		UpdateBlockedAreasTimer.Invalidate();

		SpawnName = null;

		WalkableSeeds.Clear();
	}

	public void Update() {
		if (IsGenerating()) {
			UpdateGeneration(0.03f);
			return;
		}

		if (UpdateBlockedAreasTimer.HasStarted() && UpdateBlockedAreasTimer.IsElapsed()) {
			TestAllAreasForBlockedStatus();
			UpdateBlockedAreasTimer.Invalidate();
		}

		UpdateBlockedAreas();
		UpdateAvoidanceObstacleAreas();

		if (nav_edit.GetBool()) {
			if (IsEditing == false) {
				OnEditModeStart();
				IsEditing = true;
			}

			DrawEditMode();
		}
		else {
			if (IsEditing) {
				OnEditModeEnd();
				IsEditing = false;
			}
		}

		if (nav_show_danger.GetBool()) DrawDanger();
		if (nav_show_player_counts.GetBool()) DrawPlayerCounts();
		if (nav_show_func_nav_avoid.GetBool()) DrawFuncNavAvoid();
		if (nav_show_func_nav_prefer.GetBool()) DrawFuncNavPrefer();
		if (nav_show_func_nav_prerequisite.GetBool()) DrawFuncNavPrerequisite();

		// if (nav_show_potentially_visible.GetBool()) {
		// todo
		// }

		for (int i = 0; i < WalkableSeeds.Count; i++) {
			WalkableSeedSpot spot = WalkableSeeds[i];

			const float height = 50.0f;
			const float width = 25.0f;

			DrawLine(spot.Pos, spot.Pos + height * spot.Normal, 3, 255, 0, 255);
			DrawLine(spot.Pos + new Vector3(width, 0, 0), spot.Pos + height * spot.Normal, 3, 255, 0, 255);
			DrawLine(spot.Pos + new Vector3(-width, 0, 0), spot.Pos + height * spot.Normal, 3, 255, 0, 255);
			DrawLine(spot.Pos + new Vector3(0, width, 0), spot.Pos + height * spot.Normal, 3, 255, 0, 255);
			DrawLine(spot.Pos + new Vector3(0, -width, 0), spot.Pos + height * spot.Normal, 3, 255, 0, 255);
		}
	}

	static void DrawLine(in Vector3 from, in Vector3 to, float _, int r, int g, int b) => Shared.DebugOverlay.Line(from, to, r, g, b, true, Shared.DebugOverlay.Persist);

	void FireGameEvent(IGameEvent gameEvent) { }

	void AllocateGrid(float minX, float maxX, float minY, float maxY) {
		Grid.Clear();

		MinX = minX;
		MinY = minY;

		GridSizeX = (int)((maxX - minX) / GridCellSize) + 1;
		GridSizeY = (int)((maxY - minY) / GridCellSize) + 1;

		Grid = [];
		for (int i = 0; i < GridSizeX * GridSizeY; i++)
			Grid.Add([]);
	}

	public void AddNavArea(NavArea area) {
		if (Grid.Count == 0)
			AllocateGrid(0, 0, 0, 0);

		int loX = WorldToGridX(area.GetCorner(NavCornerType.NorthWest).X);
		int loY = WorldToGridY(area.GetCorner(NavCornerType.NorthWest).Y);
		int hiX = WorldToGridX(area.GetCorner(NavCornerType.SouthEast).X);
		int hiY = WorldToGridY(area.GetCorner(NavCornerType.SouthEast).Y);

		for (int y = loY; y <= hiY; ++y) {
			for (int x = loX; x <= hiX; ++x)
				Grid[x + y * GridSizeX].Add(area);
		}

		int key = ComputeHashKey(area.GetID());

		if (HashTable[key] != null) {
			area.PrevHash = null;
			area.NextHash = HashTable[key];
			HashTable[key].PrevHash = area;
			HashTable[key] = area;
		}
		else {
			HashTable[key] = area;
			area.NextHash = null;
			area.PrevHash = null;
		}

		if ((area.GetAttributes() & (int)NavAttributeType.Transient) != 0)
			TransientAreas.Add(area);

		++AreaCount;
	}

	public void RemoveNavArea(NavArea area) { }

	void OnServerActivate() { }

	void TestAllAreasForBlockedStatus() {
		foreach (NavArea area in NavArea.TheNavAreas)
			area.UpdateBlocked(true);
	}

	void OnRoundRestart() { }

	void OnRoundRestartPreEntity() { }

	void BuildTransientAreaList() { }

	void GridToWorld(int gridX, int gridY, Vector3 pos) { }

	public NavArea? GetNavArea(Vector3 pos, float beneathLimit = 120.0f) {
		if (Grid.Count == 0)
			return null;

		int x = WorldToGridX(pos.X);
		int y = WorldToGridY(pos.Y);
		List<NavArea> areaVector = Grid[x + y * GridSizeX];

		NavArea? use = null;
		float useZ = -99999999.9f;
		Vector3 testPos = pos + new Vector3(0, 0, 5);

		for (int it = 0; it < areaVector.Count; ++it) {
			NavArea area = areaVector[it];

			if (area.IsOverlapping(testPos)) {
				float z = area.GetZ(testPos);

				if (z > testPos.Z)
					continue;

				if (z < pos.Z - beneathLimit)
					continue;

				if (z > useZ) {
					use = area;
					useZ = z;
				}
			}
		}

		return use;
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

	public NavArea? GetNavAreaByID(uint id) {
		int key = ComputeHashKey(id);

		for (NavArea? area = HashTable[key]; area != null; area = area.NextHash) {
			if (area.GetID() == id)
				return area;
		}

		return null;
	}

	public List<NavLadder> GetLadders() => Ladders;

	public NavLadder GetLadderByID(uint id) {
		throw new NotImplementedException();
	}

	uint GetPlace(Vector3 pos) {
		throw new NotImplementedException();
	}

	void LoadPlaceDatabase() { }

	string? PlaceToName(NavPlace place) {
		if (place >= 1 && place <= PlaceCount)
			return PlaceName[place];

		return "";
	}

	public NavPlace NameToPlace(ReadOnlySpan<char> name) {
		for (uint i = 0; i < PlaceCount; i++) {
			if (FStrEq(PlaceName[i], name))
				return i;
		}

		return Nav.UndefinedPlace;
	}

	NavPlace PartialNameToPlace(ReadOnlySpan<char> name) {
		throw new NotImplementedException();
	}

	void PrintAllPlaces() { }

	public bool GetGroundHeight(Vector3 pos, out float height, Vector3? normal = null) {
		// const float maxOffset = 100.0f;

		// TraceFilterGroundEntities filter = new(null, CollisionGroup.None, WALK_THRU_EVERYTHING);

		// Trace result;
		// Vector3 to = new(pos.x, pos.y, pos.z - 10000.0f);
		// Vector3 from = new(pos.x, pos.y, pos.z + HalfHumanHeight + 1e-3);

		// while (to.Z - pos.Z < maxOffset) {
		// 	Util.TraceLine(from, to, Mask.NPCSolidBrushOnly, ref filter, out result);
		// 	if (!result.StartSolid && ((result.Fraction == 1.0f) || ((from.Z - result.EndPos.Z) >= Nav.HalfHumanHeight))) {
		// 		height = result.EndPos.Z;
		// 		if (normal != null)
		// 			normal = !result.Plane.Normal.IsZero() ? result.Plane.Normal : new Vector3(0, 0, 1);
		// 		return true;
		// 	}

		// 	to.Z = result.StartSolid ? from.Z : result.EndPos.Z;
		// 	from.Z = (float)(to.Z + Nav.HalfHumanHeight + 1e-3);
		// }

		height = 0.0f;
		// if (normal != null)
		// 	normal = new Vector3(0, 0, 1);

		return false;
	}

	bool GetSimpleGroundHeight(Vector3 pos, float height, Vector3 normal) {
		throw new NotImplementedException();
	}

	void DrawDanger() { }

	void DrawPlayerCounts() { }

	void DrawFuncNavAvoid() { }

	void DrawFuncNavPrefer() { }

	void DrawFuncNavPrerequisite() { }

	bool IsGenerating() => GenerationMode != GenerationModeType.None;

	void IncreaseDangerNearby(int teamID, float amount, NavArea startArea, Vector3 pos, float maxRadius, float dangerLimit) { }

	void CommandNavMarkWalkable() { }

	void DestroyLadders() { }

	void StripNavigationAreas() { }

	public HidingSpot CreateHidingSpot() => new();

	void DestroyHidingSpots() { }

	void OnAreaBlocked(NavArea area) { }

	void OnAreaUnblocked(NavArea area) { }

	void UpdateBlockedAreas() {
		foreach (NavArea area in BlockedAreas)
			area.UpdateBlocked();
	}

	public void RegisterAvoidanceObstacle(INavAvoidanceObstacle obstruction) { }

	void UnregisterAvoidanceObstacle(INavAvoidanceObstacle obstruction) { }

	void OnAvoidanceObstacleEnteredArea(NavArea area) { }

	void OnAvoidanceObstacleLeftArea(NavArea area) { }

	void UpdateAvoidanceObstacleAreas() {
		foreach (NavArea area in AvoidanceObstacleAreas)
			area.UpdateAvoidanceObstacles();
	}

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

	public static HidingSpot? GetHidingSpotByID(uint id) {
		foreach (HidingSpot spot in HidingSpot.TheHidingSpots) {
			if (spot.GetID() == id)
				return spot;
		}

		return null;
	}
}

[Flags]
public enum HidingSpotFlags : byte
{
	/// <summary>in a corner with good hard cover nearby </summary>
	InCover = 0x01,
	/// <summary>had at least one decent sniping corridor </summary>
	GoodSniperSpot = 0x02,
	/// <summary>can see either very far, or a large area, or both </summary>
	IdealSniperSpot = 0x04,
	/// <summary>spot in the open, usually on a ledge or cliff </summary>
	Exposed = 0x08
};

public class HidingSpot
{
	public static List<HidingSpot> TheHidingSpots = [];

	Vector3 Pos;
	uint ID;
	uint Marker;
	NavArea? Area;
	byte Flags;
	static uint NextID = 1;
	static uint MasterMarker = 0;

	public HidingSpot() {
		Pos = Vector3.Zero;
		ID = NextID++;
		Flags = 0;
		Area = null;
		TheHidingSpots.Add(this);
	}

	public void Save(object? fileBuffer, uint version) { }
	public void Load(BinaryReader fileBuffer, uint version) {
		ID = fileBuffer.ReadUInt32();
		Pos.X = fileBuffer.ReadSingle();
		Pos.Y = fileBuffer.ReadSingle();
		Pos.Z = fileBuffer.ReadSingle();
		Flags = fileBuffer.ReadByte();

		if (ID >= NextID)
			NextID = ID + 1;
	}

	public NavErrorType PostLoad() {
		Area = NavMesh.Instance!.GetNavArea(Pos with { Z = Pos.Z + Nav.HalfHumanHeight });

		if (Area == null)
			DevWarning($"A Hiding Spot is off of the Nav Mesh at setpos {Pos.X} {Pos.Y} {Pos.Z}\n");

		return NavErrorType.Ok;
	}

	bool HasGoodCover() => (Flags & (byte)HidingSpotFlags.InCover) != 0;
	bool IsGoodSniperSpot() => (Flags & (byte)HidingSpotFlags.GoodSniperSpot) != 0;
	bool IsIdealSniperSpot() => (Flags & (byte)HidingSpotFlags.IdealSniperSpot) != 0;
	bool IsExposed() => (Flags & (byte)HidingSpotFlags.Exposed) != 0;
	int GetFlags() => Flags;
	Vector3 GetPosition() => Pos;
	public uint GetID() => ID;
	NavArea? GetArea() => Area;
	void Mark() => Marker = MasterMarker;
	bool IsMarked() => Marker == MasterMarker;
	static void ChangeMasterMarker() => ++MasterMarker;
	public void SetFlags(HidingSpotFlags flags) => Flags = (byte)flags;
	public void SetPosition(Vector3 pos) => Pos = pos;
}
