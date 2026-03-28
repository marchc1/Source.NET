using static Game.Server.NavMesh.Nav;

using System.Numerics;

namespace Game.Server.NavMesh;

class NodeHashFuncs : IEqualityComparer<NavNode>
{
	public bool Equals(NavNode? lhs, NavNode? rhs) {
		if (lhs is null || rhs is null) return false;
		return lhs.GetPosition().AsVector2() == rhs.GetPosition().AsVector2();
	}

	public int GetHashCode(NavNode obj) {
		var v = obj.GetPosition().AsVector2();
		return Hash8(v);
	}

	private static int Hash8(Vector2 v) {
		unchecked {
			int hash = 17;
			hash = hash * 31 + v.X.GetHashCode();
			hash = hash * 31 + v.Y.GetHashCode();
			return hash;
		}
	}
}


public class NavNode
{
	Vector3 Pos;
	Vector3 Normal;

	static readonly NavDirType[] Opposite = [
		NavDirType.South,
		NavDirType.West,
		NavDirType.North,
		NavDirType.East
	];

	public static HashSet<NavNode>? g_NavNodeHash;

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
	NavNode? Parent;
	NavArea? Area;

	uint ID;
	NavAttributeType AttributeFlags;
	byte Visited;
	bool IsCovered;
	bool IsOnDisplacement;

	static NavNode? List;
	static uint ListLength;
	static uint NextID;

	public NavNode(Vector3 pos, Vector3 normal, NavNode? parent, bool isOnDisplacement) {
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

		g_NavNodeHash ??= new(new NodeHashFuncs());

		bool didInsert = g_NavNodeHash.Add(this);
		if (!didInsert) {
			NavNode existingNode = g_NavNodeHash.First(n => n.Equals(this));
			NextAtXY = existingNode;
			g_NavNodeHash.Remove(existingNode);
			g_NavNodeHash.Add(this);
		}
		else
			NextAtXY = null;
	}

	public static void CleanupGeneration() {
		g_NavNodeHash = null;

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

	void CheckCrouch() {
		for (int i = 0; i < (int)NavCornerType.NumCorners; i++) {
			NavCornerType cornerType = (NavCornerType)i;
			Vector2 cornerVec = CornerToVector2D(cornerType);

			Vector3 mins = new(0, 0, 0);
			Vector3 maxs = new(0, 0, 0);

			if (cornerVec.X < 0)
				mins.X = -HalfHumanHeight;
			else if (cornerVec.X > 0)
				maxs.X = HalfHumanHeight;

			if (cornerVec.Y < 0)
				mins.Y = -HalfHumanHeight;
			else if (cornerVec.Y > 0)
				maxs.Y = HalfHumanHeight;

			maxs.Z = HumanHeight;

			for (int j = 0; j < 3; j++) {
				if (mins[j] > maxs[j])
					(maxs[j], mins[j]) = (mins[j], maxs[j]);
			}

			if (!TestForCrouchArea(cornerType, mins, maxs, GroundHeightAboveNode[i])) {
				SetAttribute(NavAttributeType.Crouch);
				Crouch[i] = true;
			}
		}
	}

	void ConnectTo(NavNode node, NavDirType dir, float obstacleHeight, float obstacleStartDist, float obstacleEndDist) {
		Assert(obstacleStartDist >= 0 && obstacleStartDist <= GenerationStepSize);
		Assert(obstacleEndDist >= 0 && obstacleStartDist <= GenerationStepSize);
		Assert(obstacleStartDist < obstacleEndDist);

		To[(int)dir] = node;
		ObstacleHeight[(int)dir] = obstacleHeight;
		ObstacleStartDist[(int)dir] = obstacleStartDist;
		ObstacleEndDist[(int)dir] = obstacleEndDist;
	}

	private static readonly NavNode lookup = new NavNode(default, default, null, false);
	public static NavNode? GetNode(Vector3 pos) {
		const float tolerance = 0.45f * GenerationStepSize;

		NavNode? node = null;

		if (g_NavNodeHash != null) {
			lookup.Pos = pos;

			NavNode? existingNode = g_NavNodeHash.FirstOrDefault(n => n.Equals(lookup));
			if (existingNode != null) {
				for (node = existingNode; node != null; node = node.NextAtXY) {
					float dz = Math.Abs(node.Pos.Z - pos.Z);
					if (dz < tolerance)
						break;
				}
			}
		}

		return node;
	}

	bool IsBiLinked(NavDirType dir) => To[(int)dir] != null && To[(int)dir].To[(int)Opposite[(int)dir]] == this;

	bool IsClosedCell() => IsBiLinked(NavDirType.South)
			&& IsBiLinked(NavDirType.East)
			&& To[(int)NavDirType.East].IsBiLinked(NavDirType.South)
			&& To[(int)NavDirType.South].IsBiLinked(NavDirType.East)
			&& To[(int)NavDirType.East].To[(int)NavDirType.South] == To[(int)NavDirType.South].To[(int)NavDirType.East];

	public NavNode GetConnectedNode(NavDirType dir) => To[(int)dir];

	public Vector3 GetPosition() => Pos;

	Vector3 GetNormal() => Normal;

	uint GetID() => ID;

	static NavNode? GetFirst() => List;

	static uint GetListLength() => ListLength;

	NavNode? GetNext() => Next;

	public NavNode GetParent() => Parent;

	void MarkAsVisited(NavDirType dir) => Visited |= (byte)(1 << (int)dir);

	bool HasVisited(NavDirType dir) => (Visited & (1 << (int)dir)) != 0;

	void Cover() => IsCovered = true;

	bool IsNodeCovered() => IsCovered;

	public void AssignArea(NavArea area) => Area = area;

	NavArea? GetArea() => Area;

	public void SetAttribute(NavAttributeType attr) => AttributeFlags |= attr;

	public NavAttributeType GetAttributes() => AttributeFlags;

	bool IsBlockedInAnyDirection() => IsBlocked[(int)NavCornerType.NorthEast] || IsBlocked[(int)NavCornerType.NorthWest] || IsBlocked[(int)NavCornerType.SouthEast] || IsBlocked[(int)NavCornerType.SouthWest];

	bool IsOnDisplacementSurface() => IsOnDisplacement;
}

public class NavNodeHashComparer : IEqualityComparer<NavNode>
{
	public bool Equals(NavNode? x, NavNode? y) {
		if (ReferenceEquals(x, y)) return true;
		if (x is null || y is null) return false;
		return x.GetPosition().AsVector2() == y.GetPosition().AsVector2();
	}

	public int GetHashCode(NavNode obj) {
		Vector2 v = obj.GetPosition().AsVector2();
		return HashCode.Combine(v.X, v.Y);
	}
}
