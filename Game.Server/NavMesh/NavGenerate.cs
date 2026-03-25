using static Game.Server.NavMesh.NavGenerate;
using static Game.Server.NavMesh.Nav;

using Source.Common.Commands;

using System.Numerics;

namespace Game.Server.NavMesh;

static class NavGenerate
{
	public const int MAX_BLOCKED_AREAS = 256;

	public const float MaxObstacleAreaWidth = StepHeight;
	public const float MinObstacleAreaWidth = 10.0f;

	public static uint[] BlockedID = new uint[MAX_BLOCKED_AREAS];
	public static int BlockedIDCount = 0;

	public static float LastMsgTime = 0.0f;

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

		if ((jumpArea.GetAttributes() & (int)NavAttributeType.Jump) == 0)
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

			if ((sourceArea.GetAttributes() & (int)NavAttributeType.Jump) != 0) {
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

			if ((destArea.GetAttributes() & (int)NavAttributeType.Jump) != 0)
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

	void ConnectGeneratedAreas() { }

	void MergeGeneratedAreas() { }

	void FixUpGeneratedAreas() { }

	void FixConnections() { }

	void FixCornerOnCornerAreas() { }

	void SplitAreasUnderOverhangs() { }

	bool TestArea(NavNode node, int width, int height) {
		throw new NotImplementedException();
	}

	bool CheckObstacles(NavNode node, int width, int height, int x, int y) {
		throw new NotImplementedException();
	}

	int BuildArea(NavNode node, int width, int height) {
		throw new NotImplementedException();
	}

	void CreateNavAreasFromNodes() { }

	void AddWalkableSeeds() { }

	public void ClearWalkableSeeds() => WalkableSeeds.Clear();

	public void BeginGeneration(bool incremental = false) {
		throw new NotImplementedException();
	}

	public void BeginAnalysis(bool quitWhenFinished = false) { }

	bool UpdateGeneration(float maxTime) {
		throw new NotImplementedException();
	}

	void SetPlayerSpawnName(char name) { }

	char GetPlayerSpawnName() {
		throw new NotImplementedException();
	}

	NavNode AddNode(Vector3 destPos, Vector3 normal, NavDirType dir, NavNode source, bool isOnDisplacement, float obstacleHeight, float obstacleStartDist, float obstacleEndDist) {
		throw new NotImplementedException();
	}

	bool FindGroundForNode(Vector3 pos, Vector3 normal) {
		throw new NotImplementedException();
	}

	bool SampleStep() {
		throw new NotImplementedException();
	}

	void AddWalkableSeed(Vector3 pos, Vector3 normal) { }

	NavNode GetNextWalkableSeedNode() {
		throw new NotImplementedException();
	}

	void CommandNavSubdivide(in TokenizedCommand args) { }

	void ValidateNavAreaConnections() { }

	void PostProcessCliffAreas() { }
}