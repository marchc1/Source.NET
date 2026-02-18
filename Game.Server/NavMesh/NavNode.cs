using System.Numerics;

namespace Game.Server.NavMesh;

public class NavNode
{
	NavNode(Vector3 pos, Vector3 normal, NavNode parent, bool isOnDisplacement) { }

	void CleanupGeneration() { }

	void Draw() { }

	float GetGroundHeightAboveNode(NavCornerType cornerType) {
		throw new NotImplementedException();
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
