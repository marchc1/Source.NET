using System.Net.NetworkInformation;
using System.Numerics;

namespace Game.Server.NavMesh;

public class NavNode
{
	Vector3 Pos;
	Vector3 Normal;

	readonly NavNode[] To = new NavNode[(int)NavDirType.NumDirections];
	readonly NavNode[] Connections = new NavNode[(int)NavDirType.NumDirections];
	readonly float[] ObstacleHeight = new float[(int)NavDirType.NumDirections];
	readonly float[] ObstacleStartDist = new float[(int)NavDirType.NumDirections];
	readonly float[] ObstacleEndDist = new float[(int)NavDirType.NumDirections];
	readonly float[] GroundHeightAboveNode = new float[(int)NavCornerType.NumCorners];
	readonly bool[] IsBlocked = new bool[(int)NavCornerType.NumCorners];
	readonly bool[] Crouch = new bool[(int)NavCornerType.NumCorners];

	NavNode? Next;
	NavNode? NextAtXY;
	NavNode Parent;
	NavArea? Area;

	uint ID;
	int AttributeFlags;
	byte Visited;
	bool IsCovered;
	bool IsOnDisplacement;

	static NavNode? List;
	static uint ListLength;
	static uint NextID;

	static Dictionary<Vector2, NavNode>? NavNodeHash = new(16 * 1024);

	public NavNode(Vector3 pos, Vector3 normal, NavNode parent, bool isOnDisplacement) {
		Pos = pos;
		Normal = normal;

		ID = NextID++;

		int i;
		for (i = 0; i < (int)NavDirType.NumDirections; i++) {
			To[i] = null!;
			ObstacleHeight[i] = 0;
			ObstacleStartDist[i] = 0;
			ObstacleEndDist[i] = 0;
		}

		for (i = 0; i < (int)NavCornerType.NumCorners; i++) {
			Crouch[i] = false;
			IsBlocked[i] = false;
		}

		Visited = 0;
		Parent = parent;

		Next = List;
		List = this;
		ListLength++;

		IsCovered = false;
		Area = null;

		AttributeFlags = 0;

		IsOnDisplacement = isOnDisplacement;

		Vector2 key = Pos.AsVector2();
		if (NavNodeHash!.TryGetValue(key, out NavNode? existingNode)) {
			NextAtXY = existingNode;
			NavNodeHash[key] = this;
		}
		else {
			NextAtXY = null;
			NavNodeHash.Add(key, this);
		}
	}

	public static void CleanupGeneration() {
		NavNodeHash = null;

		NavNode? node = List;
		while (node != null) {
			NavNode? next = node.Next;
			node.Next = null;
			node = next;
		}

		List = null;
		ListLength = 0;
		NextID = 1;
	}

	void Draw() { }

	float GetGroundHeightAboveNode(NavCornerType cornerType) {
		if (cornerType >= 0 && cornerType < NavCornerType.NumCorners)
			return GroundHeightAboveNode[(int)cornerType];

		float blockedHeight = 0.0f;
		for (int i = 0; i < (int)NavCornerType.NumCorners; i++)
			blockedHeight = Math.Max(blockedHeight, GroundHeightAboveNode[i]);

		return blockedHeight;
	}

	bool TestForCrouchArea(NavCornerType cornerNum, Vector3 mins, Vector3 maxs, float groundHeightAboveNode) {
		throw new NotImplementedException();
	}

	void CheckCrouch() { }

	void ConnectTo(NavNode node, NavDirType dir, float obstacleHeight, float obstacleStartDist, float obstacleEndDist) { }

	NavNode GetNode(Vector3 pos) {
		throw new NotImplementedException();
	}

	bool IsBiLinked(NavDirType dir) {
		throw new NotImplementedException();
	}

	bool IsClosedCell() {
		throw new NotImplementedException();
	}

	NavNode GetConnectedNode(NavDirType dir) => To[(int)dir];
	Vector3 GetPosition() => Pos;
	Vector3 GetNormal() => Normal;
	uint GetID() => ID;
	static NavNode GetFirst() => List;
	static uint GetListLength() => ListLength;
	NavNode GetNext() => Next;
	NavNode GetParent() => Parent;
	void MarkAsVisited(NavDirType dir) => Visited |= (byte)(1 << (int)dir);
	bool HasVisited(NavDirType dir) => (Visited & (1 << (int)dir)) != 0;
	void AssignArea(NavArea area) => Area = area;
	NavArea GetArea() => Area;
	bool IsBlockedInAnyDirection() => IsBlocked[(int)NavCornerType.NorthEast] || IsBlocked[(int)NavCornerType.NorthWest] || IsBlocked[(int)NavCornerType.SouthEast] || IsBlocked[(int)NavCornerType.SouthWest];
}

public class NavNodeHashComparer : IEqualityComparer<NavNode>
{
	public bool Equals(NavNode? x, NavNode? y) {
		if (ReferenceEquals(x, y)) return true;
		if (x is null || y is null) return false;

		// return x.GetPosition().AsVector2D() == y.GetPosition().AsVector2D();
		throw new NotImplementedException();
	}

	public int GetHashCode(NavNode obj) {
		// var v = obj.GetPosition().AsVector2D();
		// return HashCode.Combine(v.X, v.Y);
		throw new NotImplementedException();
	}
}
