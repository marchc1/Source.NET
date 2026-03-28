using Game.Shared;

using System.Numerics;

namespace Game.Server.NavMesh;

public class NavLadder
{
	public enum LadderDirectionType
	{
		Up,
		Down,
		NumLadderDirections
	}
	Vector3 Top;
	Vector3 Bottom;
	public float Length;
	float Width;
	public NavArea? TopForwardArea;
	public NavArea? TopLeftArea;
	public NavArea? TopRightArea;
	public NavArea? TopBehindArea;
	public NavArea? BottomArea;
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
	public uint ID;

	public NavLadder() {
		TopForwardArea = null;
		TopRightArea = null;
		TopLeftArea = null;
		TopBehindArea = null;
		BottomArea = null;
		ID = NextID++;
	}

	void Shift(Vector3 shift) { }

	public uint GetID() => ID;

	public static void CompressIDs() {
		NextID = 0;
		List<NavLadder> ladders = NavMesh.Instance!.GetLadders();
		for (int i = 0; i < ladders.Count; i++)
			ladders[i].ID = NextID++;
	}

	NavArea GetConnection(LadderConnectionType dir) {
		throw new NotImplementedException();
	}

	public void OnSplit(NavArea original, NavArea alpha, NavArea beta) { }

	void ConnectTo(NavArea area) { }

	void OnDestroyNotify(NavArea dead) { }

	void Disconnect(NavArea area) { }

	bool IsConnected(NavArea area, LadderDirectionType dir) {
		throw new NotImplementedException();
	}

	void SetDir(NavDirType dir) { }

	public void DrawLadder() { }

	public void DrawConnectedAreas() { }

	void OnRoundRestart() { }

	void FindLadderEntity() { }

	public void Save(BinaryWriter fileBuffer, uint version) {
		DevWarning("NavLadder::Save: not implemented\n");
	}

	public void Load(BinaryReader fileBuffer, uint version) { }

	bool IsInUse(BasePlayer ignore) {
		throw new NotImplementedException();
	}

	Vector3 GetPosAtHeight(float height) {
		throw new NotImplementedException();
	}

	bool IsUsableByTeam(int teamNumber) {
		throw new NotImplementedException();
	}

	public BaseEntity GetLadderEntity() {
		throw new NotImplementedException();
	}

	NavDirType GetDir() {
		throw new NotImplementedException();
	}

	Vector3 GetNormal() {
		throw new NotImplementedException();
	}
}