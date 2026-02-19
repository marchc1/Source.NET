namespace Game.Server.NavMesh;

using System.Numerics;

using Source;

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
	public HidingSpot Spot;
	public uint ID;
}

struct SpotEncounter()
{
	public NavConnect From;
	public NavDirType FromDir;
	public NavConnect To;
	public NavDirType ToDir;
	public Ray Path;
	public SpotOrderVector Spots = [];
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
	public static HidingSpotVector TheHidingSpots = [];

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

	NavErrorType PostLoad() {
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
	uint GetID() => ID;
	NavArea? GetArea() => Area;
	void Mark() => Marker = MasterMarker;
	bool IsMarked() => Marker == MasterMarker;
	static void ChangeMasterMarker() => ++MasterMarker;
	public void SetFlags(HidingSpotFlags flags) => Flags = (byte)flags;
	public void SetPosition(Vector3 pos) => Pos = pos;
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

	public readonly NavConnectVector[] Connect = new NavConnectVector[(int)NavDirType.NumDirections];
	public readonly NavLadderConnectVector[] Ladder = new NavLadderConnectVector[(int)NavLadder.LadderDirectionType.NumLadderDirections];
	public NavConnectVector ElevatorAreas;

	public uint NearNavSearchMarker;

	public NavArea? Parent;
	public NavTraverseType ParentHow;

	public float PathLengthSoFar;

	// FuncElevator? Elevator;
}

public partial class NavArea : NavAreaCriticalData
{
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
	// CountdownTimer BlockedTimer;
	bool IsUnderwater;
	bool IsBattlefront;
	float AvoidanceObstacleHeight;
	// CountdownTimer AvoidanceObstacleTimer;
	readonly float[] ClearedTimestamp = new float[MAX_NAV_TEAMS];
	readonly float[] Danger = new float[MAX_NAV_TEAMS];
	readonly float[] DangerTimestamp = new float[MAX_NAV_TEAMS];
	readonly HidingSpotVector HidingSpots = [];
	readonly SpotEncounterVector SpotEncounters = [];
	readonly float[] EarliestOccupyTime = new float[MAX_NAV_TEAMS];
	readonly float[] LightIntensity = new float[(int)NavCornerType.NumCorners];
	static uint MasterMarker;
	static NavArea OpenList;
	static NavArea OpenListTail;
	readonly NavConnectVector[] IncomingConnect = new NavConnectVector[(int)NavDirType.NumDirections];
	readonly NavNode?[] Node = new NavNode[(int)NavCornerType.NumCorners];
	// List<Handle<FuncNavPrerequisite>> PrerequisiteVector;   // list of prerequisites that must be met before this area can be traversed
	NavArea? PrevHash, NextHash;
	int DamagingTickCount;
	AreaBindInfo InheritVisibilityFrom;
	readonly AreaBindInfoArray PotentiallyVisibleAreas = [];
	bool IsInheritedFrom;
	UInt32 VisTestCounter;
	static UInt32 CurrVisTestCounter;

	void CompressIDs() { }

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

	void GetExtent(Extent extent) { }

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

	uint GetID() => ID;
	void SetAttributes(int bits) => AttributeFlags = bits;
	int GetAttributes() => AttributeFlags;
	bool HasAttributes(int bits) => (AttributeFlags & bits) != 0;
	void RemoveAttributes(int bits) => AttributeFlags &= ~bits;
	void SetPlace(NavPlace place) => Place = place;
	NavPlace GetPlace() => Place;

	void AddLadderUp(NavLadder ladder) { }

	void AddLadderDown(NavLadder ladder) { }

	void FinishMerge(NavArea adjArea) { }

	void MergeAdjacentConnections(NavArea adjArea) { }

	void AssignNodes(NavArea area) { }

	bool SplitEdit(bool splitAlongX, float splitEdge, NavArea outAlpha, NavArea outBeta) {
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

	bool IsOverlapping(Vector3 pos, float tolerance) {
		throw new NotImplementedException();
	}

	bool IsOverlapping(NavArea area) {
		throw new NotImplementedException();
	}

	bool IsOverlapping(Extent extent) {
		throw new NotImplementedException();
	}

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
		throw new NotImplementedException();
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

	void Draw() { }

	void DrawFilled(int r, int g, int b, int a, float deltaT, bool noDepthTest, float margin) { }

	void DrawSelectedSet(Vector3 shift) { }

	void DrawDragSelectionSet(Color dragSelectionSetColor) { }

	void DrawHidingSpots() { }

	void DrawConnectedAreas() { }

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

	bool IsBlocked(int teamID, bool ignoreNavBlockers) {
		throw new NotImplementedException();
	}

	void MarkAsBlocked(int teamID, BaseEntity blocker, bool bGenerateEvent) { }

	void UpdateBlockedFromNavBlockers() { }

	void UnblockArea(int teamID) { }

	void UpdateBlocked(bool force, int teamID) { }

	void CheckFloor(BaseEntity ignore) { }

	void MarkObstacleToAvoid(float obstructionHeight) { }

	void UpdateAvoidanceObstacles() { }

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

	void CheckWaterLevel() { }

	void SetupPVS() { }

	bool IsInPVS() {
		throw new NotImplementedException();
	}

	// VisibilityType ComputeVisibility(NavArea area, bool isPVSValid, bool bCheckPVS, bool pOutsidePVS) {
	// 	throw new NotImplementedException();
	// }

	// AreaBindInfoArray ComputeVisibilityDelta(NavArea other) {
	// 	throw new NotImplementedException();
	// }

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

	bool IsDegenerate() {
		throw new NotImplementedException();
	}

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

	bool IsDamaging() {
		throw new NotImplementedException();
	}

	void MarkAsDamaging(float duration) { }

	bool HasAvoidanceObstacle(float maxObstructionHeight) {
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

	float GetZ(Vector3 pos) {
		throw new NotImplementedException();
	}

	Vector3 GetCorner(NavCornerType corner) {
		throw new NotImplementedException();
	}
}
