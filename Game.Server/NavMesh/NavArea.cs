namespace Game.Server.NavMesh;

using HidingSpotVector = List<HidingSpot>;
using SpotEncounterVector = List<SpotEncounter>;
using SpotOrderVector = List<SpotOrder>;
using NavConnectVector = List<NavConnect>;
using NavLadderConnectVector = List<NavLadderConnect>;

using System.Numerics;

using Source;

struct NavConnect()
{
	public uint ID = 0;
	public NavArea? Area;
	public float Length = -1;
	public readonly bool Equals(NavConnect other) => Area == other.Area;
}

struct NavLadderConnect()
{
	public uint IDs;
	public NavLadder? Ladder;
	public readonly bool Equals(NavLadderConnect other) => Ladder == other.Ladder;
}

struct SpotOrder
{
	public float T;
	public HidingSpot Spot;
	public uint ID;
}

struct SpotEncounter
{
	public NavConnect From;
	public NavDirType FromDir;
	public NavConnect To;
	public NavDirType ToDir;
	public Ray Path;
	public SpotOrderVector Spots;
}


class HidingSpot
{

}

public partial class NavArea
{
	const int MAX_NAV_TEAMS = 2;

	static bool IsReset;
	static uint NextKD;
	uint ID;
	uint DebugID;
	NavPlace Place;
	// CountdownTimer BlockedTimer;
	bool IsUnderwater;
	bool IsBattlefront;
	float AvoidanceObstacleHeight;
	// CountdownTimer AvoidanceObstacleTimer;
	float[] ClearedTimestamp = new float[MAX_NAV_TEAMS];
	float[] Danger = new float[MAX_NAV_TEAMS];
	float[] DangerTimestamp = new float[MAX_NAV_TEAMS];
	HidingSpotVector HidingSpots = [];
	SpotEncounterVector SpotEncounters = [];
	float[] EarliestOccupyTime = new float[MAX_NAV_TEAMS];
	float[] LightIntensity = new float[(int)NavCornerType.NumCorners];
	static uint MasterMarker;
	static NavArea OpenList;
	static NavArea OpenListTail;
	NavConnectVector[] IncomingConnect = new NavConnectVector[(int)NavDirType.NumDirections];
	NavNode[] Node = new NavNode[(int)NavCornerType.NumCorners];
	// List<Handle<FuncNavPrerequisite>> PrerequisiteVector;   // list of prerequisites that must be met before this area can be traversed
	NavArea PrevHash, NextHash;
	int DamagingTickCount;
	// AreaBindInfo InheritVisibilityFrom;
	// AreaBindInfoArray PotentiallyVisibleAreas;
	bool IsInheritedFrom;
	UInt32 VisTestCounter;
	static UInt32 CurrVisTestCounter;

	void CompressIDs() { }

	NavArea() { }

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

	void ResetNodes() { }

	bool HasNodes() {
		throw new NotImplementedException();
	}

	void OnDestroyNotify(NavArea dead) { }

	void OnDestroyNotify(NavLadder dead) { }

	void ConnectTo(NavArea area, NavDirType dir) { }

	void ConnectTo(NavLadder ladder) { }

	void Disconnect(NavArea area) { }

	void Disconnect(NavLadder ladder) { }

	void AddLadderUp(NavLadder ladder) { }

	void AddLadderDown(NavLadder ladder) { }

	void FinishMerge(NavArea adjArea) { }

	void MergeAdjacentConnections(NavArea adjArea) { }

	void AssignNodes(NavArea area) { }

	bool SplitEdit(bool splitAlongX, float splitEdge, NavArea outAlpha, NavArea outBeta) {
		throw new NotImplementedException();
	}

	// bool IsConnected(NavLadder ladder, NavLadder.LadderDirectionType dir) {
	// 	throw new NotImplementedException();
	// }

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
