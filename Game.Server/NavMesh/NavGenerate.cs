using Source.Common.Commands;

using System.Numerics;

namespace Game.Server.NavMesh;

class NavGenerate
{

}

class ApproachAreaCost
{

}

class JumpConnector
{

}

class IncrementallyGeneratedAreas
{

}

class AreaSet
{

}

class TestOverlapping
{

}

class Subdivider
{

}

public partial class NavMesh
{
	void BuildLadders() { }

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

	void CommandNavRemoveJumpAreas() { }

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

	void BeginGeneration(bool incremental) { }

	void BeginAnalysis(bool quitWhenFinished) { }

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