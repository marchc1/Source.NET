using static Game.Server.NavMesh.Nav;

namespace Game.Server.NavMesh;

using System.Collections.Concurrent;
using System.Numerics;

using Game.Server.NextBot;

using Source;
using Source.Common;
using Source.Common.Formats.BSP;
using Source.Common.Mathematics;

public class FuncElevator;

public struct NavConnect() : IEquatable<NavConnect>
{
	public uint ID = 0;
	public NavArea? Area;
	public float Length = -1;
	public readonly bool Equals(NavConnect other) => Area == other.Area;
	public override readonly bool Equals(object? obj) => obj is NavConnect other && Equals(other);
	public override readonly int GetHashCode() => Area?.GetHashCode() ?? 0;
}

public struct NavLadderConnect() : IEquatable<NavLadderConnect>
{
	public uint ID;
	public NavLadder? Ladder;
	public readonly bool Equals(NavLadderConnect other) => Ladder == other.Ladder;
	public override readonly bool Equals(object? obj) => obj is NavLadderConnect other && Equals(other);
	public override readonly int GetHashCode() => Ladder?.GetHashCode() ?? 0;
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
	public NavAttributeType AttributeFlags;

	public readonly List<NavConnect>[] Connect = new List<NavConnect>[(int)NavDirType.NumDirections];
	public readonly List<NavLadderConnect>[] Ladder = new List<NavLadderConnect>[(int)NavLadder.LadderDirectionType.NumLadderDirections];
	public List<NavConnect> ElevatorAreas;

	public uint NearNavSearchMarker;

	public NavArea? Parent;
	public NavTraverseType ParentHow;

	public float PathLengthSoFar;

	public FuncElevator? Elevator;
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

	public static bool IsReset;
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
	static NavArea? OpenList;
	static NavArea? OpenListTail;
	readonly List<NavConnect>[] IncomingConnect = new List<NavConnect>[(int)NavDirType.NumDirections];
	public readonly NavNode?[] Node = new NavNode[(int)NavCornerType.NumCorners];
	List<Handle<FuncNavPrerequisite>> PrerequisiteVector;   // list of prerequisites that must be met before this area can be traversed
	public NavArea? PrevHash, NextHash;
	int DamagingTickCount;
	AreaBindInfo InheritVisibilityFrom;
	readonly List<AreaBindInfo> PotentiallyVisibleAreas = [];
	bool IsInheritedFrom;
	UInt32 VisTestCounter;
	static UInt32 CurrVisTestCounter;
	List<FuncNavCost> FuncNavCostVector;

	public static void CompressIDs() {
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

		for (int i = 0; i < IncomingConnect.Length; i++) IncomingConnect[i] = [];

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

		Elevator = null;
		ElevatorAreas = [];

		InvDXCorners = 0;
		InvDYCorners = 0;

		InheritVisibilityFrom.Area = null;
		IsInheritedFrom = false;

		FuncNavCostVector = [];

		VisTestCounter = UInt32.MaxValue - 1;
	}

	void Build(Vector3 corner, Vector3 otherCorner) {
		if (corner.X < otherCorner.X) {
			NWCorner.X = corner.X;
			SECorner.X = otherCorner.X;
		}
		else {
			SECorner.X = corner.X;
			NWCorner.X = otherCorner.X;
		}

		if (corner.Y < otherCorner.Y) {
			NWCorner.Y = corner.Y;
			SECorner.Y = otherCorner.Y;
		}
		else {
			SECorner.Y = corner.Y;
			NWCorner.Y = otherCorner.Y;
		}

		NWCorner.Z = corner.Z;
		SECorner.Z = corner.Z;

		Center.X = (NWCorner.X + SECorner.X) / 2.0f;
		Center.Y = (NWCorner.Y + SECorner.Y) / 2.0f;
		Center.Z = (NWCorner.Z + SECorner.Z) / 2.0f;

		if ((SECorner.X - NWCorner.X) > 0.0f && (SECorner.Y - NWCorner.Y) > 0.0f) {
			InvDXCorners = 1.0f / (SECorner.X - NWCorner.X);
			InvDYCorners = 1.0f / (SECorner.Y - NWCorner.Y);
		}
		else
			InvDXCorners = InvDYCorners = 0;

		NEZ = corner.Z;
		SWZ = otherCorner.Z;

		CalcDebugID();
	}

	void Build(Vector3 nwCorner, Vector3 neCorner, Vector3 seCorner, Vector3 swCorner) {
		NWCorner = nwCorner;
		SECorner = seCorner;

		Center.X = (NWCorner.X + SECorner.X) / 2.0f;
		Center.Y = (NWCorner.Y + SECorner.Y) / 2.0f;
		Center.Z = (NWCorner.Z + SECorner.Z) / 2.0f;

		NEZ = neCorner.Z;
		SWZ = swCorner.Z;

		if ((SECorner.X - NWCorner.X) > 0.0f && (SECorner.Y - NWCorner.Y) > 0.0f) {
			InvDXCorners = 1.0f / (SECorner.X - NWCorner.X);
			InvDYCorners = 1.0f / (SECorner.Y - NWCorner.Y);
		}
		else {
			InvDXCorners = InvDYCorners = 0;
		}

		CalcDebugID();
	}

	public void Build(NavNode nwNode, NavNode neNode, NavNode seNode, NavNode swNode) {
		NWCorner = nwNode.GetPosition();
		SECorner = seNode.GetPosition();

		Center.X = (NWCorner.X + SECorner.X) / 2.0f;
		Center.Y = (NWCorner.Y + SECorner.Y) / 2.0f;
		Center.Z = (NWCorner.Z + SECorner.Z) / 2.0f;

		NEZ = neNode.GetPosition().Z;
		SWZ = swNode.GetPosition().Z;

		Node[(int)NavCornerType.NorthWest] = nwNode;
		Node[(int)NavCornerType.NorthEast] = neNode;
		Node[(int)NavCornerType.SouthEast] = seNode;
		Node[(int)NavCornerType.SouthWest] = swNode;

		if ((SECorner.X - NWCorner.X) > 0.0f && (SECorner.Y - NWCorner.Y) > 0.0f) {
			InvDXCorners = 1.0f / (SECorner.X - NWCorner.X);
			InvDYCorners = 1.0f / (SECorner.Y - NWCorner.Y);
		}
		else
			InvDXCorners = InvDYCorners = 0;

		AssignNodes(this);

		CalcDebugID();
	}

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

	public void OnServerActivate() { }

	void OnRoundRestart() { }

	public void ResetNodes() {
		for (int i = 0; i < (int)NavCornerType.NumCorners; i++)
			Node[i] = null;
	}

	public bool HasNodes() {
		throw new NotImplementedException();
	}

	void OnDestroyNotify(NavArea dead) { }

	void OnDestroyNotify(NavLadder dead) { }

	public void ConnectTo(NavArea area, NavDirType dir) {
		if (area == this)
			return;

		foreach (NavConnect connect in Connect[(int)dir])
			if (connect.Area == area)
				return;

		NavConnect con = new() {
			Area = area,
			Length = (area.GetCenter() - GetCenter()).Length()
		};
		Connect[(int)dir].Add(con);
		IncomingConnect[(int)dir].RemoveAll(c => c.Area == area);

		NavDirType opposite = OppositeDirection(dir);
		con.Area = this;

		if (area.Connect[(int)opposite].FindIndex(c => c.Area == this) == -1)
			area.AddIncomingConnection(this, opposite);
	}

	void ConnectTo(NavLadder ladder) { }

	void Disconnect(NavArea area) { }

	void Disconnect(NavLadder ladder) { }

	public uint GetID() => ID;

	public void SetAttributes(NavAttributeType bits) => AttributeFlags = bits;

	public NavAttributeType GetAttributes() => AttributeFlags;

	public bool HasAttributes(NavAttributeType bits) => (AttributeFlags & bits) != 0;

	void RemoveAttributes(NavAttributeType bits) => AttributeFlags &= ~bits;

	public void SetPlace(NavPlace place) => Place = place;

	public NavPlace GetPlace() => Place;

	void AddLadderUp(NavLadder ladder) { }

	void AddLadderDown(NavLadder ladder) { }

	void FinishMerge(NavArea adjArea) { }

	void MergeAdjacentConnections(NavArea adjArea) { }

	public void AssignNodes(NavArea area) {
		NavNode? horizLast = Node[(int)NavCornerType.NorthEast];
		for (NavNode? vertNode = Node[(int)NavCornerType.NorthWest]; vertNode != Node[(int)NavCornerType.SouthWest]; vertNode = vertNode!.GetConnectedNode(NavDirType.South)) {
			for (NavNode? horizNode = vertNode; horizNode != horizLast; horizNode = horizNode!.GetConnectedNode(NavDirType.East))
				horizNode!.AssignArea(area);

			horizLast = horizLast!.GetConnectedNode(NavDirType.South);
		}
	}

	public bool SplitEdit(bool splitAlongX, float splitEdge, out NavArea outAlpha, out NavArea outBeta) {
		throw new NotImplementedException();
	}

	bool IsConnected(NavLadder ladder, NavLadder.LadderDirectionType dir) {
		throw new NotImplementedException();
	}

	public bool IsConnected(NavArea area, NavDirType dir) {
		throw new NotImplementedException();
	}

	float ComputeGroundHeightChange(NavArea area) {
		throw new NotImplementedException();
	}

	public List<NavConnect> GetIncomingConnections(NavDirType dir) => IncomingConnect[(int)dir];

	public void AddIncomingConnection(NavArea source, NavDirType incomingEdgeDir) {
		NavConnect connect = new() {
			Area = source
		};

		if (!IncomingConnect[(int)incomingEdgeDir].Contains(connect)) {
			connect.Length = (source.GetCenter() - GetCenter()).Length();
			IncomingConnect[(int)incomingEdgeDir].Add(connect);
		}
	}

	public List<NavLadderConnect> GetLadders(NavLadder.LadderDirectionType dir) => Ladder[(int)dir];

	public FuncElevator? GetElevator() => Elevator;

	public List<NavConnect> GetElevatorAreas() => ElevatorAreas;

	void FinishSplitEdit(NavArea newArea, NavDirType ignoreEdge) { }

	bool SpliceEdit(NavArea other) {
		throw new NotImplementedException();
	}

	void CalcDebugID() { }

	bool MergeEdit(NavArea adj) {
		throw new NotImplementedException();
	}

	void InheritAttributes(NavArea first, NavArea second) { }

	public void Strip() => SpotEncounters.Clear();

	bool IsRoughlySquare() {
		throw new NotImplementedException();
	}

	public bool IsOverlapping(Vector3 pos, float tolerance = 0.0f) => pos.X + tolerance >= NWCorner.X && pos.X - tolerance <= SECorner.X && pos.Y + tolerance >= NWCorner.Y && pos.Y - tolerance <= SECorner.Y;

	bool IsOverlapping(NavArea area) => area.NWCorner.X < SECorner.X && area.SECorner.X > NWCorner.X && area.NWCorner.Y < SECorner.Y && area.SECorner.Y > NWCorner.Y;

	public bool IsOverlapping(Extent extent) => extent.Lo.X < SECorner.X && extent.Hi.X > NWCorner.X && extent.Lo.Y < SECorner.Y && extent.Hi.Y > NWCorner.Y;

	bool IsOverlappingX(NavArea area) {
		throw new NotImplementedException();
	}

	bool IsOverlappingY(NavArea area) {
		throw new NotImplementedException();
	}

	public bool Contains(Vector3 pos) {
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

	public void GetClosestPointOnArea(ref Vector3 pos, out Vector3 close) {
		float x = pos.X >= NWCorner.X ? pos.X : NWCorner.X;
		x = x <= SECorner.X ? x : SECorner.X;

		float y = pos.Y >= NWCorner.Y ? pos.Y : NWCorner.Y;
		y = y <= SECorner.Y ? y : SECorner.Y;

		float z = GetZ(x, y);

		close = new(x, y, z);
	}

	float GetDistanceSquaredToPoint(Vector3 pos) {
		throw new NotImplementedException();
	}

	NavArea GetRandomAdjacentArea(NavDirType dir) {
		throw new NotImplementedException();
	}

	void CollectAdjacentAreas(List<NavArea> adjVector) {
		throw new NotImplementedException();
	}

	public List<NavConnect> GetAdjacentAreas(NavDirType dir) => Connect[(int)dir];

	public void ComputePortal(NavArea to, NavDirType dir, ref Vector3 center, out float halfWidth) {
		if (dir == NavDirType.North || dir == NavDirType.South) {
			center.Y = dir == NavDirType.North ? NWCorner.Y : SECorner.Y;

			float left = Math.Max(NWCorner.X, to.NWCorner.X);
			float right = Math.Min(SECorner.X, to.SECorner.X);

			if (left < NWCorner.X)
				left = NWCorner.X;
			else if (left > SECorner.X)
				left = SECorner.X;

			if (right < NWCorner.X)
				right = NWCorner.X;
			else if (right > SECorner.X)
				right = SECorner.X;

			center.X = (left + right) / 2.0f;
			halfWidth = (right - left) / 2.0f;
		}
		else {
			center.X = dir == NavDirType.West ? NWCorner.X : SECorner.X;

			float top = Math.Max(NWCorner.Y, to.NWCorner.Y);
			float bottom = Math.Min(SECorner.Y, to.SECorner.Y);

			if (top < NWCorner.Y)
				top = NWCorner.Y;
			else
				if (top > SECorner.Y)
					top = SECorner.Y;

			if (bottom < NWCorner.Y)
				bottom = NWCorner.Y;
			else if (bottom > SECorner.Y)
				bottom = SECorner.Y;

			center.Y = (top + bottom) / 2.0f;
			halfWidth = (bottom - top) / 2.0f;
		}

		center.Z = GetZ(center.X, center.Y);
	}

	NavDirType ComputeLargestPortal(NavArea to, Vector3 center, float halfWidth) {
		throw new NotImplementedException();
	}

	void ComputeClosestPointInPortal(NavArea to, NavDirType dir, Vector3 fromPos, Vector3 closePos) { }

	bool IsContiguous(NavArea other) {
		throw new NotImplementedException();
	}

	public float ComputeAdjacentConnectionHeightChange(NavArea destinationArea) {
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

	public void AddToOpenList() {
		Assert((OpenList != null && OpenList.PrevOpen == null) || OpenList == null);

		if (IsOpen())
			return;

		OpenMarker = MasterMarker;

		if (OpenList == null) {
			OpenList = this;
			OpenListTail = this;
			PrevOpen = null;
			NextOpen = null;
			return;
		}

		NavArea? area, last = null;

		for (area = OpenList; area != null; area = area.NextOpen) {
			if (GetTotalCost() < area.GetTotalCost())
				break;

			last = area;
		}

		if (area != null) {
			PrevOpen = area.PrevOpen;

			if (PrevOpen != null)
				PrevOpen.NextOpen = this;
			else
				OpenList = this;

			NextOpen = area;
			area.PrevOpen = this;
		}
		else {
			last!.NextOpen = this;
			PrevOpen = last;

			NextOpen = null;

			OpenListTail = this;
		}

		Assert((OpenList != null && OpenList.PrevOpen == null) || OpenList == null);
	}

	void AddToOpenListTail() { }

	public void UpdateOnOpenList() {
		while (PrevOpen != null && GetTotalCost() < PrevOpen.GetTotalCost()) {
			NavArea other = PrevOpen;
			NavArea? before = other.PrevOpen;
			NavArea? after = NextOpen;

			NextOpen = other;
			PrevOpen = before;

			other.PrevOpen = this;
			other.NextOpen = after;

			if (before != null)
				before.NextOpen = this;
			else
				OpenList = this;

			if (after != null)
				after.PrevOpen = other;
			else
				OpenListTail = other;
		}
	}

	void RemoveFromOpenList() { }

	public static void ClearSearchLists() {
		MakeNewMarker();
		OpenList = null;
		OpenListTail = null;
	}

	public void SetTotalCost(float value) {
		Assert(value >= 0);
		TotalCost = value;
	}

	public float GetTotalCost() => TotalCost;

	public void SetCostSoFar(float value) {
		Assert(value >= 0);
		CostSoFar = value;
	}

	public float GetCostSoFar() => CostSoFar;

	public void SetPathLengthSoFar(float value) {
		Assert(value >= 0);
		PathLengthSoFar = value;
	}

	float GetPathLengthSoFar() => PathLengthSoFar;

	void SetCorner(NavCornerType corner, Vector3 newPosition) { }

	bool IsHidingSpotCollision(Vector3 pos) {
		const float collisionRange = 30.0f;

		foreach (HidingSpot spot in HidingSpots) {
			if ((spot.GetPosition() - pos).Length() <= collisionRange)
				return true;
		}

		return false;
	}

	bool IsHidingSpotInCover(Vector3 spot) {
		int coverCount = 0;

		Vector3 from = spot;
		from.Z += HalfHumanHeight;

		Vector3 to = from + new Vector3(0, 0, 20.0f);
		Util.TraceLine(from, to, Mask.NPCSolidBrushOnly, null, CollisionGroup.None, out Trace result);
		if (result.Fraction != 1.0f)
			return true;

		const float coverRange = 100.0f;
		const float inc = (float)(Math.PI / 8.0f);

		for (float angle = 0.0f; angle < 2.0f * Math.PI; angle += inc) {
			to = from + new Vector3(coverRange * (float)Math.Cos(angle), coverRange * (float)Math.Sin(angle), HalfHumanHeight);

			Util.TraceLine(from, to, Mask.NPCSolidBrushOnly, null, CollisionGroup.None, out result);

			if (result.Fraction != 1.0f)
				++coverCount;
		}

		const int halfCover = 8;
		if (coverCount < halfCover)
			return false;

		return true;
	}

	static Vector3 FindPositionInArea(NavArea area, NavCornerType corner) {
		int multX = 1, multY = 1;
		switch (corner) {
			case NavCornerType.NorthWest:
				break;
			case NavCornerType.NorthEast:
				multX = -1;
				break;
			case NavCornerType.SouthWest:
				multY = -1;
				break;
			case NavCornerType.SouthEast:
				multX = -1;
				multY = -1;
				break;
		}

		const float offset = 12.5f;
		Vector3 cornerPos = area.GetCorner(corner);

		Vector3 pos = cornerPos + new Vector3(offset * multX, offset * multY, 0.0f);
		if (!area.IsOverlapping(pos)) {
			pos = cornerPos + new Vector3(offset * multX, area.GetSizeY() * 0.5f * multY, 0.0f);
			if (!area.IsOverlapping(pos)) {
				pos = cornerPos + new Vector3(area.GetSizeX() * 0.5f * multX, offset * multY, 0.0f);
				if (!area.IsOverlapping(pos)) {
					pos = cornerPos + new Vector3(area.GetSizeX() * 0.5f * multX, area.GetSizeY() * 0.5f * multY, 0.0f);
					if (!area.IsOverlapping(pos)) {
						AssertMsg(false, $"A Hiding Spot can't be placed on its area at ({cornerPos.X}, {cornerPos.Y}, {cornerPos.Z})");

						pos = cornerPos + new Vector3(1.0f * multX, 1.0f * multY, 0.0f);
						if (!area.IsOverlapping(pos))
							pos = cornerPos;
					}
				}
			}
		}

		return pos;
	}

	struct HidingSpotExtent
	{
		public float Lo;
		public float Hi;
	}

	public void ComputeHidingSpots() {
		HidingSpotExtent extent = new();

		HidingSpots.Clear();

		if ((GetAttributes() & NavAttributeType.Crouch) != 0)
			return;

		if ((GetAttributes() & NavAttributeType.DontHide) != 0)
			return;

		int[] cornerCount = new int[(int)NavCornerType.NumCorners];
		for (int i = 0; i < (int)NavCornerType.NumCorners; ++i)
			cornerCount[i] = 0;

		const float cornerSize = 20.0f;

		for (int d = 0; d < (int)NavDirType.NumDirections; ++d) {
			extent.Lo = 999999.9f;
			extent.Hi = -999999.9f;

			bool isHoriz = d == (int)NavDirType.North || d == (int)NavDirType.South;

			for (int it = 0; it < Connect[d].Count; it++) {
				NavConnect connect = Connect[d][it];

				if (connect.Area!.IsConnected(this, OppositeDirection((NavDirType)d)) == false)
					continue;

				if ((connect.Area.GetAttributes() & NavAttributeType.Jump) != 0)
					continue;

				if (isHoriz) {
					if (connect.Area.NWCorner.X < extent.Lo)
						extent.Lo = connect.Area.NWCorner.X;

					if (connect.Area.SECorner.X > extent.Hi)
						extent.Hi = connect.Area.SECorner.X;
				}
				else {
					if (connect.Area.NWCorner.Y < extent.Lo)
						extent.Lo = connect.Area.NWCorner.Y;

					if (connect.Area.SECorner.Y > extent.Hi)
						extent.Hi = connect.Area.SECorner.Y;
				}
			}

			switch (d) {
				case (int)NavDirType.North:
					if (extent.Lo - NWCorner.X >= cornerSize)
						++cornerCount[(int)NavCornerType.NorthWest];

					if (SECorner.X - extent.Hi >= cornerSize)
						++cornerCount[(int)NavCornerType.NorthEast];
					break;

				case (int)NavDirType.South:
					if (extent.Lo - NWCorner.X >= cornerSize)
						++cornerCount[(int)NavCornerType.SouthWest];

					if (SECorner.X - extent.Hi >= cornerSize)
						++cornerCount[(int)NavCornerType.SouthEast];
					break;

				case (int)NavDirType.East:
					if (extent.Lo - NWCorner.Y >= cornerSize)
						++cornerCount[(int)NavCornerType.NorthEast];

					if (SECorner.Y - extent.Hi >= cornerSize)
						++cornerCount[(int)NavCornerType.SouthEast];
					break;

				case (int)NavDirType.West:
					if (extent.Lo - NWCorner.Y >= cornerSize)
						++cornerCount[(int)NavCornerType.NorthWest];

					if (SECorner.Y - extent.Hi >= cornerSize)
						++cornerCount[(int)NavCornerType.SouthWest];
					break;
			}
		}

		for (int c = 0; c < (int)NavCornerType.NumCorners; ++c) {
			if (cornerCount[c] == 2) {
				Vector3 pos = FindPositionInArea(this, (NavCornerType)c);
				if (c == 0 || !IsHidingSpotCollision(pos)) {
					HidingSpot spot = NavMesh.Instance!.CreateHidingSpot();
					spot.SetPosition(pos);
					spot.SetFlags(IsHidingSpotInCover(pos) ? HidingSpotFlags.InCover : HidingSpotFlags.Exposed);
					HidingSpots.Add(spot);
				}
			}
		}
	}

	void ClassifySniperSpot(HidingSpot spot) {
		Vector3 eye = spot.GetPosition();

		NavArea? hidingArea = NavMesh.Instance!.GetNavArea(spot.GetPosition());
		if (hidingArea != null && (hidingArea.GetAttributes() & NavAttributeType.Stand) != 0) {
			eye.Z += HumanEyeHeight;
		}
		else {
			eye.Z += HumanCrouchEyeHeight;
		}

		Vector3 walkable = default;

		Extent sniperExtent = default;
		float farthestRangeSq = 0.0f;
		const float minSniperRangeSq = 1000.0f * 1000.0f;
		bool found = false;

		sniperExtent.Lo = Vector3.Zero;
		sniperExtent.Hi = Vector3.Zero;

		Extent areaExtent = default;
		for (int it = 0; it < TheNavAreas.Count; it++) {
			NavArea area = TheNavAreas[it];

			area.GetExtent(ref areaExtent);

			for (walkable.Y = areaExtent.Lo.Y + GenerationStepSize / 2.0f; walkable.Y < areaExtent.Hi.Y; walkable.Y += GenerationStepSize) {
				for (walkable.X = areaExtent.Lo.X + GenerationStepSize / 2.0f; walkable.X < areaExtent.Hi.X; walkable.X += GenerationStepSize) {
					walkable.Z = area.GetZ(walkable) + HalfHumanHeight;

					Util.TraceLine(eye, walkable, (Mask)(Contents.Solid | Contents.Moveable | Contents.PlayerClip), null, CollisionGroup.None, out Trace result);

					if (result.Fraction == 1.0f && !result.StartSolid) {
						float rangeSq = (eye - walkable).LengthSquared();
						if (rangeSq > farthestRangeSq) {
							farthestRangeSq = rangeSq;

							if (rangeSq >= minSniperRangeSq) {
								if (found) {
									if (walkable.X < sniperExtent.Lo.X)
										sniperExtent.Lo.X = walkable.X;
									if (walkable.X > sniperExtent.Hi.X)
										sniperExtent.Hi.X = walkable.X;

									if (walkable.Y < sniperExtent.Lo.Y)
										sniperExtent.Lo.Y = walkable.Y;
									if (walkable.Y > sniperExtent.Hi.Y)
										sniperExtent.Hi.Y = walkable.Y;
								}
								else {
									sniperExtent.Lo = walkable;
									sniperExtent.Hi = walkable;
									found = true;
								}
							}
						}
					}
				}
			}
		}

		if (found) {
			float snipableArea = sniperExtent.Area();

			const float minIdealSniperArea = 200.0f * 200.0f;
			const float longSniperRangeSq = 1500.0f * 1500.0f;

			if (snipableArea >= minIdealSniperArea || farthestRangeSq >= longSniperRangeSq)
				spot.Flags |= (byte)HidingSpotFlags.IdealSniperSpot;
			else
				spot.Flags |= (byte)HidingSpotFlags.GoodSniperSpot;
		}
	}

	public void ComputeSniperSpots() {
		if (nav_quicksave.GetBool())
			return;

		foreach (HidingSpot spot in HidingSpots)
			ClassifySniperSpot(spot);
	}

	SpotEncounter GetSpotEncounter(NavArea from, NavArea to) {
		throw new NotImplementedException();
	}

	void AddSpotEncounters(NavArea from, NavDirType fromDir, NavArea to, NavDirType toDir) {
		SpotEncounter e = new();

		e.From.Area = from;
		e.FromDir = fromDir;

		e.To.Area = to;
		e.ToDir = toDir;

		ComputePortal(to, toDir, ref e.Path.To, out float halfWidth);
		ComputePortal(from, fromDir, ref e.Path.From, out halfWidth);

		const float eyeHeight = HumanEyeHeight;
		e.Path.From.Z = from.GetZ(e.Path.From) + eyeHeight;
		e.Path.To.Z = to.GetZ(e.Path.To) + eyeHeight;

		Vector3 dir = e.Path.To - e.Path.From;
		float length = dir.NormalizeInPlace();

		HidingSpot.ChangeMasterMarker();

		const float stepSize = 25.0f;
		const float seeSpotRange = 2000.0f;

		Vector3 eye, delta;
		HidingSpot spot;
		SpotOrder spotOrder = default;

		bool done = false;
		for (float along = 0.0f; !done; along += stepSize) {
			if (along >= length) {
				along = length;
				done = true;
			}

			eye = e.Path.From + along * dir;

			for (int it = 0; it < HidingSpot.TheHidingSpots.Count; it++) {
				spot = HidingSpot.TheHidingSpots[it];

				if (!spot.HasGoodCover())
					continue;

				if (spot.IsMarked())
					continue;

				Vector3 spotPos = spot.GetPosition();

				delta.X = spotPos.X - eye.X;
				delta.Y = spotPos.Y - eye.Y;
				delta.Z = spotPos.Z + eyeHeight - eye.Z;

				if (delta.LengthSquared() > seeSpotRange * seeSpotRange)
					continue;

				Util.TraceLine(eye, new Vector3(spotPos.X, spotPos.Y, spotPos.Z + eyeHeight), Mask.NPCSolidBrushOnly, null, CollisionGroup.None, out Trace result);
				if (result.Fraction != 1.0f)
					continue;

				delta.NormalizeInPlace();
				float dot = MathLib.DotProduct(dir, delta);
				if (dot < 0.7071f && dot > -0.7071f) {
					if (along > 0.0f) {
						spotOrder.Spot = spot;
						spotOrder.T = along / length;
						e.Spots.Add(spotOrder);
					}
				}

				spot.Mark();
			}
		}

		SpotEncounters.Add(e);
	}

	public void ComputeSpotEncounters() {
		SpotEncounters.Clear();

		for (int fromDir = 0; fromDir < (int)NavDirType.NumDirections; fromDir++) {
			foreach (NavConnect fromConnect in Connect[fromDir]) {
				for (int toDir = 0; toDir < (int)NavDirType.NumDirections; toDir++) {
					foreach (NavConnect toConnect in Connect[toDir]) {
						if (fromConnect.Area == toConnect.Area)
							continue;

						AddSpotEncounters(fromConnect.Area!, (NavDirType)fromDir, toConnect.Area!, (NavDirType)toDir);
					}
				}
			}
		}
	}

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

	public static void MakeNewMarker() {
		++MasterMarker;
		if (MasterMarker == 0)
			MasterMarker = 1;
	}

	public void Mark() => Marker = MasterMarker;

	public bool IsMarked() => Marker == MasterMarker;

	public void SetParent(NavArea? parent, NavTraverseType how = NavTraverseType.NumTraverseTypes) {
		Parent = parent;
		ParentHow = how;
	}

	public NavArea? GetParent() => Parent;

	public NavTraverseType GetParentHow() => ParentHow;

	public bool ComputeLighting() {
		if (engine.IsDedicatedServer()) {
			for (int i = 0; i < (int)NavCornerType.NumCorners; i++)
				LightIntensity[i] = 1.0f;

			return true;
		}

		for (int i = 0; i < (int)NavCornerType.NumCorners; i++) {
			Vector3 pos = FindPositionInArea(this, (NavCornerType)i);
			pos.Z = GetZ(pos.X, pos.Y) + HalfHumanHeight - StepHeight;

			if (NavMesh.Instance!.GetGroundHeight(pos, out float height))
				pos.Z = height + HalfHumanHeight - StepHeight;

			Vector3 light = Vector3.Zero;
			Vector3 ambient = Vector3.Zero;

			{
				return false;
			}

			float amientIntensity = ambient.X + ambient.Y + ambient.Z;
			float lightIntensity = light.X + light.Y + light.Z;
			lightIntensity = Math.Clamp(lightIntensity, 0.0f, 1.0f);
			lightIntensity = Math.Max(lightIntensity, amientIntensity);

			LightIntensity[i] = lightIntensity;
		}

		return true;
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

	public void MarkObstacleToAvoid(float obstructionHeight) {
		if (AvoidanceObstacleHeight < obstructionHeight) {
			if (AvoidanceObstacleHeight == 0)
				NavMesh.Instance!.OnAvoidanceObstacleEnteredArea(this);

			AvoidanceObstacleHeight = obstructionHeight;
		}
	}

	public void UpdateAvoidanceObstacles() {
		if (!AvoidanceObstacleTimer.IsElapsed())
			return;

		const float MaxBlockedCheckInterval = 5;
		float interval = (float)(BlockedTimer.GetCountdownDuration() + 1);
		if (interval > MaxBlockedCheckInterval)
			interval = MaxBlockedCheckInterval;

		AvoidanceObstacleTimer.Start(interval);

		Vector3 mins = NWCorner;
		Vector3 maxs = SECorner;

		mins.Z = Math.Min(NWCorner.Z, SECorner.Z);
		maxs.Z = Math.Max(NWCorner.Z, SECorner.Z) + HumanCrouchHeight;

		float obstructionHeight = 0.0f;
		for (int i = 0; i < NavMesh.Instance!.GetObstructions().Count; ++i) {
			INavAvoidanceObstacle obstruction = NavMesh.Instance!.GetObstructions()[i];
			BaseEntity obstructingEntity = obstruction.GetObstructingEntity();
			if (obstructingEntity == null)
				continue;

			obstructingEntity.CollisionProp().WorldSpaceSurroundingBounds(out AngularImpulse vecSurroundMins, out AngularImpulse vecSurroundMaxs);
			if (!CollisionUtils.IsBoxIntersectingBox(mins, maxs, vecSurroundMins, vecSurroundMaxs))
				continue;

			if (!obstruction.CanObstructNavAreas())
				continue;

			float propHeight = obstruction.GetNavObstructionHeight();

			obstructionHeight = Math.Max(obstructionHeight, propHeight);
		}

		AvoidanceObstacleHeight = obstructionHeight;

		if (AvoidanceObstacleHeight == 0.0f)
			NavMesh.Instance!.OnAvoidanceObstacleLeftArea(this);
	}

	void ClearAllNavCostEntities() {
		RemoveAttributes(NavAttributeType.FuncCost);
		FuncNavCostVector.Clear();
	}

	void AddFuncNavCostEntity(FuncNavCost cost) {
		SetAttributes(NavAttributeType.FuncCost);
		FuncNavCostVector.Add(cost);
	}

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

	static readonly byte[] PVS = new byte[PAD_NUMBER(BSPFileCommon.MAX_MAP_CLUSTERS, 8) / 8];

	public VisibilityType ComputeVisibility(NavArea area, bool isPVSValid, bool checkPVS, out bool outsidePVS) {
		outsidePVS = false;
		float distanceSq = (area.GetCenter() - GetCenter()).LengthSquared();

		if (nav_max_view_distance.GetFloat() > 0.00001f) {
			if (distanceSq > nav_max_view_distance.GetFloat() * nav_max_view_distance.GetFloat())
				return VisibilityType.NotVisible;
		}

		if (!isPVSValid)
			SetupPVS();

		Vector3 eye = new(0, 0, 0.75f * HumanHeight);

		if (checkPVS) {
			Extent areaExtent = new();
			areaExtent.Lo = areaExtent.Hi = area.GetCenter() + eye;
			areaExtent.Encompass(area.GetCorner(NavCornerType.NorthWest) + eye);
			areaExtent.Encompass(area.GetCorner(NavCornerType.NorthEast) + eye);
			areaExtent.Encompass(area.GetCorner(NavCornerType.SouthWest) + eye);
			areaExtent.Encompass(area.GetCorner(NavCornerType.SouthEast) + eye);

			if (!engine.CheckBoxInPVS(areaExtent.Lo, areaExtent.Hi, PVS)) {
				outsidePVS = true;
				return VisibilityType.NotVisible;
			}

			outsidePVS = false;
		}

		Vector3 thisNW = GetCorner(NavCornerType.NorthWest) + eye;
		Vector3 thisNE = GetCorner(NavCornerType.NorthEast) + eye;
		Vector3 thisSW = GetCorner(NavCornerType.SouthWest) + eye;
		Vector3 thisSE = GetCorner(NavCornerType.SouthEast) + eye;
		Vector3 thisCenter = GetCenter() + eye;

		Vector3 traceMins = thisNW;
		Vector3 traceMaxs = thisSE;

		traceMins.Z = Math.Min(Math.Min(Math.Min(thisNW.Z, thisNE.Z), thisSE.Z), thisSW.Z);
		traceMaxs.Z = Math.Max(Math.Max(Math.Max(thisNW.Z, thisNE.Z), thisSE.Z), thisSW.Z) + 0.1f;

		traceMins -= thisCenter;
		traceMaxs -= thisCenter;

		Vector3 vOtherMins = area.GetCorner(NavCornerType.NorthWest);
		Vector3 vOtherMaxs = area.GetCorner(NavCornerType.SouthEast);

		MathLib.CalcClosestPointOnAABB(vOtherMins, vOtherMaxs, thisCenter, out Vector3 target);
		target.Z = area.GetZ(target) + eye.Z;

		TraceFilterNoNPCsOrPlayer traceFilter = new(null, CollisionGroup.None);

		Util.TraceHull(thisCenter, target, traceMins, traceMaxs, Mask.BlockLOS | ((Mask)Contents.IgnoreNoDrawOpaque), ref traceFilter, out Trace tr);

		if (tr.Fraction == 1.0f || (tr.EndPos.X > vOtherMins.X && tr.EndPos.X < vOtherMaxs.X && tr.EndPos.Y > vOtherMins.Y && tr.EndPos.Y < vOtherMaxs.Y))
			return VisibilityType.CompletelyVisible;

		byte vis = (byte)VisibilityType.CompletelyVisible;

		float margin = GenerationStepSize / 2.0f;
		Vector3 shift = new(0, 0, 0.75f * HumanHeight);

		if (area.IsPartiallyVisible(GetCenter() + eye))
			vis |= (byte)VisibilityType.PotentiallyVisible;
		else
			vis = (byte)(vis & ~(byte)VisibilityType.CompletelyVisible);

		Vector3 eyeToCenter = GetCenter() - area.GetCenter();
		eyeToCenter = Vector3.Normalize(eyeToCenter);

		float angleTolerance = nav_potentially_visible_dot_tolerance.GetFloat();

		for (shift.Y = margin; shift.Y <= GetSizeY() - margin; shift.Y += GenerationStepSize) {
			for (shift.X = margin; shift.X <= GetSizeX() - margin; shift.X += GenerationStepSize) {
				if (vis == (byte)VisibilityType.PotentiallyVisible)
					return VisibilityType.PotentiallyVisible;

				Vector3 testPos = GetCorner(NavCornerType.NorthWest) + shift;
				testPos.Z = GetZ(testPos) + eye.Z;

				if (distanceSq > 1000 * 1000) {
					Vector3 eyeToCorner = testPos - (GetCenter() + eye);
					eyeToCorner = Vector3.Normalize(eyeToCorner);
					if (Vector3.Dot(eyeToCorner, eyeToCenter) >= angleTolerance)
						continue;
				}

				if (area.IsPartiallyVisible(testPos))
					vis |= (byte)VisibilityType.PotentiallyVisible;
				else
					vis = (byte)(vis & ~(byte)VisibilityType.CompletelyVisible);
			}
		}

		return (VisibilityType)vis;
	}

	List<AreaBindInfo> ComputeVisibilityDelta(NavArea other) {
		throw new NotImplementedException();
	}

	void ResetPotentiallyVisibleAreas() { }

	[Flags]
	public enum VisibilityType : byte
	{
		NotVisible,
		PotentiallyVisible,
		CompletelyVisible
	}

	void ComputeVisToArea(NavArea currentArea, NavArea otherArea) {
		NavArea area = otherArea;
		VisibilityType visThisToOther = (area == currentArea) ? VisibilityType.CompletelyVisible : VisibilityType.NotVisible;
		VisibilityType visOtherToThis = VisibilityType.NotVisible;

		if (area != currentArea) {
			visOtherToThis = currentArea.ComputeVisibility(area, true, true, out bool outsidePVS);

			if (!outsidePVS &&
					(visOtherToThis != VisibilityType.NotVisible ||
					 (currentArea.GetCenter() - area.GetCenter()).LengthSquared() < nav_max_view_distance.GetFloat() * nav_max_view_distance.GetFloat())) {
				visThisToOther = area.ComputeVisibility(currentArea, true, false, out _);
			}

			if (visOtherToThis == VisibilityType.NotVisible && visThisToOther != VisibilityType.NotVisible)
				visOtherToThis = VisibilityType.PotentiallyVisible;

			if (visThisToOther == VisibilityType.NotVisible && visOtherToThis != VisibilityType.NotVisible)
				visThisToOther = VisibilityType.PotentiallyVisible;
		}

		AreaBindInfo info = new();

		if (visThisToOther != VisibilityType.NotVisible) {
			info.Area = area;
			info.Attributes = (byte)visThisToOther;
			g_ComputedVis.Add(info);
		}

		if (visOtherToThis != VisibilityType.NotVisible) {
			info.Area = currentArea;
			info.Attributes = (byte)visOtherToThis;
			g_ComputedVis.Add(info);
		}
	}

	ConcurrentBag<AreaBindInfo> g_ComputedVis = [];
	HashSet<NavVisPair_t> g_NavVisPairHash = new(new VisPairHashFuncs());
	public void ComputeVisibilityToMesh() {
		InheritVisibilityFrom.Area = null;
		IsInheritedFrom = false;

		NavAreaCollector collector = new();

		float radius = nav_max_view_distance.GetFloat();
		if (radius == 0.0f)
			radius = 1500.0f;

		collector.Areas.EnsureCapacity(1000);
		NavMesh.Instance!.ForAllAreasInRadius(collector.Invoke, GetCenter(), radius);

		NavVisPair_t visPair = new();

		for (int i = collector.Areas.Count - 1; i >= 0; --i) {
			visPair.SetPair(this, collector.Areas[i]);

			if (g_NavVisPairHash.Contains(visPair))
				collector.Areas.RemoveAt(i);
		}

		SetupPVS();

		Parallel.ForEach(collector.Areas, area => ComputeVisToArea(this, area));

		PotentiallyVisibleAreas.EnsureCapacity(g_ComputedVis.Count);

		while (g_ComputedVis.TryTake(out AreaBindInfo info))
			PotentiallyVisibleAreas.Add(info);

		for (int i = 0; i < collector.Areas.Count; i++) {
			visPair.SetPair(this, collector.Areas[i]);
			Assert(!g_NavVisPairHash.Contains(visPair));
			g_NavVisPairHash.Add(visPair);
		}
	}

	bool IsEntirelyVisible(Vector3 eye, BaseEntity? ignore = null) {
		throw new NotImplementedException();
	}

	public bool IsPartiallyVisible(Vector3 eye, BaseEntity? ignore = null) {
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

	List<Handle<FuncNavPrerequisite>> GetPrerequisiteVector() {
		throw new NotImplementedException();
	}

	void RemoveAllPrerequisites() { }

	void AddPrerequisite(FuncNavPrerequisite prereq) {
		throw new NotImplementedException();
	}

	float GetDangerDecayRate() {
		throw new NotImplementedException();
	}

	public float GetSizeX() => SECorner.X - NWCorner.X;
	public float GetSizeY() => SECorner.Y - NWCorner.Y;

	bool IsDegenerate() {
		throw new NotImplementedException();
	}

	public int GetAdjacentCount(NavDirType dir) => Connect[(int)dir].Count;

	public NavArea GetAdjacentArea(NavDirType dir, int i) {
		throw new NotImplementedException();
	}

	public bool IsOpen() {
		throw new NotImplementedException();
	}

	public static bool IsOpenListEmpty() {
		throw new NotImplementedException();
	}

	public static NavArea PopOpenList() {
		throw new NotImplementedException();
	}

	public bool IsClosed() {
		throw new NotImplementedException();
	}

	public void AddToClosedList() => Mark();

	public void RemoveFromClosedList() { }

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
			NavCornerType.NorthEast => new(SECorner.X, NWCorner.Y, NEZ),
			NavCornerType.SouthEast => SECorner,
			NavCornerType.SouthWest => new(NWCorner.X, SECorner.Y, SWZ),
			_ => throw new ArgumentOutOfRangeException(nameof(corner), corner, null)
		};
	}
}
