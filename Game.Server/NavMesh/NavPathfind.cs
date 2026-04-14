using System.Numerics;

using Source;
using Source.Common;

namespace Game.Server.NavMesh;

enum RouteType
{
	Default,
	Fastest,
	Safest,
	Retreat
}

[Flags]
public enum SearchFlags : uint
{
	IncludeIncomingConnections = 0x1,
	IncludeBlockedAreas = 0x2,
	ExcludeOutgoingConnections = 0x4,
	ExcludeElevators = 0x8
}

class ShortestPathCost
{
	public float Invoke(NavArea area, NavArea? fromArea, NavLadder? ladder, FuncElevator? elevator, float length) {
		if (fromArea == null)
			return 0.0f;

		float dist;
		if (ladder != null)
			dist = ladder.Length;
		else if (length > 0.0f)
			dist = length;
		else
			dist = (area.Center - fromArea.Center).Length();

		float cost = dist + fromArea.CostSoFar;

		if ((area.AttributeFlags & NavAttributeType.Crouch) != 0) {
			const float crouchPenalty = 20.0f;
			cost += crouchPenalty * dist;
		}

		if ((area.AttributeFlags & NavAttributeType.Jump) != 0) {
			const float jumpPenalty = 5.0f;
			cost += jumpPenalty * dist;
		}

		return cost;
	}
}

public class ISearchSurroundingAreasFunctor
{
	public virtual bool Invoke(NavArea area, NavArea? priorArea, float travelDistanceSoFar) => true;

	public virtual bool ShouldSearch(NavArea adjArea, NavArea currentArea, float travelDistanceSoFar) => !adjArea.IsBlocked(Constants.TEAM_ANY);

	public virtual void IterateAdjacentAreas(NavArea area, NavArea? priorArea, float travelDistanceSoFar) {
		for (int dir = 0; dir < (int)NavDirType.NumDirections; ++dir) {
			List<NavConnect> adjList = area.GetAdjacentAreas((NavDirType)dir);
			for (int i = 0; i < adjList.Count; ++i) {
				NavArea? adjArea = adjList[i].Area;
				if (adjArea != null && ShouldSearch(adjArea, area, travelDistanceSoFar))
					IncludeInSearch(adjArea, area);
			}
		}
	}

	public virtual void PostSearch() { }

	public void IncludeInSearch(NavArea area, NavArea? priorArea) {
		if (area == null)
			return;

		if (!area.IsMarked()) {
			area.Mark();
			area.TotalCost = 0.0f;
			area.SetParent(priorArea);

			if (priorArea != null) {
				float distAlong = priorArea.CostSoFar;
				distAlong += (area.Center - priorArea.Center).Length();
				area.CostSoFar = distAlong;
			}
			else
				area.CostSoFar = 0.0f;

			area.AddToOpenList();
		}
	}
}

class FarAwayFunctor
{
	public float Invoke(NavArea area, NavArea fromArea, NavLadder? ladder) {
		if (area == fromArea)
			return 9999999.9f;
		return 1.0f / (fromArea.Center - area.Center).Length();
	}
}

class FarAwayFromPositionFunctor(Vector3 pos)
{
	readonly Vector3 Pos = pos;
	public float Invoke(NavArea area, NavArea fromArea, NavLadder? ladder) => 1.0f / (Pos - area.Center).Length();
}

public static class NavPathfind
{
	enum SearchType
	{
		Floor,
		Ladders,
		Elevators
	}

	public static bool NavAreaBuildPath(NavArea? startArea, NavArea? goalArea, Vector3? goalPos, Func<NavArea, NavArea?, NavLadder?, FuncElevator?, float, float> costFunc, out NavArea? closestArea, float maxPathLength = 0.0f, int teamID = Constants.TEAM_ANY, bool ignoreNavBlockers = false) {
		closestArea = startArea;

		if (startArea == null)
			return false;

		startArea.SetParent(null);

		if (goalArea != null && goalArea.IsBlocked(teamID, ignoreNavBlockers))
			goalArea = null;

		if (goalArea == null && goalPos == null)
			return false;

		if (startArea == goalArea)
			return true;

		Vector3 actualGoalPos = goalPos ?? goalArea!.Center;

		NavArea.ClearSearchLists();

		startArea.TotalCost = (startArea.Center - actualGoalPos).Length();

		float initCost = costFunc(startArea, null, null, null, -1.0f);
		if (initCost < 0.0f)
			return false;

		startArea.CostSoFar = initCost;
		startArea.PathLengthSoFar = 0.0f;
		startArea.AddToOpenList();

		float closestAreaDist = startArea.TotalCost;

		while (!NavArea.IsOpenListEmpty()) {
			NavArea area = NavArea.PopOpenList();

			if (area.IsBlocked(teamID, ignoreNavBlockers))
				continue;

			if (area == goalArea || (goalArea == null && goalPos.HasValue && area.Contains(goalPos.Value))) {
				closestArea = area;
				return true;
			}

			SearchType searchWhere = SearchType.Floor;
			int searchIndex = 0;

			int dir = (int)NavDirType.North;
			List<NavConnect> floorList = area.GetAdjacentAreas(NavDirType.North);

			bool ladderUp = true;
			List<NavLadderConnect>? ladderList = null;
			const int AHEAD = 0, LEFT = 1, RIGHT = 2;
			int ladderTopDir = AHEAD;
			bool bHaveMaxPathLength = maxPathLength > 0.0f;
			float length = -1;

			while (true) {
				NavArea? newArea = null;
				NavTraverseType how = default;
				NavLadder? ladder = null;
				FuncElevator? elevator = null;

				if (searchWhere == SearchType.Floor) {
					if (searchIndex >= floorList.Count) {
						++dir;
						if (dir == (int)NavDirType.NumDirections) {
							searchWhere = SearchType.Ladders;
							ladderList = area.GetLadders(NavLadder.LadderDirectionType.Up);
							searchIndex = 0;
							ladderTopDir = AHEAD;
						}
						else {
							floorList = area.GetAdjacentAreas((NavDirType)dir);
							searchIndex = 0;
						}
						continue;
					}

					NavConnect floorConnect = floorList[searchIndex];
					newArea = floorConnect.Area;
					length = floorConnect.Length;
					how = (NavTraverseType)dir;
					++searchIndex;
				}
				else if (searchWhere == SearchType.Ladders) {
					if (searchIndex >= ladderList!.Count) {
						if (!ladderUp) {
							searchWhere = SearchType.Elevators;
							searchIndex = 0;
							ladder = null;
						}
						else {
							ladderUp = false;
							ladderList = area.GetLadders(NavLadder.LadderDirectionType.Down);
							searchIndex = 0;
						}
						continue;
					}

					if (ladderUp) {
						ladder = ladderList[searchIndex].Ladder;

						if (ladderTopDir == AHEAD)
							newArea = ladder!.TopForwardArea;
						else if (ladderTopDir == LEFT)
							newArea = ladder!.TopLeftArea;
						else if (ladderTopDir == RIGHT)
							newArea = ladder!.TopRightArea;
						else {
							++searchIndex;
							ladderTopDir = AHEAD;
							continue;
						}

						how = NavTraverseType.LadderUp;
						++ladderTopDir;
					}
					else {
						newArea = ladderList[searchIndex].Ladder?.BottomArea;
						how = NavTraverseType.LadderDown;
						ladder = ladderList[searchIndex].Ladder;
						++searchIndex;
					}

					if (newArea == null)
						continue;

					length = -1.0f;
				}
				else {
					List<NavConnect> elevatorAreas = area.GetElevatorAreas();
					elevator = area.GetElevator();

					if (elevator == null || searchIndex >= elevatorAreas.Count) {
						elevator = null;
						break;
					}

					newArea = elevatorAreas[searchIndex++].Area;
					how = newArea!.Center.Z > area.Center.Z ? NavTraverseType.ElevatorUp : NavTraverseType.ElevatorDown;
					length = -1.0f;
				}

				if (newArea == area.GetParent() || newArea == area)
					continue;

				if (newArea!.IsBlocked(teamID, ignoreNavBlockers))
					continue;

				float newCostSoFar = costFunc(newArea, area, ladder, elevator, length);

				if (float.IsNaN(newCostSoFar))
					newCostSoFar = 1e30f;

				if (newCostSoFar < 0.0f)
					continue;

				float minNewCostSoFar = area.CostSoFar * 1.00001f + 0.00001f;
				newCostSoFar = MathF.Max(newCostSoFar, minNewCostSoFar);

				if (bHaveMaxPathLength) {
					float deltaLength = (newArea.Center - area.Center).Length();
					float newLengthSoFar = area.PathLengthSoFar + deltaLength;
					if (newLengthSoFar > maxPathLength)
						continue;
					newArea.PathLengthSoFar = newLengthSoFar;
				}

				if ((newArea.IsOpen() || newArea.IsClosed()) && newArea.CostSoFar <= newCostSoFar)
					continue;

				float distSq = (newArea.Center - actualGoalPos).LengthSquared();
				float newCostRemaining = distSq > 0.0f ? MathF.Sqrt(distSq) : 0.0f;

				if (closestArea != null && newCostRemaining < closestAreaDist) {
					closestArea = newArea;
					closestAreaDist = newCostRemaining;
				}

				newArea.CostSoFar = newCostSoFar;
				newArea.TotalCost = newCostSoFar + newCostRemaining;

				if (newArea.IsClosed())
					newArea.RemoveFromClosedList();

				if (newArea.IsOpen())
					newArea.UpdateOnOpenList();
				else
					newArea.AddToOpenList();

				newArea.SetParent(area, how);
			}

			area.AddToClosedList();
		}

		return false;
	}

	public static bool NavAreaBuildPath(NavArea? startArea, NavArea? goalArea, Vector3? goalPos, Func<NavArea, NavArea?, NavLadder?, FuncElevator?, float, float> costFunc, float maxPathLength = 0.0f, int teamID = Constants.TEAM_ANY, bool ignoreNavBlockers = false)
		=> NavAreaBuildPath(startArea, goalArea, goalPos, costFunc, out _, maxPathLength, teamID, ignoreNavBlockers);

	public static float NavAreaTravelDistance(NavArea? startArea, NavArea? endArea, Func<NavArea, NavArea?, NavLadder?, FuncElevator?, float, float> costFunc, float maxPathLength = 0.0f) {
		if (startArea == null || endArea == null)
			return -1.0f;

		if (startArea == endArea)
			return 0.0f;

		if (!NavAreaBuildPath(startArea, endArea, null, costFunc, maxPathLength))
			return -1.0f;

		float distance = 0.0f;
		for (NavArea? area = endArea; area?.GetParent() != null; area = area.GetParent())
			distance += (area.Center - area.GetParent()!.Center).Length();

		return distance;
	}

	static void AddAreaToOpenList(NavArea? area, NavArea? parent, Vector3 startPos, float maxRange) {
		if (area == null)
			return;

		if (!area.IsMarked()) {
			area.Mark();
			area.TotalCost = 0.0f;
			area.SetParent(parent);

			if (maxRange > 0.0f) {
				area.GetClosestPointOnArea(ref startPos, out Vector3 closePos);
				Vector2 diff = new(closePos.X - startPos.X, closePos.Y - startPos.Y);
				if (diff.Length() < maxRange) {
					float distAlong = parent!.CostSoFar;
					distAlong += (area.Center - parent.Center).Length();
					area.CostSoFar = distAlong;

					if (distAlong <= 1.5f * maxRange)
						area.AddToOpenList();
				}
			}
			else {
				area.AddToOpenList();
			}
		}
	}

	public static void SearchSurroundingAreas(NavArea? startArea, Vector3 startPos, Func<NavArea, bool> func, float maxRange = -1.0f, SearchFlags options = 0, int teamID = Constants.TEAM_ANY) {
		if (startArea == null)
			return;

		NavArea.MakeNewMarker();
		NavArea.ClearSearchLists();

		startArea.AddToOpenList();
		startArea.TotalCost = 0.0f;
		startArea.CostSoFar = 0.0f;
		startArea.SetParent(null);
		startArea.Mark();

		while (!NavArea.IsOpenListEmpty()) {
			NavArea area = NavArea.PopOpenList();

			if (area.IsBlocked(teamID) && (options & SearchFlags.IncludeBlockedAreas) == 0)
				continue;

			if (func(area)) {
				for (int dir = 0; dir < (int)NavDirType.NumDirections; ++dir) {
					int count = area.GetAdjacentCount((NavDirType)dir);
					for (int i = 0; i < count; ++i) {
						NavArea? adjArea = area.GetAdjacentArea((NavDirType)dir, i);
						if ((options & SearchFlags.ExcludeOutgoingConnections) != 0) {
							if (!adjArea.IsConnected(area, NavDirType.NumDirections))
								continue;
						}
						AddAreaToOpenList(adjArea, area, startPos, maxRange);
					}
				}

				if ((options & SearchFlags.IncludeIncomingConnections) != 0) {
					for (int dir = 0; dir < (int)NavDirType.NumDirections; ++dir) {
						List<NavConnect> list = area.GetIncomingConnections((NavDirType)dir);
						foreach (NavConnect connect in list)
							AddAreaToOpenList(connect.Area, area, startPos, maxRange);
					}
				}

				List<NavLadderConnect>? ladderList = area.GetLadders(NavLadder.LadderDirectionType.Up);
				if (ladderList != null) {
					foreach (NavLadderConnect lc in ladderList) {
						AddAreaToOpenList(lc.Ladder?.TopForwardArea, area, startPos, maxRange);
						AddAreaToOpenList(lc.Ladder?.TopLeftArea, area, startPos, maxRange);
						AddAreaToOpenList(lc.Ladder?.TopRightArea, area, startPos, maxRange);
					}
				}

				ladderList = area.GetLadders(NavLadder.LadderDirectionType.Down);
				if (ladderList != null) {
					foreach (NavLadderConnect lc in ladderList)
						AddAreaToOpenList(lc.Ladder?.BottomArea, area, startPos, maxRange);
				}

				if ((options & SearchFlags.ExcludeElevators) == 0) {
					foreach (NavConnect ec in area.GetElevatorAreas())
						AddAreaToOpenList(ec.Area, area, startPos, maxRange);
				}
			}
		}
	}

	public static void SearchSurroundingAreas(NavArea? startArea, ISearchSurroundingAreasFunctor func, float travelDistanceLimit = -1.0f) {
		if (startArea != null) {
			NavArea.MakeNewMarker();
			NavArea.ClearSearchLists();

			startArea.AddToOpenList();
			startArea.TotalCost = 0.0f;
			startArea.CostSoFar = 0.0f;
			startArea.SetParent(null);
			startArea.Mark();

			while (!NavArea.IsOpenListEmpty()) {
				NavArea area = NavArea.PopOpenList();

				if (travelDistanceLimit > 0.0f && area.CostSoFar > travelDistanceLimit)
					continue;

				if (func.Invoke(area, area.GetParent(), area.CostSoFar))
					func.IterateAdjacentAreas(area, area.GetParent(), area.CostSoFar);
				else
					break;
			}
		}

		func.PostSearch();
	}

	public static void CollectSurroundingAreas(List<NavArea> nearbyAreaVector, NavArea? startArea, float travelDistanceLimit = 1500.0f, float maxStepUpLimit = Nav.StepHeight, float maxDropDownLimit = 100.0f) {
		nearbyAreaVector.Clear();

		if (startArea != null) {
			NavArea.MakeNewMarker();
			NavArea.ClearSearchLists();

			startArea.AddToOpenList();
			startArea.TotalCost = 0.0f;
			startArea.CostSoFar = 0.0f;
			startArea.SetParent(null);
			startArea.Mark();

			while (!NavArea.IsOpenListEmpty()) {
				NavArea area = NavArea.PopOpenList();

				if (travelDistanceLimit > 0.0f && area.CostSoFar > travelDistanceLimit)
					continue;

				if (area.GetParent() != null) {
					float deltaZ = area.GetParent()!.ComputeAdjacentConnectionHeightChange(area);
					if (deltaZ > maxStepUpLimit) continue;
					if (deltaZ < -maxDropDownLimit) continue;
				}

				nearbyAreaVector.Add(area);
				area.Mark();

				for (int dir = 0; dir < (int)NavDirType.NumDirections; ++dir) {
					int count = area.GetAdjacentCount((NavDirType)dir);
					for (int i = 0; i < count; ++i) {
						NavArea? adjArea = area.GetAdjacentArea((NavDirType)dir, i);
						if (adjArea.IsBlocked(Constants.TEAM_ANY)) continue;

						if (!adjArea.IsMarked()) {
							adjArea.TotalCost = 0.0f;
							adjArea.SetParent(area);

							float distAlong = area.CostSoFar;
							distAlong += (adjArea.Center - area.Center).Length();
							adjArea.CostSoFar = distAlong;
							adjArea.AddToOpenList();
						}
					}
				}
			}
		}
	}

	public static NavArea? FindMinimumCostArea(NavArea? startArea, Func<NavArea, NavArea?, NavLadder?, float> costFunc) {
		const float minSize = 150.0f;
		const int NUM_CHEAP_AREAS = 32;

		(NavArea? area, float cost)[] cheapAreaSet = new (NavArea?, float)[NUM_CHEAP_AREAS];
		int cheapAreaSetCount = 0;

		foreach (NavArea area in NavArea.TheNavAreas) {
			if (area.GetSizeX() < minSize || area.GetSizeY() < minSize)
				continue;

			float cost = costFunc(area, startArea, null);

			if (cheapAreaSetCount < NUM_CHEAP_AREAS)
				cheapAreaSet[cheapAreaSetCount++] = (area, cost);
			else {
				int expensive = 0;
				for (int i = 1; i < NUM_CHEAP_AREAS; ++i)
					if (cheapAreaSet[i].cost > cheapAreaSet[expensive].cost)
						expensive = i;

				if (cheapAreaSet[expensive].cost > cost)
					cheapAreaSet[expensive] = (area, cost);
			}
		}

		if (cheapAreaSetCount > 0)
			return cheapAreaSet[RandomInt(0, cheapAreaSetCount - 1)].area;

		int numAreas = NavArea.TheNavAreas.Count;
		int which = RandomInt(0, numAreas - 1);
		foreach (NavArea area in NavArea.TheNavAreas) {
			if (which-- == 0)
				return area;
		}

		return cheapAreaSet[RandomInt(0, cheapAreaSetCount - 1)].area;
	}

	public static void SelectSeparatedShuffleSet(int maxCount, float minSeparation, List<NavArea> inVector, List<NavArea> outVector) {
		outVector.Clear();

		List<NavArea> shuffledVector = [.. inVector];

		int n = shuffledVector.Count;
		while (n > 1) {
			int k = RandomInt(0, n - 1);
			n--;
			(shuffledVector[n], shuffledVector[k]) = (shuffledVector[k], shuffledVector[n]);
		}

		for (int i = 0; i < shuffledVector.Count; ++i) {
			NavArea area = shuffledVector[i];

			List<NavArea> nearVector = [];
			CollectSurroundingAreas(nearVector, area, minSeparation, 2.0f * Nav.StepHeight, 2.0f * Nav.StepHeight);

			int j;
			for (j = 0; j < i; ++j) {
				if (nearVector.Contains(shuffledVector[j]))
					break;
			}

			if (j == i) {
				outVector.Add(area);
				if (outVector.Count >= maxCount)
					return;
			}
		}
	}
}