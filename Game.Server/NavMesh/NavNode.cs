using static Game.Server.NavMesh.Nav;

using System.Numerics;

using Source;
using Source.Common.Formats.BSP;

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
	public readonly float[] ObstacleHeight = new float[(int)NavDirType.NumDirections];
	public readonly float[] ObstacleStartDist = new float[(int)NavDirType.NumDirections];
	public readonly float[] ObstacleEndDist = new float[(int)NavDirType.NumDirections];
	readonly float[] GroundHeightAboveNode = new float[(int)NavCornerType.NumCorners];
	public readonly bool[] IsBlocked = new bool[(int)NavCornerType.NumCorners];
	public readonly bool[] Crouch = new bool[(int)NavCornerType.NumCorners];

	NavNode? Next;
	NavNode? NextAtXY;
	NavNode? Parent;
	NavArea? Area;

	uint ID;
	NavAttributeType AttributeFlags;
	byte Visited;
	public bool IsCovered;
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

	public void Draw() {
		if (!nav_show_nodes.GetBool())
			return;

		int r = 0, g = 0, b = 0;

		if (IsCovered) {
			if ((GetAttributes() & NavAttributeType.Crouch) != 0)
				b = 255;
			else
				r = 255;
		}
		else {
			if ((GetAttributes() & NavAttributeType.Crouch) != 0)
				b = 255;
			g = 255;
		}

		Shared.DebugOverlay.Cross3D(Pos, 2, r, g, b, true, Shared.DebugOverlay.Persist);

		if ((!IsCovered && nav_show_node_id.GetBool()) || (IsCovered && nav_show_node_id.GetInt() < 0))
			Shared.DebugOverlay.Text(Pos, $"{ID}", true, Shared.DebugOverlay.Persist);

		if ((uint)nav_test_node.GetInt() == ID) {
			NavMesh.Instance!.TestArea(this, 1, 1);
			nav_test_node.SetValue(0);
		}

		if ((uint)nav_test_node_crouch.GetInt() == ID) {
			CheckCrouch();
			nav_test_node_crouch.SetValue(0);
		}

		if ((GetAttributes() & NavAttributeType.Crouch) != 0) {
			for (int i = 0; i < (int)NavCornerType.NumCorners; i++) {
				if (IsBlocked[i] || Crouch[i]) {
					Vector2 cornerVec = default;
					CornerToVector2D((NavCornerType)i, ref cornerVec);

					const float scale = 3.0f;
					Vector3 scaled = new(cornerVec.X * scale, cornerVec.Y * scale, 0);

					if (IsBlocked[i])
						Shared.DebugOverlay.HorzArrow(Pos, Pos + scaled, 0.5f, 255, 0, 0, 255, true, Shared.DebugOverlay.Persist);
					else
						Shared.DebugOverlay.HorzArrow(Pos, Pos + scaled, 0.5f, 0, 0, 255, 255, true, Shared.DebugOverlay.Persist);
				}
			}
		}

		if (nav_show_node_grid.GetBool()) {
			for (int i = 0; i < (int)NavDirType.NumDirections; i++) {
				NavNode? nodeNext = GetConnectedNode((NavDirType)i);
				if (nodeNext != null) {
					Shared.DebugOverlay.Line(Pos, nodeNext.Pos, 255, 255, 0, false, Shared.DebugOverlay.Persist);

					float obstacleHeight = ObstacleHeight[i];
					if (obstacleHeight > 0) {
						float z = Pos.Z + obstacleHeight;
						Vector3 from = Pos;
						Vector3 to = from;
						AddDirectionVector(ref to, (NavDirType)i, ObstacleStartDist[i]);
						Shared.DebugOverlay.Line(from, to, 255, 0, 255, false, Shared.DebugOverlay.Persist);
						from = to;
						to.Z = z;
						Shared.DebugOverlay.Line(from, to, 255, 0, 255, false, Shared.DebugOverlay.Persist);
						from = to;
						to = Pos;
						to.Z = z;
						AddDirectionVector(ref to, (NavDirType)i, ObstacleEndDist[i]);
						Shared.DebugOverlay.Line(from, to, 255, 0, 255, false, Shared.DebugOverlay.Persist);
					}
				}
			}
		}
	}

	public float GetGroundHeightAboveNode(NavCornerType cornerType) {
		if (cornerType >= 0 && cornerType < NavCornerType.NumCorners)
			return GroundHeightAboveNode[(int)cornerType];

		float blockedHeight = 0.0f;
		for (int i = 0; i < (int)NavCornerType.NumCorners; i++)
			blockedHeight = Math.Max(blockedHeight, GroundHeightAboveNode[i]);

		return blockedHeight;
	}

	bool TestForCrouchArea(NavCornerType cornerNum, Vector3 mins, Vector3 maxs, out float groundHeightAboveNode) {
		TraceFilterWalkableEntities filter = new(null, CollisionGroup.BreakableGlass, WalkThruFlags.Everything);

		Vector3 start = Pos;
		Vector3 end = start;
		end.Z += JumpCrouchHeight;

		Util.TraceHull(start, end, mins, maxs, Mask.NPCSolidBrushOnly, ref filter, out Trace tr);

		float maxHeight = tr.EndPos.Z - start.Z;

		Vector3 realMaxs = maxs;

		for (float height = 0; height <= maxHeight; height += 1.0f) {
			start = Pos;
			start.Z += height;

			realMaxs.Z = HalfHumanHeight;
			Util.TraceHull(start, start, mins, realMaxs, Mask.NPCSolidBrushOnly, ref filter, out tr);
			if (!tr.StartSolid) {
				groundHeightAboveNode = start.Z - Pos.Z;

				realMaxs.Z = HumanHeight;
				Util.TraceHull(start, start, mins, realMaxs, Mask.NPCSolidBrushOnly, ref filter, out tr);
				if (!tr.StartSolid) {
					if ((uint)nav_test_node_crouch.GetInt() == GetID())
						Shared.DebugOverlay.Box(start, mins, maxs, 0, 255, 255, 100, 100);
					return true;
				}

				if ((uint)nav_test_node_crouch.GetInt() == GetID())
					Shared.DebugOverlay.Box(start, mins, maxs, 255, 0, 0, 100, 100);

				return false;
			}
		}

		groundHeightAboveNode = JumpCrouchHeight;

		IsBlocked[(int)cornerNum] = true;
		return false;
	}

	public void CheckCrouch() {
		for (int i = 0; i < (int)NavCornerType.NumCorners; i++) {
			if (nav_test_node_crouch_dir.GetInt() != (int)NavCornerType.NumCorners && i != nav_test_node_crouch_dir.GetInt())
				continue;

			NavCornerType cornerType = (NavCornerType)i;
			Vector2 cornerVec = default;
			CornerToVector2D(cornerType, ref cornerVec);

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

			if (!TestForCrouchArea(cornerType, mins, maxs, out GroundHeightAboveNode[i])) {
				SetAttributes(NavAttributeType.Crouch);
				Crouch[i] = true;
			}
		}
	}

	public void ConnectTo(NavNode node, NavDirType dir, float obstacleHeight, float obstacleStartDist, float obstacleEndDist) {
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

	public bool IsClosedCell() => IsBiLinked(NavDirType.South)
			&& IsBiLinked(NavDirType.East)
			&& To[(int)NavDirType.East].IsBiLinked(NavDirType.South)
			&& To[(int)NavDirType.South].IsBiLinked(NavDirType.East)
			&& To[(int)NavDirType.East].To[(int)NavDirType.South] == To[(int)NavDirType.South].To[(int)NavDirType.East];

	public NavNode GetConnectedNode(NavDirType dir) => To[(int)dir];

	public Vector3 GetPosition() => Pos;

	public Vector3 GetNormal() => Normal;

	uint GetID() => ID;

	public static NavNode? GetFirst() => List;

	public static uint GetListLength() => ListLength;

	public NavNode? GetNext() => Next;

	public NavNode? GetParent() => Parent;

	public void MarkAsVisited(NavDirType dir) => Visited |= (byte)(1 << (int)dir);

	public bool HasVisited(NavDirType dir) => (Visited & (1 << (int)dir)) != 0;

	public void Cover() => IsCovered = true;

	bool IsNodeCovered() => IsCovered;

	public void AssignArea(NavArea area) => Area = area;

	public NavArea? GetArea() => Area;

	public void SetAttributes(NavAttributeType attr) => AttributeFlags |= attr;

	public NavAttributeType GetAttributes() => AttributeFlags;

	public bool IsBlockedInAnyDirection() => IsBlocked[(int)NavCornerType.NorthEast] || IsBlocked[(int)NavCornerType.NorthWest] || IsBlocked[(int)NavCornerType.SouthEast] || IsBlocked[(int)NavCornerType.SouthWest];

	public bool IsOnDisplacementSurface() => IsOnDisplacement;

	public override bool Equals(object? obj) {
		if (obj is NavNode other)
			return GetPosition().AsVector2() == other.GetPosition().AsVector2();

		return false;
	}
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
