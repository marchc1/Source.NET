using static Game.Server.NavMesh.NavGenerate;
using static Game.Server.NavMesh.Nav;

using Source.Common.Commands;

using System.Numerics;
using Source.Common;
using Source;
using Source.Common.Formats.Keyvalues;
using Source.Common.Mathematics;
using Source.Common.Formats.BSP;

namespace Game.Server.NavMesh;

static class NavGenerate
{
	public const int MAX_BLOCKED_AREAS = 256;

	public const float MaxObstacleAreaWidth = StepHeight;
	public const float MinObstacleAreaWidth = 10.0f;

	public static uint[] BlockedID = new uint[MAX_BLOCKED_AREAS];
	public static int BlockedIDCount = 0;

	public static double LastMsgTime = 0.0f;

	public static Vector3 NavTraceMins = new(-0.45f, -0.45f, 0);
	public static Vector3 NavTraceMaxs = new(0.45f, 0.45f, HumanCrouchHeight);
}

class ApproachAreaCost
{
	public float Invoke(NavArea area, NavArea? fromArea, NavLadder? ladder, FuncElevator? elevator) {
		for (int i = 0; i < BlockedIDCount; i++) {
			if (area.GetID() == BlockedID[i])
				return -1.0f;
		}

		if (fromArea == null)
			return 0.0f;

		float dist;

		if (ladder != null)
			dist = ladder.Length;
		else
			dist = (area.GetCenter() - fromArea.GetCenter()).Length();

		return dist + fromArea.GetCostSoFar();
	}
}

class JumpConnector
{
	struct Connection
	{
		public NavArea Source;
		public NavArea Dest;
		public NavDirType Dir;
	}

	public bool Invoke(NavArea jumpArea) {
		if (!nav_generate_jump_connections.GetBool())
			return true;

		if ((jumpArea.GetAttributes() & NavAttributeType.Jump) == 0)
			return true;

		for (int i = 0; i < (int)NavDirType.NumDirections; i++) {
			NavDirType incomingDir = (NavDirType)i;
			NavDirType outgoingDir = OppositeDirection(incomingDir);

			List<NavConnect> incoming = jumpArea.GetIncomingConnections(incomingDir);
			List<NavConnect> from = jumpArea.GetAdjacentAreas(incomingDir);
			List<NavConnect> dest = jumpArea.GetAdjacentAreas(outgoingDir);

			TryToConnect(jumpArea, incoming, dest, outgoingDir);
			TryToConnect(jumpArea, from, dest, outgoingDir);
		}

		return true;
	}

	void TryToConnect(NavArea jumpArea, List<NavConnect> source, List<NavConnect> dest, NavDirType outgoingDir) {
		foreach (NavConnect sourceConnect in source) {
			NavArea sourceArea = sourceConnect.Area!;
			if (!sourceArea.IsConnected(jumpArea, outgoingDir))
				continue;

			if ((sourceArea.GetAttributes() & NavAttributeType.Jump) != 0) {
				NavDirType incomingDir = OppositeDirection(outgoingDir);
				List<NavConnect> in1 = sourceArea.GetIncomingConnections(incomingDir);
				List<NavConnect> in2 = sourceArea.GetAdjacentAreas(incomingDir);

				TryToConnect(jumpArea, in1, dest, outgoingDir);
				TryToConnect(jumpArea, in2, dest, outgoingDir);

				continue;
			}

			TryToConnect(jumpArea, sourceArea, dest, outgoingDir);
		}
	}

	void TryToConnect(NavArea jumpArea, NavArea sourceArea, List<NavConnect> dest, NavDirType outgoingDir) {
		foreach (NavConnect destConnect in dest) {
			NavArea destArea = destConnect.Area!;

			if ((destArea.GetAttributes() & NavAttributeType.Jump) != 0)
				continue;

			Vector3 center = Vector3.Zero;
			sourceArea.ComputePortal(destArea, outgoingDir, ref center, out float halfWidth);

			if (halfWidth <= 0.0f)
				continue;

			Vector3 dir = Vector3.Zero;
			AddDirectionVector(ref dir, outgoingDir, 5.0f);

			sourceArea.GetClosestPointOnArea(ref center, out Vector3 sourcePos);
			destArea.GetClosestPointOnArea(ref center, out Vector3 destPos);

			if (sourceArea.HasAttributes(NavAttributeType.Stairs) && sourcePos.Z + StepHeight < destPos.Z)
				continue;

			if ((sourcePos - destPos).Length() < GenerationStepSize * 3)
				sourceArea.ConnectTo(destArea, outgoingDir);
		}
	}
}

class IncrementallyGeneratedAreas
{
	public bool Invoke(NavArea area) => area.HasNodes();
}

class AreaSet(List<NavArea> areas)
{
	readonly List<NavArea> Areas = areas;
	public bool Invoke(NavArea area) => Areas.Contains(area);
}

class TestOverlapping(Vector3 nw, Vector3 ne, Vector3 sw, Vector3 se)
{
	Vector3 NW = nw;
	Vector3 NE = ne;
	Vector3 SW = sw;
	Vector3 SE = se;

	private float GetZ(Vector3 pos) {
		float dx = SE.X - NW.X;
		float dy = SE.Y - NW.Y;

		if (dx == 0.0f || dy == 0.0f)
			return NE.Z;

		float u = (pos.X - NW.X) / dx;
		float v = (pos.Y - NW.Y) / dy;

		if (u < 0.0f)
			u = 0.0f;
		else if (u > 1.0f)
			u = 1.0f;

		if (v < 0.0f)
			v = 0.0f;
		else if (v > 1.0f)
			v = 1.0f;

		float northZ = NW.Z + u * (NE.Z - NW.Z);
		float southZ = SW.Z + u * (SE.Z - SW.Z);

		return northZ + v * (southZ - northZ);
	}

	public bool OverlapsExistingArea() {
		Vector3 nw = NW;
		Vector3 se = SE;
		Vector3 start = nw;

		start.X += GenerationStepSize / 2.0f;
		start.Y += GenerationStepSize / 2.0f;

		while (start.X < se.X) {
			start.Y = nw.Y + GenerationStepSize / 2.0f;
			while (start.Y < se.Y) {
				start.Z = GetZ(start);
				Vector3 end = start;
				start.Z -= StepHeight;
				end.Z += HalfHumanHeight;

				if (NavMesh.Instance!.FindNavAreaOrLadderAlongRay(start, end, out NavArea? overlappingArea, out _, null) && overlappingArea != null)
					return true;

				start.Y += GenerationStepSize;
			}

			start.X += GenerationStepSize;
		}

		return false;
	}
}

class Subdivider(int depth)
{
	int Depth = depth;

	public bool Invoke(NavArea area) {
		SubdivideX(area, true, true, Depth);
		return true;
	}

	public void SubdivideX(NavArea area, bool canDivideX, bool canDivideY, int depth) {
		if (!canDivideX || depth <= 0)
			return;

		float split = area.GetSizeX() / 2.0f;

		if (split < GenerationStepSize) {
			if (canDivideY)
				SubdivideY(area, false, canDivideY, depth);
			return;
		}

		split += area.GetCorner(NavCornerType.NorthWest).X;
		split = NavMesh.Instance!.SnapToGrid(split);

		if (area.SplitEdit(false, split, out NavArea alpha, out NavArea beta)) {
			SubdivideX(alpha, canDivideX, canDivideY, depth);
			SubdivideX(beta, canDivideX, canDivideY, depth);
		}
	}

	public void SubdivideY(NavArea area, bool canDivideX, bool canDivideY, int depth) {
		if (!canDivideY || depth <= 0)
			return;

		float split = area.GetSizeY() / 2.0f;

		if (split < GenerationStepSize) {
			if (canDivideX)
				SubdivideX(area, canDivideX, false, depth);
			return;
		}

		split += area.GetCorner(NavCornerType.NorthWest).Y;
		split = NavMesh.Instance!.SnapToGrid(split);

		if (area.SplitEdit(true, split, out NavArea alpha, out NavArea beta)) {
			SubdivideY(alpha, canDivideX, canDivideY, depth - 1);
			SubdivideY(beta, canDivideX, canDivideY, depth - 1);
		}
	}
}

public partial class NavMesh
{
	void BuildLadders() {
		DestroyLadders();
	}

	void CreateLadder(Vector3 absMin, Vector3 absMax, float maxHeightAboveTopArea) { }

	void CreateLadder(Vector3 top, Vector3 bottom, float width, Vector2 ladderDir, float maxHeightAboveTopArea) { }

	void MarkPlayerClipAreas() { }

	void MarkJumpAreas() { }

	void StichAndRemoveJumpAreas() { }

	void HandleObstacleTopAreas() { }

	void RaiseAreasWithInternalObstacles() { }

	void CreateObstacleTopAreas() { }

	bool CreateObstacleTopAreaIfNecessary(NavArea area, NavArea areaOther, NavDirType dir, bool multiNode) {
		throw new NotImplementedException();
	}

	void RemoveOverlappingObstacleTopAreas() { }

	void MarkStairAreas() { }

	void RemoveJumpAreas() { }

	public void CommandNavRemoveJumpAreas() { }

	void SquareUpAreas() { }

	void StitchGeneratedAreas() { }

	void StitchAreaSet(List<NavArea> areas) { }

	void StitchAreaIntoMesh(NavArea area, NavDirType dir, Func<NavArea, bool> func) {

	}

	public bool CheckCliff(Vector3 fromPos, NavDirType dir) {
		return false;

		Vector3 toPos = fromPos;
		AddDirectionVector(ref toPos, dir, GenerationStepSize);

		if (TraceAdjacentNode(0, fromPos, toPos, out Trace trace, DeathDrop * 10) && !trace.AllSolid && !trace.StartSolid) {
			float deltaZ = fromPos.Z - trace.EndPos.Z;
			if (deltaZ > CliffHeight)
				return true;

			if ((dir == NavDirType.South || dir == NavDirType.East) && Math.Abs(deltaZ) < StepHeight)
				return CheckCliff(trace.EndPos, dir);
		}

		return false;
	}

	void ConnectGeneratedAreas() { }

	void MergeGeneratedAreas() { }

	void FixUpGeneratedAreas() { }

	void FixConnections() { }

	void FixCornerOnCornerAreas() { }

	void SplitAreasUnderOverhangs() { }

	bool TestForValidJumpArea(NavNode node) {
		return true;

		NavNode? east = node.GetConnectedNode(NavDirType.East);
		NavNode? south = node.GetConnectedNode(NavDirType.South);
		if (east == null || south == null)
			return false;

		NavNode? southEast = east.GetConnectedNode(NavDirType.South);
		if (southEast == null)
			return false;

		if (!IsHeightDifferenceValid(
			node.GetPosition().Z,
			south.GetPosition().Z,
			southEast.GetPosition().Z,
			east.GetPosition().Z))
			return false;

		if (!IsHeightDifferenceValid(
			south.GetPosition().Z,
			node.GetPosition().Z,
			southEast.GetPosition().Z,
			east.GetPosition().Z))
			return false;

		if (!IsHeightDifferenceValid(
			southEast.GetPosition().Z,
			south.GetPosition().Z,
			node.GetPosition().Z,
			east.GetPosition().Z))
			return false;

		if (!IsHeightDifferenceValid(
			east.GetPosition().Z,
			south.GetPosition().Z,
			southEast.GetPosition().Z,
			node.GetPosition().Z))
			return false;

		return true;
	}

	bool TestForValidCrouchArea(NavNode node) {
		TraceFilterWalkableEntities filter = new(null, CollisionGroup.PlayerMovement, WalkThruFlags.Everything);
		Vector3 start = node.GetPosition();
		Vector3 end = node.GetPosition();
		end.Z += JumpCrouchHeight;

		Vector3 mins = new(0, 0, 0);
		Vector3 maxs = new(GenerationStepSize, GenerationStepSize, HumanCrouchHeight);

		Util.TraceHull(start, end, mins, maxs, GetGenerationTraceMask(), ref filter, out Trace tr);

		return !tr.AllSolid;
	}

	bool IsHeightDifferenceValid(float test, float other1, float other2, float other3) {
		const float CloseDelta = StepHeight / 2;
		if (Math.Abs(other1 - other2) > CloseDelta)
			return true;

		if (Math.Abs(other1 - other3) > CloseDelta)
			return true;

		if (Math.Abs(other2 - other3) > CloseDelta)
			return true;

		const float MaxDelta = StepHeight;
		if (Math.Abs(test - other1) > MaxDelta)
			return false;

		if (Math.Abs(test - other2) > MaxDelta)
			return false;

		if (Math.Abs(test - other3) > MaxDelta)
			return false;

		return true;
	}

	bool TestArea(NavNode node, int width, int height) {
		Vector3 normal = node.GetNormal();
		float d = -MathLib.DotProduct(normal, node.GetPosition());

		bool nodeCrouch = node.Crouch[(int)NavCornerType.SouthEast];

		if (node.IsBlocked[(int)NavCornerType.SouthEast])
			return false;

		int nodeAttributes = (int)(node.GetAttributes() & ~NavAttributeType.Crouch);

		const float offPlaneTolerance = 5.0f;

		NavNode vertNode, horizNode;

		vertNode = node;
		int x, y;
		for (y = 0; y < height; y++) {
			horizNode = vertNode;

			for (x = 0; x < width; x++) {
				bool horizNodeCrouch = false;
				bool westEdge = (x == 0);
				bool eastEdge = (x == width - 1);
				bool northEdge = (y == 0);
				bool southEdge = (y == height - 1);

				if (northEdge && westEdge) {
					horizNodeCrouch = horizNode.Crouch[(int)NavCornerType.SouthEast];
					if (horizNode.IsBlocked[(int)NavCornerType.SouthEast])
						return false;
				}
				else if (northEdge && eastEdge) {
					horizNodeCrouch = horizNode.Crouch[(int)NavCornerType.SouthEast] || horizNode.Crouch[(int)NavCornerType.SouthWest];
					if (horizNode.IsBlocked[(int)NavCornerType.SouthEast] || horizNode.IsBlocked[(int)NavCornerType.SouthWest])
						return false;
				}
				else if (southEdge && westEdge) {
					horizNodeCrouch = horizNode.Crouch[(int)NavCornerType.SouthEast] || horizNode.Crouch[(int)NavCornerType.NorthEast];
					if (horizNode.IsBlocked[(int)NavCornerType.SouthEast] || horizNode.IsBlocked[(int)NavCornerType.NorthEast])
						return false;
				}
				else if (southEdge && eastEdge) {
					horizNodeCrouch = (horizNode.GetAttributes() & NavAttributeType.Crouch) != 0;
					if (horizNode.IsBlockedInAnyDirection())
						return false;
				}
				else if (northEdge) {
					horizNodeCrouch = horizNode.Crouch[(int)NavCornerType.SouthEast] || horizNode.Crouch[(int)NavCornerType.SouthWest];
					if (horizNode.IsBlocked[(int)NavCornerType.SouthEast] || horizNode.IsBlocked[(int)NavCornerType.SouthWest])
						return false;
				}
				else if (southEdge) {
					horizNodeCrouch = (horizNode.GetAttributes() & NavAttributeType.Crouch) != 0;
					if (horizNode.IsBlockedInAnyDirection())
						return false;
				}
				else if (eastEdge) {
					horizNodeCrouch = (horizNode.GetAttributes() & NavAttributeType.Crouch) != 0;
					if (horizNode.IsBlockedInAnyDirection())
						return false;
				}
				else if (westEdge) {
					horizNodeCrouch = horizNode.Crouch[(int)NavCornerType.SouthEast] || horizNode.Crouch[(int)NavCornerType.NorthEast];
					if (horizNode.IsBlocked[(int)NavCornerType.SouthEast] || horizNode.IsBlocked[(int)NavCornerType.NorthEast])
						return false;
				}
				else {
					horizNodeCrouch = (horizNode.GetAttributes() & NavAttributeType.Crouch) != 0;
					if (horizNode.IsBlockedInAnyDirection())
						return false;
				}

				if (nodeCrouch != horizNodeCrouch)
					return false;

				int horizNodeAttributes = (int)(horizNode.GetAttributes() & ~NavAttributeType.Crouch);
				if (horizNodeAttributes != nodeAttributes)
					return false;

				if (horizNode.IsCovered)
					return false;

				if (!horizNode.IsClosedCell())
					return false;

				if (!CheckObstacles(horizNode, width, height, x, y))
					return false;

				horizNode = horizNode.GetConnectedNode(NavDirType.East);
				if (horizNode == null)
					return false;

				if (width > 1 || height > 1) {
					float dist = (float)Math.Abs(MathLib.DotProduct(horizNode.GetPosition(), normal) + d);
					if (dist > offPlaneTolerance)
						return false;
				}
			}

			if (!CheckObstacles(horizNode, width, height, x, y))
				return false;

			vertNode = vertNode.GetConnectedNode(NavDirType.South);
			if (vertNode == null)
				return false;

			if (width > 1 || height > 1) {
				float dist = (float)Math.Abs(MathLib.DotProduct(vertNode.GetPosition(), normal) + d);
				if (dist > offPlaneTolerance)
					return false;
			}
		}

		if (width > 1 || height > 1) {
			horizNode = vertNode;

			for (x = 0; x < width; x++) {
				if (!CheckObstacles(horizNode, width, height, x, y))
					return false;

				horizNode = horizNode.GetConnectedNode(NavDirType.East);
				if (horizNode == null)
					return false;

				float dist = (float)Math.Abs(MathLib.DotProduct(horizNode.GetPosition(), normal) + d);
				if (dist > offPlaneTolerance)
					return false;
			}

			if (!CheckObstacles(horizNode, width, height, x, y))
				return false;
		}

		vertNode = node;
		for (y = 0; y < height; ++y) {
			horizNode = vertNode;

			for (x = 0; x < width; ++x) {
				if (!TestForValidJumpArea(horizNode))
					return false;

				if (nodeCrouch && !TestForValidCrouchArea(horizNode))
					return false;

				horizNode = horizNode.GetConnectedNode(NavDirType.East);
			}

			vertNode = vertNode.GetConnectedNode(NavDirType.South);
		}

		if (GenerationMode == GenerationModeType.Incremental) {
			Vector3 nw = node.GetPosition();

			vertNode = node;
			for (y = 0; y < height; ++y)
				vertNode = vertNode.GetConnectedNode(NavDirType.South);
			Vector3 sw = vertNode.GetPosition();

			horizNode = node;
			for (x = 0; x < width; ++x)
				horizNode = horizNode.GetConnectedNode(NavDirType.East);
			Vector3 ne = horizNode.GetPosition();

			vertNode = horizNode;
			for (y = 0; y < height; ++y)
				vertNode = vertNode.GetConnectedNode(NavDirType.South);
			Vector3 se = vertNode.GetPosition();

			TestOverlapping test = new(nw, ne, sw, se);
			if (test.OverlapsExistingArea())
				return false;
		}

		return true;
	}

	bool CheckObstacles(NavNode node, int width, int height, int x, int y) {
		if (width > 1 || height > 1) {
			if ((x > 0) && (node.ObstacleHeight[(int)NavDirType.West] > StepHeight))
				return false;

			if ((y > 0) && (node.ObstacleHeight[(int)NavDirType.North] > StepHeight))
				return false;

			if ((x < width - 1) && (node.ObstacleHeight[(int)NavDirType.East] > StepHeight))
				return false;

			if ((y < height - 1) && (node.ObstacleHeight[(int)NavDirType.South] > StepHeight))
				return false;
		}

		return true;
	}

	int BuildArea(NavNode node, int width, int height) {
		NavNode? nwNode = node;
		NavNode? neNode = null;
		NavNode? swNode = null;
		NavNode? seNode = null;

		NavNode? vertNode = node;
		NavNode? horizNode;

		int coveredNodes = 0;

		for (int y = 0; y < height; y++) {
			horizNode = vertNode;

			for (int x = 0; x < width; x++) {
				horizNode.Cover();
				++coveredNodes;

				horizNode = horizNode.GetConnectedNode(NavDirType.East);
			}

			if (y == 0)
				neNode = horizNode;

			vertNode = vertNode.GetConnectedNode(NavDirType.South);
		}

		swNode = vertNode;

		horizNode = vertNode;
		for (int x = 0; x < width; x++) {
			horizNode = horizNode.GetConnectedNode(NavDirType.East);
		}
		seNode = horizNode;

		if (nwNode == null || neNode == null || swNode == null || seNode == null) {
			Error("BuildArea - null node.\n");
			return -1;
		}

		NavArea? area = CreateArea();
		if (area == null) {
			Error("BuildArea: Out of memory.\n");
			return -1;
		}

		area.Build(nwNode, neNode, seNode, swNode);

		NavArea.TheNavAreas.Add(area);

		area.SetAttributes(node.GetAttributes());

		if (nwNode.ObstacleHeight[(int)NavDirType.South] > StepHeight || nwNode.ObstacleHeight[(int)NavDirType.East] > StepHeight ||
				neNode.ObstacleHeight[(int)NavDirType.West] > StepHeight || neNode.ObstacleHeight[(int)NavDirType.South] > StepHeight ||
				seNode.ObstacleHeight[(int)NavDirType.North] > StepHeight || seNode.ObstacleHeight[(int)NavDirType.West] > StepHeight ||
				swNode.ObstacleHeight[(int)NavDirType.East] > StepHeight || swNode.ObstacleHeight[(int)NavDirType.North] > StepHeight
			) {
			Assert(width == 1);
			Assert(height == 1);

			area.SetAttributes(area.GetAttributes() | NavAttributeType.NoMerge);
		}

		bool nodeCrouch = node.Crouch[(int)NavCornerType.SouthEast];
		if ((area.GetAttributes() & NavAttributeType.Crouch) != 0 && !nodeCrouch)
			area.SetAttributes(area.GetAttributes() & ~NavAttributeType.Crouch);

		return coveredNodes;
	}

	void CreateNavAreasFromNodes() {
		int tryWidth = nav_area_max_size.GetInt();
		int tryHeight = tryWidth;
		int uncoveredNodes = (int)NavNode.GetListLength();

		while (uncoveredNodes > 0) {
			for (NavNode? node = NavNode.GetFirst(); node != null; node = node.GetNext()) {
				if (node.IsCovered)
					continue;

				if (TestArea(node, tryWidth, tryHeight)) {
					int covered = BuildArea(node, tryWidth, tryHeight);
					if (covered < 0) {
						Error("Generate: Error - Data corrupt.\n");
						return;
					}

					uncoveredNodes -= covered;
				}
			}

			if (tryWidth >= tryHeight)
				--tryWidth;
			else
				--tryHeight;

			if (tryWidth <= 0 || tryHeight <= 0)
				break;
		}

		Console.WriteLine($"Num areas generated: {NavArea.TheNavAreas.Count}");

		if (NavArea.TheNavAreas.Count == 0) {
			AllocateGrid(0, 0, 0, 0);
			return;
		}

		Extent extent;
		extent.Lo.X = 9999999999.9f;
		extent.Lo.Y = 9999999999.9f;
		extent.Hi.X = -9999999999.9f;
		extent.Hi.Y = -9999999999.9f;

		foreach (NavArea area in NavArea.TheNavAreas) {
			Extent areaExtent = default;
			area.GetExtent(ref areaExtent);

			if (areaExtent.Lo.X < extent.Lo.X)
				extent.Lo.X = areaExtent.Lo.X;
			if (areaExtent.Lo.Y < extent.Lo.Y)
				extent.Lo.Y = areaExtent.Lo.Y;
			if (areaExtent.Hi.X > extent.Hi.X)
				extent.Hi.X = areaExtent.Hi.X;
			if (areaExtent.Hi.Y > extent.Hi.Y)
				extent.Hi.Y = areaExtent.Hi.Y;
		}

		AllocateGrid(extent.Lo.X, extent.Hi.X, extent.Lo.Y, extent.Hi.Y);

		foreach (NavArea area in NavArea.TheNavAreas)
			AddNavArea(area);

		ConnectGeneratedAreas();
		MarkPlayerClipAreas();
		MarkJumpAreas();
		MergeGeneratedAreas();
		SplitAreasUnderOverhangs();
		SquareUpAreas();
		MarkStairAreas();
		StichAndRemoveJumpAreas();
		HandleObstacleTopAreas();
		FixUpGeneratedAreas();

		if (GenerationMode != GenerationModeType.Incremental) {
			for (int i = 0; i < Ladders.Count; ++i) {
				NavLadder ladder = Ladders[i];
				// ladder.ConnectGeneratedLadder(0.0f); todo
			}
		}
	}

	void AddWalkableSeeds() {
		BaseEntity? spawn = gEntList.FindEntityByClassname(null, GetPlayerSpawnName());

		if (spawn != null) {
			Vector3 pos = spawn.GetAbsOrigin();
			pos.X = SnapToGrid(pos.X);
			pos.Y = SnapToGrid(pos.Y);

			if (FindGroundForNode(ref pos, out Vector3 normal))
				AddWalkableSeed(pos, normal);
		}
	}

	public void ClearWalkableSeeds() => WalkableSeeds.Clear();

	public void BeginGeneration(bool incremental = false) {
		IGameEvent? evnt = gameeventmanager.CreateEvent("nav_generate");
		if (evnt != null)
			gameeventmanager.FireEvent(evnt);

		engine.ServerCommand("bot_kick\n");

		if (incremental)
			nav_quicksave.SetValue(1);

		GenerationState = GenerationStateType.SampleWalkableSpace;
		SampleTick = 0;
		GenerationMode = incremental ? GenerationModeType.Incremental : GenerationModeType.Full;
		LastMsgTime = 0.0f;

		DestroyNavigationMesh(incremental);
		SetNavPlace(UndefinedPlace);

		if (!incremental) {
			BuildLadders();
			AddWalkableSeeds();
		}

		CurrentNode = null;

		if (WalkableSeeds.Count == 0) {
			GenerationMode = GenerationModeType.None;
			Msg("No valid walkable seed positions.  Cannot generate Navigation Mesh.\n");
			return;
		}

		SeedIdx = 0;

		Msg("Generating Navigation Mesh...\n");
		GenerationStartTime = Platform.Time;
	}

	public void BeginAnalysis(bool quitWhenFinished = false) { }

	static uint MovedPlayerToArea;
	static CountdownTimer? PlayerSettleTimer;
	static readonly List<NavArea> UnlitAreas = [];
	static readonly List<NavArea> UnlitSeedAreas = [];
	static ConVarRef host_thread_mode = new("host_thread_mode");
	bool UpdateGeneration(float maxTime) {
		double startTime = Platform.Time;
		PlayerSettleTimer ??= new();

		switch (GenerationState) {
			case GenerationStateType.SampleWalkableSpace:
				AnalysisProgress("Sampling walkable space...", 100, SampleTick / 10, false);
				SampleTick = (SampleTick + 1) % 1000;

				while (SampleStep()) {
					if (Platform.Time - startTime > maxTime)
						return true;
				}

				GenerationState = GenerationStateType.CreateAreasFromSamples;
				return true;
			case GenerationStateType.CreateAreasFromSamples:
				Msg("Creating navigation areas from sampled data...\n");
				if (GenerationMode == GenerationModeType.Incremental) {
					ClearSelectedSet();
					foreach (NavArea area in NavArea.TheNavAreas)
						AddToSelectedSet(area);
				}

				CreateNavAreasFromNodes();

				if (GenerationMode == GenerationModeType.Incremental)
					CommandNavToggleSelectedSet();

				DestroyHidingSpots();

				List<NavArea> tmpSet = [];
				foreach (NavArea area in NavArea.TheNavAreas) tmpSet.Add(area);
				NavArea.TheNavAreas.Clear();
				foreach (NavArea area in tmpSet) NavArea.TheNavAreas.Add(area);

				GenerationState = GenerationStateType.FindHidingSpots;
				GenerationIndex = 0;
				return true;
			case GenerationStateType.FindHidingSpots:
				while (GenerationIndex < NavArea.TheNavAreas.Count) {
					NavArea area = NavArea.TheNavAreas[GenerationIndex];
					GenerationIndex++;

					area.ComputeHidingSpots();

					if (Platform.Time - startTime > maxTime) {
						AnalysisProgress("Finding hiding spots...", 100, 100 * GenerationIndex / NavArea.TheNavAreas.Count);
						return true;
					}
				}

				Msg("Finding hiding spots...DONE\n");

				GenerationState = GenerationStateType.FindSniperSpots;
				GenerationIndex = 0;
				return true;
			case GenerationStateType.FindEncounterSpots:
				while (GenerationIndex < NavArea.TheNavAreas.Count) {
					NavArea area = NavArea.TheNavAreas[GenerationIndex];
					GenerationIndex++;

					area.ComputeSpotEncounters();

					if (Platform.Time - startTime > maxTime) {
						AnalysisProgress("Finding encounter spots...", 100, 100 * GenerationIndex / NavArea.TheNavAreas.Count);
						return true;
					}
				}

				Msg("Finding encounter spots...DONE\n");

				GenerationState = GenerationStateType.FindSniperSpots;
				GenerationIndex = 0;
				return true;
			case GenerationStateType.FindSniperSpots:
				while (GenerationIndex < NavArea.TheNavAreas.Count) {
					NavArea area = NavArea.TheNavAreas[GenerationIndex];
					GenerationIndex++;

					area.ComputeSniperSpots();

					if (Platform.Time - startTime > maxTime) {
						AnalysisProgress("Finding sniper spots...", 100, 100 * GenerationIndex / NavArea.TheNavAreas.Count);
						return true;
					}
				}

				Msg("Finding sniper spots...DONE\n");

				GenerationState = GenerationStateType.ComputeMeshVisibility;
				GenerationIndex = 0;
				BeginVisibilityComputations();
				Msg("Computing mesh visibility...\n");
				return true;
			case GenerationStateType.ComputeMeshVisibility:
				while (GenerationIndex < NavArea.TheNavAreas.Count) {
					NavArea area = NavArea.TheNavAreas[GenerationIndex];
					GenerationIndex++;

					area.ComputeVisibilityToMesh();

					if (Platform.Time - startTime > maxTime) {
						AnalysisProgress("Computing mesh visibility...", 100, 100 * GenerationIndex / NavArea.TheNavAreas.Count);
						return true;
					}
				}

				Msg("Optimizing mesh visibility...\n");
				EndVisibilityComputations();
				Msg("Computing mesh visibility...DONE\n");

				GenerationState = GenerationStateType.FindEarliestOccupyTimes;
				GenerationIndex = 0;
				return true;
			case GenerationStateType.FindEarliestOccupyTimes:
				while (GenerationIndex < NavArea.TheNavAreas.Count) {
					NavArea area = NavArea.TheNavAreas[GenerationIndex];
					GenerationIndex++;

					area.ComputeEarliestOccupyTimes();

					if (Platform.Time - startTime > maxTime) {
						AnalysisProgress("Finding earliest occupy times...", 100, 100 * GenerationIndex / NavArea.TheNavAreas.Count);
						return true;
					}
				}

				Msg("Finding earliest occupy times...DONE\n");

				bool shouldSkipLightComputation = GenerationMode == GenerationModeType.Incremental || engine.IsDedicatedServer();

				if (shouldSkipLightComputation)
					GenerationState = GenerationStateType.Custom;
				else {
					GenerationState = GenerationStateType.FindLightIntensity;
					PlayerSettleTimer.Invalidate();
					NavArea.MakeNewMarker();
					UnlitAreas.Clear();
					foreach (NavArea nit in NavArea.TheNavAreas) {
						UnlitAreas.Add(nit);
						UnlitSeedAreas.Add(nit);
					}
				}

				GenerationIndex = 0;
				return true;
			case GenerationStateType.FindLightIntensity:
				// host_thread_mode 0

				BasePlayer? host = Util.GetListenServerHost();

				if (UnlitAreas.Count == 0 || host == null) {
					Msg("Finding light intensity...DONE\n");
					GenerationState = GenerationStateType.Custom;
					GenerationIndex = 0;
					return true;
				}

				if (!PlayerSettleTimer.IsElapsed())
					return true;

				int sit = 0;
				while (sit < UnlitAreas.Count) {
					NavArea area = UnlitAreas[sit];

					if (area.ComputeLighting()) {
						UnlitSeedAreas.Remove(area);
						UnlitAreas.RemoveAt(sit);
						continue;
					}
					else
						sit++;
				}

				if (UnlitAreas.Count > 0) {
					if (UnlitSeedAreas.Count > 0) {
						NavArea moveArea = UnlitSeedAreas[0];
						UnlitSeedAreas.RemoveAt(0);

						Vector3 eyePos = moveArea.GetCenter();
						if (GetGroundHeight(eyePos, out float height))
							eyePos.Z = height + HalfHumanHeight - StepHeight;
						else
							eyePos.Z += HalfHumanHeight - StepHeight;

						// host.SetAbsOrigin(eyePos); // todo
						AnalysisProgress("Finding light intensity...", 100, 100 * (NavArea.TheNavAreas.Count - UnlitAreas.Count) / NavArea.TheNavAreas.Count);
						MovedPlayerToArea = moveArea.GetID();
						PlayerSettleTimer.Start(0.1f);
						return true;
					}
					else {
						Msg($"Finding light intensity...DONE ({UnlitAreas.Count} unlit areas)\n");
						if (UnlitAreas.Count > 0) {
							Warning($"To see unlit areas:\n");
							for (int i = 0; i < UnlitAreas.Count; i++) {
								NavArea area = UnlitAreas[i];
								Warning($"nav_unmark; nav_mark {area.GetID()}; nav_warp_to_mark;\n");
							}
						}

						GenerationState = GenerationStateType.Custom;
						GenerationIndex = 0;
					}
				}

				Msg("Finding light intensity...DONE\n");

				GenerationState = GenerationStateType.Custom;
				GenerationIndex = 0;
				return true;
			case GenerationStateType.Custom:
				if (GenerationIndex == 0) {
					BeginCustomAnalysis(GenerationMode == GenerationModeType.Incremental);
					Msg("Start custom...\n");
				}

				while (GenerationIndex < NavArea.TheNavAreas.Count) {
					NavArea area = NavArea.TheNavAreas[GenerationIndex];
					++GenerationIndex;

					area.CustomAnalysis(GenerationMode == GenerationModeType.Incremental);

					if (Platform.Time - startTime > maxTime) {
						AnalysisProgress("Custom game-specific analysis...", 100, 100 * GenerationIndex / NavArea.TheNavAreas.Count);
						return true;
					}
				}

				Msg("Post custom...\n");
				PostCustomAnalysis();
				EndCustomAnalysis();

				Msg("Custom game-specific analysis...DONE\n");

				GenerationState = GenerationStateType.SaveNavMesh;
				GenerationIndex = 0;

				ConVarRef mat_queue_mode = new("mat_queue_mode");
				mat_queue_mode.SetValue(-1);
				host_thread_mode.SetValue(HostThreatModeRestoreValue);
				return true;
			case GenerationStateType.SaveNavMesh:
				if (GenerationMode == GenerationModeType.AnalysisOnly || GenerationMode == GenerationModeType.Full)
					bIsAnalyzed = true;

				float generationTime = (float)(Platform.Time - GenerationStartTime);
				Msg($"Generation complete! {generationTime:F1} seconds elapsed.\n");

				bool restart = GenerationMode != GenerationModeType.Incremental;
				GenerationMode = GenerationModeType.None;
				bIsLoaded = true;

				ClearWalkableSeeds();
				HideAnalysisProgress();

				if (Save())
					Msg($"Navigation map '{GetFilename()}' saved.\n");
				else {
					ReadOnlySpan<char> filename = GetFilename();
					Msg($"ERROR: Cannot save navigation map '{(filename.IsEmpty ? "" : filename)}'.\n");
				}

				if (QuitWhenFinished)
					engine.ServerCommand("quit\n");
				else if (restart)
					engine.ChangeLevel(gpGlobals.MapName, null);
				else {
					foreach (NavArea area in NavArea.TheNavAreas)
						area.ResetNodes();
				}
				return false;
		}

		return false;
	}

	public virtual void BeginCustomAnalysis(bool incremental) { }
	public virtual void PostCustomAnalysis() { }
	public virtual void EndCustomAnalysis() { }

	static void AnalysisProgress(ReadOnlySpan<char> msg, int ticks, int current, bool showPercent = true) {
		const double MsgInterval = 10.0f;
		double now = Platform.Time;
		if (now > LastMsgTime + MsgInterval) {
			if (showPercent && ticks != 0)
				Msg($"{msg} {current * 100.0f / ticks:0}%\n");
			else
				Msg($"{msg}\n");

			LastMsgTime = now;
		}

		KeyValues data = new("data");
		data.SetString("msg", msg);
		data.SetInt("total", ticks);
		data.SetInt("current", current);

		ShowViewPortPanelToAll("nav_progress", true, data);
	}

	static void HideAnalysisProgress() => ShowViewPortPanelToAll("nav_progress", false, null);

	static void ShowViewPortPanelToAll(ReadOnlySpan<char> panelName, bool show, KeyValues? data) {
		RecipientFilter filter = new();
		filter.AddAllPlayers();
		filter.MakeReliable();

		int count = 0;
		KeyValues? subkey = null;

		if (data != null) {
			subkey = data.GetFirstSubKey();
			while (subkey != null) {
				count++;
				subkey = subkey.GetNextKey();
			}
			subkey = data.GetFirstSubKey();
		}

		UserMessageBegin(filter, "VGUIMenu");
		MessageWriteString(panelName);
		MessageWriteByte(show ? 1 : 0);
		MessageWriteByte(count);
		while (subkey != null) {
			MessageWriteString(subkey.Name);
			MessageWriteString(subkey.GetString());
			subkey = subkey.GetNextKey();
		}
		MessageEnd();
	}

	void SetPlayerSpawnName(ReadOnlySpan<char> name) => SpawnName = name.ToString();

	ReadOnlySpan<char> GetPlayerSpawnName() => SpawnName ?? "info_player_start";

	NavNode AddNode(Vector3 destPos, Vector3 normal, NavDirType dir, NavNode source, bool isOnDisplacement, float obstacleHeight, float obstacleStartDist, float obstacleEndDist) {
		NavNode? node = NavNode.GetNode(destPos);

		bool useNew = false;
		if (node == null) {
			node = new(destPos, normal, source, isOnDisplacement);
			// OnNodeAdded(node);
			useNew = true;
		}

		source.ConnectTo(node, dir, obstacleHeight, obstacleStartDist, obstacleEndDist);

		const float zTolerance = 50.0f;
		float deltaZ = source.GetPosition().Z - destPos.Z;
		if (Math.Abs(deltaZ) < zTolerance) {
			if (obstacleHeight > 0)
				obstacleHeight = Math.Max(obstacleHeight + deltaZ, 0);

			node.ConnectTo(source, OppositeDirection(dir), obstacleHeight, GenerationStepSize - obstacleEndDist, GenerationStepSize - obstacleStartDist);
			node.MarkAsVisited(OppositeDirection(dir));
		}

		if (useNew)
			CurrentNode = node;

		node.CheckCrouch();

		for (int i = 0; i < (int)NavDirType.NumDirections; i++) {
			NavDirType checkDir = (NavDirType)i;
			if (CheckCliff(node.GetPosition(), checkDir))
				node.SetAttributes(node.GetAttributes() | NavAttributeType.Cliff);
		}

		return node;
	}

	bool FindGroundForNode(ref Vector3 pos, out Vector3 normal) {
		TraceFilterWalkableEntities filter = new(null, CollisionGroup.PlayerMovement, WalkThruFlags.Everything);

		Vector3 start = new(pos.X, pos.Y, pos.Z + VEC_DUCK_HULL_MAX.Z - 0.1f);
		Vector3 end = new(pos.X, pos.Y, pos.Z - DeathDrop);

		Util.TraceHull(start, end, NavTraceMins, NavTraceMaxs, GetGenerationTraceMask(), ref filter, out Trace tr);

		pos = tr.EndPos;
		normal = tr.Plane.Normal;

		return !tr.AllSolid;
	}

	public bool StayOnFloor(ref Trace trace, float zLimit = DeathDrop) {
		Vector3 start = trace.EndPos;
		Vector3 end = start;
		end.Z -= zLimit;

		TraceFilterWalkableEntities filter = new(null, CollisionGroup.None, WalkThruFlags.Everything);
		Util.TraceHull(start, end, NavTraceMins, NavTraceMaxs, GetGenerationTraceMask(), ref filter, out trace);

		if (trace.StartSolid || trace.Fraction >= 1.0f)
			return false;

		if (trace.Plane.Normal.Z < nav_slope_limit.GetFloat())
			return false;

		return true;
	}

	public bool TraceAdjacentNode(int depth, Vector3 start, Vector3 end, out Trace trace, float zLimit = DeathDrop) {
		const float MinDistance = 1.0f;

		TraceFilterWalkableEntities filter = new(null, CollisionGroup.None, WalkThruFlags.Everything);
		Util.TraceHull(start, end, NavTraceMins, NavTraceMaxs, GetGenerationTraceMask(), ref filter, out trace);

		if (trace.StartSolid)
			return false;

		if (end.X == trace.EndPos.X && end.Y == trace.EndPos.Y)
			return StayOnFloor(ref trace, zLimit);

		if (depth > 0 && new Vector2(trace.EndPos.X - start.X, trace.EndPos.Y - start.Y).LengthSquared() < (MinDistance * MinDistance))
			return false;

		if (!StayOnFloor(ref trace, zLimit))
			return false;

		Vector3 testStart = trace.EndPos;
		Vector3 testEnd = testStart;
		testEnd.Z += StepHeight;

		if (!TraceAdjacentNode(depth + 1, testStart, testEnd, out trace))
			return false;

		Vector3 forwardTestStart = trace.EndPos;
		Vector3 forwardTestEnd = end;
		forwardTestEnd.Z = forwardTestStart.Z;

		return TraceAdjacentNode(depth + 1, forwardTestStart, forwardTestEnd, out trace);
	}

	public bool IsNodeOverlapped(Vector3 pos, Vector3 offset) {
		bool overlap = GetNavArea(pos + offset, HumanHeight) != null;
		if (!overlap) {
			Vector3 mins = new(-0.5f, -0.5f, -0.5f);
			Vector3 maxs = new(0.5f, 0.5f, 0.5f);

			Vector3 start = pos;
			start.Z += HalfHumanHeight;
			Vector3 end = start;
			end.X += offset.X * GenerationStepSize;
			end.Y += offset.Y * GenerationStepSize;

			TraceFilterWalkableEntities filter = new(null, CollisionGroup.None, WalkThruFlags.Everything);
			Util.TraceHull(start, end, mins, maxs, GetGenerationTraceMask(), ref filter, out Trace trace);

			if (trace.StartSolid || trace.AllSolid)
				return true;

			if (trace.Fraction < 0.1f)
				return true;

			start = trace.EndPos;
			end = start;
			end.Z -= HalfHumanHeight * 2;

			Util.TraceHull(start, end, mins, maxs, GetGenerationTraceMask(), ref filter, out trace);

			if (trace.StartSolid || trace.AllSolid)
				return true;

			if (trace.Fraction == 1.0f)
				return true;

			if (trace.Plane.Normal.Z < 0.7f)
				return true;
		}
		return overlap;
	}

	bool SampleStep() {
		while (true) {
			if (CurrentNode == null) {
				CurrentNode = GetNextWalkableSeedNode();

				if (CurrentNode == null) {
					if (GenerationMode == GenerationModeType.Incremental || GenerationMode == GenerationModeType.Simplify)
						return false;

					for (int i = 0; i < Ladders.Count; ++i) {
						NavLadder ladder = Ladders[i];

						// todo LadderEndSearch
						// if ((CurrentNode = LadderEndSearch(&ladder.m_bottom, ladder.GetDir())) != 0)
						// 	break;

						// if ((CurrentNode = LadderEndSearch(&ladder.m_top, ladder.GetDir())) != 0)
						// 	break;
					}

					if (CurrentNode == null) {
						return false;
					}
				}
			}

			for (NavDirType dir = NavDirType.North; dir < NavDirType.NumDirections; dir++) {
				if (!CurrentNode.HasVisited(dir)) {
					Vector3 pos = CurrentNode.GetPosition();

					int cx = (int)SnapToGrid(pos.X);
					int cy = (int)SnapToGrid(pos.Y);

					switch (dir) {
						case NavDirType.North: cy -= (int)GenerationStepSize; break;
						case NavDirType.South: cy += (int)GenerationStepSize; break;
						case NavDirType.East: cx += (int)GenerationStepSize; break;
						case NavDirType.West: cx -= (int)GenerationStepSize; break;
					}

					pos.X = cx;
					pos.Y = cy;

					GenerationDir = dir;

					CurrentNode.MarkAsVisited(GenerationDir);

					float incrementalRange = nav_generate_incremental_range.GetFloat();
					if (GenerationMode == GenerationModeType.Incremental && incrementalRange > 0) {
						bool inRange = false;
						for (int i = 0; i < WalkableSeeds.Count; ++i) {
							Vector3 seedPos = WalkableSeeds[i].Pos;
							if ((seedPos - pos).Length() < incrementalRange) {
								inRange = true;
								break;
							}
						}

						if (!inRange)
							return true;
					}

					if (GenerationMode == GenerationModeType.Simplify) {
						if (!SimplifyGenerationExtent.Contains(pos))
							return true;
					}

					Vector3 from = CurrentNode.GetPosition();
					TraceFilterWalkableEntities filter = new(null, CollisionGroup.None, WalkThruFlags.Everything);
					Vector3 to = new(0, 0, 0);
					Vector3 toNormal = new(0, 0, 0);
					float obstacleHeight = 0, obstacleStartDist = 0, obstacleEndDist = GenerationStepSize;
					if (TraceAdjacentNode(0, from, pos, out Trace result)) {
						to = result.EndPos;
						toNormal = result.Plane.Normal;
					}
					else {
						bool success = false;
						for (float height = StepHeight; height <= ClimbUpHeight; height += 1.0f) {
							Vector3 start = from;
							Vector3 end = pos;
							start.Z += height;
							end.Z += height;
							Util.TraceHull(start, end, NavTraceMins, NavTraceMaxs, GetGenerationTraceMask(), ref filter, out Trace tr);
							if (!tr.StartSolid && tr.Fraction == 1.0f) {
								if (!StayOnFloor(ref tr))
									break;

								to = tr.EndPos;
								toNormal = tr.Plane.Normal;

								start = end = from;
								end.Z += height;
								Util.TraceHull(start, end, NavTraceMins, NavTraceMaxs, GetGenerationTraceMask(), ref filter, out tr);
								if (tr.Fraction < 1.0f)
									break;

								obstacleHeight = height;
								success = true;
								break;
							}
							else {
								Vector3 vecToObstacleStart = tr.EndPos - start;
								Assert(vecToObstacleStart.LengthSqr() <= (GenerationStepSize * GenerationStepSize));
								if (vecToObstacleStart.LengthSqr() <= (GenerationStepSize * GenerationStepSize)) {
									Util.TraceHull(end, start, NavTraceMins, NavTraceMaxs, GetGenerationTraceMask(), ref filter, out tr);
									if (!tr.StartSolid && tr.Fraction < 1.0) {
										Vector3 vecToObstacleEnd = tr.EndPos - start;
										Assert(vecToObstacleEnd.LengthSqr() <= (GenerationStepSize * GenerationStepSize));
										if (vecToObstacleEnd.LengthSqr() <= (GenerationStepSize * GenerationStepSize)) {
											obstacleStartDist = vecToObstacleStart.Length();
											obstacleEndDist = vecToObstacleEnd.Length();
											if (obstacleEndDist == 0)
												obstacleEndDist = GenerationStepSize;
										}
									}
								}
							}
						}

						if (!success)
							return true;
					}

					if ((result.Surface.Flags & (ushort)(Surf.Sky | Surf.Sky2D)) != 0)
						return true;

					Vector3 testPos = to;
					bool overlapSE = IsNodeOverlapped(testPos, new Vector3(1, 1, HalfHumanHeight));
					bool overlapSW = IsNodeOverlapped(testPos, new Vector3(-1, 1, HalfHumanHeight));
					bool overlapNE = IsNodeOverlapped(testPos, new Vector3(1, -1, HalfHumanHeight));
					bool overlapNW = IsNodeOverlapped(testPos, new Vector3(-1, -1, HalfHumanHeight));
					if (overlapSE && overlapSW && overlapNE && overlapNW && GenerationMode != GenerationModeType.Simplify)
						return true;

					int tolerance = nav_generate_incremental_tolerance.GetInt();
					if (tolerance > 0 && GenerationMode == GenerationModeType.Incremental) {
						bool valid = false;
						int zPos = (int)to.Z;
						for (int i = 0; i < WalkableSeeds.Count; ++i) {
							Vector3 seedPos = WalkableSeeds[i].Pos;
							int zMin = (int)(seedPos.Z - tolerance);
							int zMax = (int)(seedPos.Z + tolerance);

							if (zPos >= zMin && zPos <= zMax) {
								valid = true;
								break;
							}
						}

						if (!valid)
							return true;
					}


					bool isOnDisplacement = result.IsDispSurface();

					if (nav_displacement_test.GetInt() > 0) {
						Vector3 start = to + new Vector3(0, 0, 0);
						Vector3 end = start + new Vector3(0, 0, nav_displacement_test.GetInt());
						Util.TraceHull(start, end, NavTraceMins, NavTraceMaxs, GetGenerationTraceMask(), ref filter, out result);

						if (result.Fraction > 0) {
							end = start;
							start = result.EndPos;
							Util.TraceHull(start, end, NavTraceMins, NavTraceMaxs, GetGenerationTraceMask(), ref filter, out result);
							if (result.Fraction < 1) {
								if (result.EndPos.Z > to.Z + StepHeight)
									return true;
							}
						}
					}

					float deltaZ = to.Z - CurrentNode.GetPosition().Z;
					if ((obstacleHeight < StepHeight) || (deltaZ > (obstacleHeight - 2.0f))) {
						obstacleHeight = 0;
						obstacleStartDist = 0;
						obstacleEndDist = GenerationStepSize;
					}

					AddNode(to, toNormal, GenerationDir, CurrentNode, isOnDisplacement, obstacleHeight, obstacleStartDist, obstacleEndDist);

					return true;
				}
			}

			CurrentNode = CurrentNode.GetParent();
		}
	}

	void AddWalkableSeed(Vector3 pos, Vector3 normal) {
		WalkableSeedSpot seed = new();
		seed.Pos.X = RoundToUnits(pos.X, GenerationStepSize);
		seed.Pos.Y = RoundToUnits(pos.Y, GenerationStepSize);
		seed.Pos.Z = pos.Z;
		seed.Normal = normal;

		WalkableSeeds.Add(seed);
	}

	NavNode? GetNextWalkableSeedNode() {
		if (SeedIdx >= WalkableSeeds.Count)
			return null;

		WalkableSeedSpot seed = WalkableSeeds[SeedIdx];
		++SeedIdx;

		NavNode? node = NavNode.GetNode(seed.Pos);
		if (node != null)
			return null;

		return new NavNode(seed.Pos, seed.Normal, null, false);
	}

	void CommandNavSubdivide(in TokenizedCommand args) { }

	void ValidateNavAreaConnections() {
		NavConnect connect = new();

		for (int it = 0; it < NavArea.TheNavAreas.Count; it++) {
			NavArea area = NavArea.TheNavAreas[it];

			for (NavDirType dir = NavDirType.North; dir < NavDirType.NumDirections; dir = (NavDirType)(((int)dir) + 1)) {
				List<NavConnect> outgoing = area.GetAdjacentAreas(dir);
				List<NavConnect> incoming = area.GetIncomingConnections(dir);

				for (int con = 0; con < outgoing.Count; con++) {
					NavArea areaOther = outgoing[con].Area!;
					connect.Area = areaOther;
					if (incoming.Contains(connect)) {
						Msg($"Area {area.GetID()} has area {areaOther.GetID()} on both 2-way and incoming list, should only be on one\n");
						Assert(false);
					}

					for (int connectCheck = con + 1; connectCheck < outgoing.Count; connectCheck++) {
						NavArea areaCheck = outgoing[connectCheck].Area!;
						if (areaOther == areaCheck) {
							Msg($"Area {area.GetID()} has multiple outgoing connections to area {areaOther.GetID()} in direction {dir}\n");
							Assert(false);
						}
					}

					List<NavConnect> outgoingOther = areaOther.GetAdjacentAreas(OppositeDirection(dir));
					List<NavConnect> incomingOther = areaOther.GetIncomingConnections(OppositeDirection(dir));

					connect.Area = area;
					if (!outgoingOther.Contains(connect)) {
						connect.Area = area;
						if (!incomingOther.Contains(connect))
							Msg($"Area {area.GetID()} has one-way connect to area {areaOther.GetID()} but does not appear on the latter's incoming list\n");
					}
				}

				for (int con = 0; con < incoming.Count; con++) {
					NavArea areaOther = incoming[con].Area!;

					for (int connectCheck = con + 1; connectCheck < incoming.Count; connectCheck++) {
						NavArea areaCheck = incoming[connectCheck].Area!;
						if (areaOther == areaCheck) {
							Msg("Area %d has multiple incoming connections to area %d in direction %d\n", area.GetID(), areaOther.GetID(), dir);
							Assert(false);
						}
					}

					List<NavConnect> outgoingOther = areaOther.GetAdjacentAreas(OppositeDirection(dir));
					connect.Area = area;
					if (!outgoingOther.Contains(connect)) {
						Msg($"Area {area.GetID()} has incoming connection from area {areaOther.GetID()} but does not appear on latter's outgoing connection list\n");
						Assert(false);
					}
				}
			}
		}
	}

	void PostProcessCliffAreas() { }
}