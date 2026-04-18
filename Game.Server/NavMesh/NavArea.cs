namespace Game.Server.NavMesh;

using System.Numerics;

using Game.Server.NextBot;
using Game.Shared;

using Source;
using Source.Common;

public struct NavConnect()
{
	public uint ID = 0;
	public NavArea? Area;
	public float Length = -1;
	public readonly bool Equals(NavConnect other) => Area == other.Area;
}

public struct NavLadderConnect()
{
	public uint ID;
	public NavLadder? Ladder;
	public readonly bool Equals(NavLadderConnect other) => Ladder == other.Ladder;
}

public struct SpotOrder
{
	public float T;
	public HidingSpot? Spot;
	public uint ID;
}

struct SpotEncounter()
{
	public NavConnect From;
	public NavDirType FromDir;
	public NavConnect To;
	public NavDirType ToDir;
	public Ray Path;
	public List<SpotOrder> Spots = [];
}

class SplitNotification(NavArea originalArea, NavArea alphaArea, NavArea betaArea)
{
	private NavArea OriginalArea = originalArea;
	private NavArea AlphaArea = alphaArea;
	private NavArea BetaArea = betaArea;
	public bool Invoke(NavLadder ladder) {
		ladder.OnSplit(OriginalArea, AlphaArea, BetaArea);
		return true;
	}
}

public class NavAreaCriticalData
{
	public const int MAX_NAV_TEAMS = 2;

	public Vector3 NWCorner;
	public Vector3 SECorner;
	public float InvDXCorners;
	public float InvDYCorners;
	public float NEZ;
	public float SWZ;
	public Vector3 Center;

	public byte[] PlayerCount = new byte[MAX_NAV_TEAMS];
	public bool[] _IsBlocked = new bool[MAX_NAV_TEAMS];

	public uint Marker;
	public float TotalCost;
	public float CostSoFar;

	public NavArea? NextOpen, PrevOpen;
	public uint OpenMarker;
	public int AttributeFlags;

	public readonly List<NavConnect>[] Connect = new List<NavConnect>[(int)NavDirType.NumDirections];
	public readonly List<NavLadderConnect>[] Ladder = new List<NavLadderConnect>[(int)NavLadder.LadderDirectionType.NumLadderDirections];
	public List<NavConnect> ElevatorAreas;

	public uint NearNavSearchMarker;

	public NavArea? Parent;
	public NavTraverseType ParentHow;

	public float PathLengthSoFar;

	// FuncElevator? Elevator;
}

public partial class NavArea : NavAreaCriticalData
{
	public static List<NavArea> TheNavAreas = [];

	public struct AreaBindInfo
	{
		public NavArea? Area;
		public uint ID;
		public byte Attributes;

		public readonly bool Equals(AreaBindInfo other) => Area == other.Area;
	}

	static bool IsReset;
	public static uint NextID;
	uint ID;
	uint DebugID;
	NavPlace Place;
	CountdownTimer BlockedTimer;
	public bool IsUnderwater;
	bool IsBattlefront;
	float AvoidanceObstacleHeight;
	CountdownTimer AvoidanceObstacleTimer;
	readonly float[] ClearedTimestamp = new float[MAX_NAV_TEAMS];
	readonly float[] Danger = new float[MAX_NAV_TEAMS];
	readonly float[] DangerTimestamp = new float[MAX_NAV_TEAMS];
	readonly List<HidingSpot> HidingSpots = [];
	readonly List<SpotEncounter> SpotEncounters = [];
	readonly float[] EarliestOccupyTime = new float[MAX_NAV_TEAMS];
	readonly float[] LightIntensity = new float[(int)NavCornerType.NumCorners];
	static uint MasterMarker;
	static NavArea OpenList;
	static NavArea OpenListTail;
	readonly List<NavConnect>[] IncomingConnect = new List<NavConnect>[(int)NavDirType.NumDirections];
	readonly NavNode?[] Node = new NavNode[(int)NavCornerType.NumCorners];
	List<Handle<FuncNavPrerequisite>> PrerequisiteVector;   // list of prerequisites that must be met before this area can be traversed
	public NavArea? PrevHash, NextHash;
	int DamagingTickCount;
	AreaBindInfo InheritVisibilityFrom;
	readonly List<AreaBindInfo> PotentiallyVisibleAreas = [];
	bool IsInheritedFrom;
	UInt32 VisTestCounter;
	static UInt32 CurrVisTestCounter;

	void CompressIDs() {
		NextID = 1;

		foreach (NavArea area in TheNavAreas) {
			area.ID = NextID++;
			NavMesh.Instance!.RemoveNavArea(area);
			NavMesh.Instance.AddNavArea(area);
		}
	}

	public NavArea() {
		Marker = 0;
		NearNavSearchMarker = 0;
		DamagingTickCount = 0;
		OpenMarker = 0;

		Parent = null;
		ParentHow = NavTraverseType.North;
		AttributeFlags = 0;
		Place = NavMesh.Instance!.GetNavPlace();
		IsUnderwater = false;
		AvoidanceObstacleHeight = 0.0f;

		TotalCost = 0.0f;
		CostSoFar = 0.0f;
		PathLengthSoFar = 0.0f;

		ResetNodes();

		for (int i = 0; i < MAX_NAV_TEAMS; i++) {
			_IsBlocked[i] = false;
			Danger[i] = 0.0f;
			DangerTimestamp[i] = 0.0f;
			ClearedTimestamp[i] = 0.0f;
			EarliestOccupyTime[i] = 0.0f;
			PlayerCount[i] = 0;
		}

		ID = NextID++;
		DebugID = ID;

		PrevHash = null;
		NextHash = null;

		IsBattlefront = false;

		for (int i = 0; i < (int)NavDirType.NumDirections; i++)
			Connect[i] = [];

		for (int i = 0; i < (int)NavLadder.LadderDirectionType.NumLadderDirections; i++)
			Ladder[i] = [];

		for (int i = 0; i < (int)NavCornerType.NumCorners; i++)
			LightIntensity[i] = 1.0f;

		// Elevator = null;
		ElevatorAreas = [];

		InvDXCorners = 0;
		InvDYCorners = 0;

		InheritVisibilityFrom.Area = null;
		IsInheritedFrom = false;

		// FuncNavCostVector = [];

		VisTestCounter = UInt32.MaxValue - 1;
	}

	void Build(Vector3 corner, Vector3 otherCorner) { }

	void Build(Vector3 nwCorner, Vector3 neCorner, Vector3 seCorner, Vector3 swCorner) { }

	void Build(NavNode nwNode, NavNode neNode, NavNode seNode, NavNode swNode) { }

	public void GetExtent(ref Extent extent) {
		extent.Lo = NWCorner;
		extent.Hi = SECorner;

		extent.Lo.Z = MathF.Min(extent.Lo.Z, NWCorner.Z);
		extent.Lo.Z = MathF.Min(extent.Lo.Z, SECorner.Z);
		extent.Lo.Z = MathF.Min(extent.Lo.Z, NEZ);
		extent.Lo.Z = MathF.Min(extent.Lo.Z, SWZ);

		extent.Hi.Z = MathF.Max(extent.Hi.Z, NWCorner.Z);
		extent.Hi.Z = MathF.Max(extent.Hi.Z, SECorner.Z);
		extent.Hi.Z = MathF.Max(extent.Hi.Z, NEZ);
		extent.Hi.Z = MathF.Max(extent.Hi.Z, SWZ);
	}

	public Vector3 GetCenter() => Center;

	NavNode FindClosestNode(Vector3 pos, NavDirType dir) {
		throw new NotImplementedException();
	}

	void GetNodes(NavDirType dir, List<NavNode> nodes) { }

	void ConnectElevators() { }

	void OnServerActivate() { }

	void OnRoundRestart() { }

	void ResetNodes() {
		for (int i = 0; i < (int)NavCornerType.NumCorners; i++)
			Node[i] = null;
	}

	bool HasNodes() {
		throw new NotImplementedException();
	}

	void OnDestroyNotify(NavArea dead) { }

	void OnDestroyNotify(NavLadder dead) { }

	void ConnectTo(NavArea area, NavDirType dir) { }

	void ConnectTo(NavLadder ladder) { }

	void Disconnect(NavArea area) { }

	void Disconnect(NavLadder ladder) { }

	public uint GetID() => ID;
	void SetAttributes(int bits) => AttributeFlags = bits;
	public int GetAttributes() => AttributeFlags;
	bool HasAttributes(int bits) => (AttributeFlags & bits) != 0;
	void RemoveAttributes(int bits) => AttributeFlags &= ~bits;
	public void SetPlace(NavPlace place) => Place = place;
	public NavPlace GetPlace() => Place;

	void AddLadderUp(NavLadder ladder) { }

	void AddLadderDown(NavLadder ladder) { }

	void FinishMerge(NavArea adjArea) { }

	void MergeAdjacentConnections(NavArea adjArea) { }

	void AssignNodes(NavArea area) { }

	public bool SplitEdit(bool splitAlongX, float splitEdge, out NavArea outAlpha, out NavArea outBeta) {
		throw new NotImplementedException();
	}

	bool IsConnected(NavLadder ladder, NavLadder.LadderDirectionType dir) {
		throw new NotImplementedException();
	}

	bool IsConnected(NavArea area, NavDirType dir) {
		throw new NotImplementedException();
	}

	float ComputeGroundHeightChange(NavArea area) {
		throw new NotImplementedException();
	}

	void AddIncomingConnection(NavArea source, NavDirType incomingEdgeDir) { }

	void FinishSplitEdit(NavArea newArea, NavDirType ignoreEdge) { }

	bool SpliceEdit(NavArea other) {
		throw new NotImplementedException();
	}

	void CalcDebugID() { }

	bool MergeEdit(NavArea adj) {
		throw new NotImplementedException();
	}

	void InheritAttributes(NavArea first, NavArea second) { }

	void Strip() { }

	bool IsRoughlySquare() {
		throw new NotImplementedException();
	}

	public bool IsOverlapping(Vector3 pos, float tolerance = 0.0f) => pos.X + tolerance >= NWCorner.X && pos.X - tolerance <= SECorner.X && pos.Y + tolerance >= NWCorner.Y && pos.Y - tolerance <= SECorner.Y;

	bool IsOverlapping(NavArea area) => area.NWCorner.X < SECorner.X && area.SECorner.X > NWCorner.X && area.NWCorner.Y < SECorner.Y && area.SECorner.Y > NWCorner.Y;

	bool IsOverlapping(Extent extent) => extent.Lo.X < SECorner.X && extent.Hi.X > NWCorner.X && extent.Lo.Y < SECorner.Y && extent.Hi.Y > NWCorner.Y;

	bool IsOverlappingX(NavArea area) {
		throw new NotImplementedException();
	}

	bool IsOverlappingY(NavArea area) {
		throw new NotImplementedException();
	}

	bool Contains(Vector3 pos) {
		throw new NotImplementedException();
	}

	bool Contains(NavArea area) {
		throw new NotImplementedException();
	}

	void ComputeNormal(Vector3 normal, bool alternate) { }

	void RemoveOrthogonalConnections(NavDirType dir) { }

	bool IsFlat() {
		throw new NotImplementedException();
	}

	bool IsCoplanar(NavArea area) {
		throw new NotImplementedException();
	}

	float GetZ(float x, float y) {
		if (InvDXCorners == 0 || InvDYCorners == 0)
			return NEZ;

		float u = (x - NWCorner.X) * InvDXCorners;
		float v = (y - NWCorner.Y) * InvDYCorners;

		u = u >= 0 ? u : 0;
		u = u >= 1 ? 1 : u;
		v = v >= 0 ? v : 0;
		v = v >= 1 ? 1 : v;

		float northZ = NWCorner.Z + u * (NEZ - NWCorner.Z);
		float southZ = SWZ + u * (SECorner.Z - SWZ);

		return northZ + v * (southZ - northZ);
	}

	void GetClosestPointOnArea(Vector3 pos, Vector3 close) { }

	float GetDistanceSquaredToPoint(Vector3 pos) {
		throw new NotImplementedException();
	}

	NavArea GetRandomAdjacentArea(NavDirType dir) {
		throw new NotImplementedException();
	}

	void CollectAdjacentAreas(List<NavArea> adjVector) {
		throw new NotImplementedException();
	}

	void ComputePortal(NavArea to, NavDirType dir, Vector3 center, float halfWidth) { }

	NavDirType ComputeLargestPortal(NavArea to, Vector3 center, float halfWidth) {
		throw new NotImplementedException();
	}

	void ComputeClosestPointInPortal(NavArea to, NavDirType dir, Vector3 fromPos, Vector3 closePos) { }

	bool IsContiguous(NavArea other) {
		throw new NotImplementedException();
	}

	float ComputeAdjacentConnectionHeightChange(NavArea destinationArea) {
		throw new NotImplementedException();
	}

	bool IsEdge(NavDirType dir) {
		throw new NotImplementedException();
	}

	NavDirType ComputeDirection(Vector3 point) {
		throw new NotImplementedException();
	}

	bool GetCornerHotspot(NavCornerType corner, Vector3[] hotspot) {
		throw new NotImplementedException();
	}

	NavCornerType GetCornerUnderCursor() {
		throw new NotImplementedException();
	}

	public void Draw() { }

	void DrawFilled(int r, int g, int b, int a, float deltaT, bool noDepthTest, float margin) { }

	public void DrawSelectedSet(Vector3 shift) { }

	public void DrawDragSelectionSet(Color dragSelectionSetColor) { }

	void DrawHidingSpots() { }

	public void DrawConnectedAreas() { }

	void AddToOpenList() { }

	void AddToOpenListTail() { }

	void UpdateOnOpenList() { }

	void RemoveFromOpenList() { }

	void ClearSearchLists() { }

	void SetCorner(NavCornerType corner, Vector3 newPosition) { }

	bool IsHidingSpotCollision(Vector3 pos) {
		throw new NotImplementedException();
	}

	void ComputeHidingSpots() { }

	void ComputeSniperSpots() { }

	SpotEncounter GetSpotEncounter(NavArea from, NavArea to) {
		throw new NotImplementedException();
	}

	void AddSpotEncounters(NavArea from, NavDirType fromDir, NavArea to, NavDirType toDir) { }

	void ComputeSpotEncounters() { }

	void DecayDanger() { }

	void IncreaseDanger(int teamID, float amount) { }

	float GetDanger(int teamID) {
		throw new NotImplementedException();
	}

	float GetLightIntensity(Vector3 pos) {
		throw new NotImplementedException();
	}

	float GetLightIntensity(float x, float y) {
		throw new NotImplementedException();
	}

	float GetLightIntensity() {
		throw new NotImplementedException();
	}

	bool ComputeLighting() {
		throw new NotImplementedException();
	}

	void RaiseCorner(NavCornerType corner, int amount, bool raiseAdjacentCorners) { }

	void PlaceOnGround(NavCornerType corner, float inset) { }

	void Shift(Vector3 shift) { }

	public bool IsBlocked(int teamID, bool ignoreNavBlockers = false) {
		throw new NotImplementedException();
	}

	void MarkAsBlocked(int teamID, BaseEntity blocker, bool bGenerateEvent) { }

	void UpdateBlockedFromNavBlockers() { }

	void UnblockArea(int teamID) { }

	public void UpdateBlocked(bool force = false, int teamID = -2) { }

	void CheckFloor(BaseEntity ignore) { }

	public void MarkObstacleToAvoid(float obstructionHeight) { }

	public void UpdateAvoidanceObstacles() { }

	void ClearAllNavCostEntities() { }

	void AddFuncNavCostEntity(FuncNavCost cost) { }

	float ComputeFuncNavCost(BaseCombatCharacter who) {
		throw new NotImplementedException();
	}

	bool HasFuncNavAvoid() {
		throw new NotImplementedException();
	}

	bool HasFuncNavPrefer() {
		throw new NotImplementedException();
	}

	void CheckWaterLevel() {
		Vector3 pos = GetCenter();
		if (!NavMesh.Instance!.GetGroundHeight(pos, out float z)) {
			IsUnderwater = false;
			return;
		}

		pos.Z = z + 1;
		// enginetrace todo
	}

	void SetupPVS() { }

	bool IsInPVS() {
		throw new NotImplementedException();
	}

	// VisibilityType ComputeVisibility(NavArea area, bool isPVSValid, bool bCheckPVS, bool pOutsidePVS) {
	// 	throw new NotImplementedException();
	// }

	List<AreaBindInfo> ComputeVisibilityDelta(NavArea other) {
		throw new NotImplementedException();
	}

	void ResetPotentiallyVisibleAreas() { }

	void ComputeVisToArea(NavArea OtherArea) { }

	void ComputeVisibilityToMesh() { }

	bool IsEntirelyVisible(Vector3 eye, BaseEntity ignore) {
		throw new NotImplementedException();
	}

	bool IsPartiallyVisible(Vector3 eye, BaseEntity ignore) {
		throw new NotImplementedException();
	}

	bool IsPotentiallyVisible(NavArea viewedArea) {
		throw new NotImplementedException();
	}

	bool IsCompletelyVisible(NavArea viewedArea) {
		throw new NotImplementedException();
	}

	bool IsPotentiallyVisibleToTeam(int teamIndex) {
		throw new NotImplementedException();
	}

	bool IsCompletelyVisibleToTeam(int teamIndex) {
		throw new NotImplementedException();
	}

	Vector3 GetRandomPoint() {
		throw new NotImplementedException();
	}

	bool HasPrerequisite(BaseCombatCharacter actor) {
		throw new NotImplementedException();
	}

	// List<Handle<FuncNavPrerequisite>> GetPrerequisiteVector() {
	// 	throw new NotImplementedException();
	// }

	void RemoveAllPrerequisites() { }

	// void AddPrerequisite(FuncNavPrerequisite prereq) {
	// 	throw new NotImplementedException();
	// }

	float GetDangerDecayRate() {
		throw new NotImplementedException();
	}

	public float GetSizeX() => SECorner.X - NWCorner.X;
	public float GetSizeY() => SECorner.Y - NWCorner.Y;

	bool IsDegenerate() {
		throw new NotImplementedException();
	}

	public int GetAdjacentCount(NavDirType dir) => Connect[(int)dir].Count;

	NavArea GetAdjacentArea(NavDirType dir, int i) {
		throw new NotImplementedException();
	}

	bool IsOpen() {
		throw new NotImplementedException();
	}

	bool IsOpenListEmpty() {
		throw new NotImplementedException();
	}

	NavArea PopOpenList() {
		throw new NotImplementedException();
	}

	bool IsClosed() {
		throw new NotImplementedException();
	}

	void AddToClosedList() { }

	void RemoveFromClosedList() { }

	void SetClearedTimestamp(int teamID) { }

	float GetClearedTimestamp(int teamID) {
		throw new NotImplementedException();
	}

	float GetEarliestOccupyTime(int teamID) {
		throw new NotImplementedException();
	}

	public bool IsDamaging() {
		throw new NotImplementedException();
	}

	void MarkAsDamaging(float duration) { }

	public bool HasAvoidanceObstacle(float maxObstructionHeight = 0) {
		throw new NotImplementedException();
	}

	float GetAvoidanceObstacleHeight() {
		throw new NotImplementedException();
	}

	bool IsVisible(Vector3 eye, Vector3 visSpot) {
		throw new NotImplementedException();
	}

	void IncrementPlayerCount(int teamID, int entIndex) {
		throw new NotImplementedException();
	}

	void DecrementPlayerCount(int teamID, int entIndex) {
		throw new NotImplementedException();
	}

	char GetPlayerCount(int teamID) {
		throw new NotImplementedException();
	}

	public float GetZ(Vector3 pos) => GetZ(pos.X, pos.Y);

	public Vector3 GetCorner(NavCornerType corner) {
		return corner switch {
			NavCornerType.NorthWest => NWCorner,
			NavCornerType.NorthEast => new Vector3(SECorner.X, NWCorner.Y, NEZ),
			NavCornerType.SouthEast => SECorner,
			NavCornerType.SouthWest => new Vector3(NWCorner.X, SECorner.Y, SWZ),
			_ => throw new ArgumentOutOfRangeException(nameof(corner), corner, null)
		};
	}
}
