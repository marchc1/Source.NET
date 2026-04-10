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
	public Vector3 Top;
	public Vector3 Bottom;
	public float Length;
	public float Width;
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

	public bool IsConnected(NavArea area, LadderDirectionType dir) {
		throw new NotImplementedException();
	}

	void SetDir(NavDirType dir) { }

	public void DrawLadder() { }

	public void DrawConnectedAreas() { }

	void OnRoundRestart() { }

	void FindLadderEntity() { }

	public void Save(BinaryWriter fileBuffer, uint version) {
		fileBuffer.Write(ID);

		fileBuffer.Write(Width);

		fileBuffer.Write(Top.X);
		fileBuffer.Write(Top.Y);
		fileBuffer.Write(Top.Z);

		fileBuffer.Write(Bottom.X);
		fileBuffer.Write(Bottom.Y);
		fileBuffer.Write(Bottom.Z);

		fileBuffer.Write(Length);

		fileBuffer.Write((uint)dIR);

		uint id;
		id = (TopForwardArea != null) ? TopForwardArea.GetID() : 0;
		fileBuffer.Write(id);

		id = (TopLeftArea != null) ? TopLeftArea.GetID() : 0;
		fileBuffer.Write(id);

		id = (TopRightArea != null) ? TopRightArea.GetID() : 0;
		fileBuffer.Write(id);

		id = (TopBehindArea != null) ? TopBehindArea.GetID() : 0;
		fileBuffer.Write(id);

		id = (BottomArea != null) ? BottomArea.GetID() : 0;
		fileBuffer.Write(id);
	}

	public void Load(BinaryReader fileBuffer, uint version) {
		ID = fileBuffer.ReadUInt32();

		if (ID >= NextID)
			NextID = ID + 1;

		Width = fileBuffer.ReadSingle();

		float x = fileBuffer.ReadSingle();
		float y = fileBuffer.ReadSingle();
		float z = fileBuffer.ReadSingle();
		Top = new Vector3(x, y, z);

		x = fileBuffer.ReadSingle();
		y = fileBuffer.ReadSingle();
		z = fileBuffer.ReadSingle();
		Bottom = new Vector3(x, y, z);

		Length = fileBuffer.ReadSingle();

		dIR = (NavDirType)fileBuffer.ReadUInt32();

		uint id;
		id = fileBuffer.ReadUInt32();
		TopForwardArea = NavMesh.Instance!.GetNavAreaByID(id);

		id = fileBuffer.ReadUInt32();
		TopLeftArea = NavMesh.Instance!.GetNavAreaByID(id);

		id = fileBuffer.ReadUInt32();
		TopRightArea = NavMesh.Instance!.GetNavAreaByID(id);

		id = fileBuffer.ReadUInt32();
		TopBehindArea = NavMesh.Instance!.GetNavAreaByID(id);

		id = fileBuffer.ReadUInt32();
		BottomArea = NavMesh.Instance!.GetNavAreaByID(id);

		if (BottomArea == null) {
			DevMsg($"ERROR: Unconnected ladder #{ID} bottom at ( {Bottom.X}, {Bottom.Y}, {Bottom.Z} )\n");
			DevWarning($"nav_unmark; nav_mark ladder {ID}; nav_warp_to_mark\n");
		}
		else if (TopForwardArea == null && TopLeftArea == null && TopRightArea == null) {
			DevMsg($"ERROR: Unconnected ladder #{ID} top at ( {Top.X}, {Top.Y}, {Top.Z} )\n");
			DevWarning($"nav_unmark; nav_mark ladder {ID}; nav_warp_to_mark\n");
		}

		FindLadderEntity();
	}

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

	public Vector3 GetNormal() {
		throw new NotImplementedException();
	}
}