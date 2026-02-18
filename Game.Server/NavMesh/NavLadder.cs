using Game.Shared;

using System.Numerics;

namespace Game.Server.NavMesh;

class NavLadder
{
	public enum LadderDirectionType
	{
		Up,
		Down,
		NumLadderDirections
	}
	Vector3 Top;
	Vector3 Bottom;
	float Length;
	float Width;
	NavArea? TopForwardArea;
	NavArea? TopLeftArea;
	NavArea? TopRightArea;
	NavArea? TopBehindArea;
	NavArea? BottomArea;
	EHANDLE LadderEntity;
	NavDirType dIR;
	Vector3 Normal;

	enum LadderConnectionType
	{
		TopForward = 0,
		TopLeft,
		TopRight,
		TopBehind,
		Bottom,
		NumLadderConnections
	}

	static uint NextID;
	uint ID;

	NavLadder() {
		TopForwardArea = null;
		TopRightArea = null;
		TopLeftArea = null;
		TopBehindArea = null;
		BottomArea = null;
		ID = NextID++;
	}

	void Shift(Vector3 shift) { }

	void CompressIDs() { }

	NavArea GetConnection(LadderConnectionType dir) {
		throw new NotImplementedException();
	}

	void OnSplit(NavArea original, NavArea alpha, NavArea beta) { }

	void ConnectTo(NavArea area) { }

	void OnDestroyNotify(NavArea dead) { }

	void Disconnect(NavArea area) { }

	bool IsConnected(NavArea area, LadderDirectionType dir) {
		throw new NotImplementedException();
	}

	void SetDir(NavDirType dir) { }

	void DrawLadder() { }

	void DrawConnectedAreas() { }

	void OnRoundRestart() { }

	void FindLadderEntity() { }

	void Save(ReadOnlySpan<char> fileBuffer, uint version) { }

	void Load(ReadOnlySpan<char> fileBuffer, uint version) { }

	bool IsInUse(BasePlayer ignore) {
		throw new NotImplementedException();
	}

	Vector3 GetPosAtHeight(float height) {
		throw new NotImplementedException();
	}

	bool IsUsableByTeam(int teamNumber) {
		throw new NotImplementedException();
	}

	BaseEntity GetLadderEntity() {
		throw new NotImplementedException();
	}

	NavDirType GetDir() {
		throw new NotImplementedException();
	}

	Vector3 GetNormal() {
		throw new NotImplementedException();
	}
}