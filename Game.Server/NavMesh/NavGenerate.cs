using static Game.Server.NavMesh.NavGenerate;
using static Game.Server.NavMesh.Nav;

using Source.Common.Commands;

using System.Numerics;
using Source.Common;
using Source;
using Source.Common.Formats.Keyvalues;

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

	void AddWalkableSeeds() {
		BaseEntity? spawn = gEntList.FindEntityByClassname(null, GetPlayerSpawnName());

		if (spawn != null) {
			Vector3 pos = spawn.GetAbsOrigin();
			pos.X = SnapToGrid(pos.X);
			pos.Y = SnapToGrid(pos.Y);

			if (FindGroundForNode(pos, out Vector3 normal))
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
		throw new NotImplementedException();
	}

	bool FindGroundForNode(Vector3 pos, out Vector3 normal) {
		TraceFilterWalkableEntities filter = new(null, CollisionGroup.PlayerMovement, WalkThruFlags.Everything);
		Trace tr = new();
		Vector3 start = new(pos.X, pos.Y, pos.Z + VEC_DUCK_HULL_MAX.Z - 0.1f);
		Vector3 end = new(pos.X, pos.Y, pos.Z - DeathDrop);

		// todo tracehull

		// throw new NotImplementedException();
		normal = Vector3.Zero;
		return true;
	}

	bool SampleStep() {
		while (true) {
			if (CurrentNode == null) {
				CurrentNode = GetNextWalkableSeedNode();

				if (CurrentNode == null) {
					if (GenerationMode == GenerationModeType.Incremental || GenerationMode == GenerationModeType.Simplify)
						return false;

					// for (int i = 0; i < Ladders.Count; i++) {
					// 	NavLadder ladder = Ladders[i];

					// }

					if (CurrentNode == null)
						return false;
				}
			}

			for (NavDirType dir = NavDirType.North; dir < NavDirType.NumDirections; dir++) {

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
						Msg("Area %d has area %d on both 2-way and incoming list, should only be on one\n", area.GetID(), areaOther.GetID());
						Assert(false);
					}

					for (int connectCheck = con + 1; connectCheck < outgoing.Count; connectCheck++) {
						NavArea areaCheck = outgoing[connectCheck].Area!;
						if (areaOther == areaCheck) {
							Msg("Area %d has multiple outgoing connections to area %d in direction %d\n", area.GetID(), areaOther.GetID(), dir);
							Assert(false);
						}
					}

					List<NavConnect> outgoingOther = areaOther.GetAdjacentAreas(OppositeDirection(dir));
					List<NavConnect> incomingOther = areaOther.GetIncomingConnections(OppositeDirection(dir));

					connect.Area = area;
					if (!outgoingOther.Contains(connect)) {
						connect.Area = area;
						if (!incomingOther.Contains(connect))
							Msg("Area %d has one-way connect to area %d but does not appear on the latter's incoming list\n", area.GetID(), areaOther.GetID());
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
						Msg("Area %d has incoming connection from area %d but does not appear on latter's outgoing connection list\n", area.GetID(), areaOther.GetID());
						Assert(false);
					}
				}
			}
		}
	}

	void PostProcessCliffAreas() { }
}