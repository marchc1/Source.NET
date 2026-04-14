using Game.Shared;

using Source;
using Source.Common.Formats.BSP;

using System.Numerics;

namespace Game.Server.NavMesh;

public partial class NavLadder
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
	NavDirType Dir;
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

	public void Shift(Vector3 shift) {
		Top += shift;
		Bottom += shift;
	}

	public uint GetID() => ID;

	public static void CompressIDs() {
		NextID = 0;
		List<NavLadder> ladders = NavMesh.Instance!.GetLadders();
		for (int i = 0; i < ladders.Count; i++)
			ladders[i].ID = NextID++;
	}

	NavArea? GetConnection(LadderConnectionType dir) {
		return dir switch {
			LadderConnectionType.TopForward => TopForwardArea,
			LadderConnectionType.TopLeft => TopLeftArea,
			LadderConnectionType.TopRight => TopRightArea,
			LadderConnectionType.TopBehind => TopBehindArea,
			LadderConnectionType.Bottom => BottomArea,
			_ => null
		};
	}

	void SetConnection(LadderConnectionType dir, NavArea area) {
		switch (dir) {
			case LadderConnectionType.TopForward:
				TopForwardArea = area;
				break;
			case LadderConnectionType.TopLeft:
				TopLeftArea = area;
				break;
			case LadderConnectionType.TopRight:
				TopRightArea = area;
				break;
			case LadderConnectionType.TopBehind:
				TopBehindArea = area;
				break;
			case LadderConnectionType.Bottom:
				BottomArea = area;
				break;
		}
	}

	public void OnSplit(NavArea original, NavArea alpha, NavArea beta) {
		for (int i = 0; i < (int)LadderConnectionType.NumLadderConnections; i++) {
			LadderConnectionType con = (LadderConnectionType)i;
			NavArea? areaConnection = GetConnection(con);

			if (areaConnection != null && areaConnection == original) {
				float alphaDistance = alpha.GetDistanceSquaredToPoint(Top);
				float betaDistance = beta.GetDistanceSquaredToPoint(Top);

				if (alphaDistance < betaDistance)
					SetConnection(con, alpha);
				else
					SetConnection(con, beta);
			}
		}
	}

	public void ConnectTo(NavArea area) {
		float center = (Top.Z + Bottom.Z) * 0.5f;

		if (area.GetCenter().Z > center) {
			NavDirType dir;

			Vector3 dirVector = area.GetCenter() - Top;
			if (MathF.Abs(dirVector.X) > MathF.Abs(dirVector.Y))
				dir = (dirVector.X > 0.0f) ? NavDirType.East : NavDirType.West;
			else
				dir = (dirVector.Y > 0.0f) ? NavDirType.South : NavDirType.North;

			if (Dir == dir)
				TopBehindArea = area;
			else if (Nav.OppositeDirection(Dir) == dir)
				TopForwardArea = area;
			else if (Nav.DirectionLeft(Dir) == dir)
				TopLeftArea = area;
			else
				TopRightArea = area;
		}
		else
			BottomArea = area;
	}

	void OnDestroyNotify(NavArea dead) => Disconnect(dead);

	public void Disconnect(NavArea area) {
		if (TopForwardArea == area)
			TopForwardArea = null;
		else if (TopLeftArea == area)
			TopLeftArea = null;
		else if (TopRightArea == area)
			TopRightArea = null;
		else if (TopBehindArea == area)
			TopBehindArea = null;
		else if (BottomArea == area)
			BottomArea = null;
	}

	public bool IsConnected(NavArea area, LadderDirectionType dir) {
		if (dir == LadderDirectionType.Down)
			return area == BottomArea;
		else if (dir == LadderDirectionType.Up)
			return area == TopForwardArea || area == TopLeftArea || area == TopRightArea || area == TopBehindArea;
		else
			return area == BottomArea || area == TopForwardArea || area == TopLeftArea || area == TopRightArea || area == TopBehindArea;
	}

	public void SetDir(NavDirType dir) {
		Dir = dir;

		Normal = Vector3.Zero;
		Nav.AddDirectionVector(ref Normal, Dir, 1.0f);

		Vector3 from = (Top + Bottom) * 0.5f + Normal * 5.0f;
		Vector3 to = from - Normal * 32.0f;

		Util.TraceLine(from, to, Mask.NPCSolidBrushOnly, null, CollisionGroup.None, out Trace result);

		if (result.Fraction != 1.0f) {
			bool climbableSurface = physprops.GetSurfaceData(result.Surface.SurfaceProps)?.Game.Climbable != 0;
			if (!climbableSurface)
				climbableSurface = (result.Contents & Contents.Ladder) != 0;

			if (climbableSurface)
				Normal = result.Plane.Normal;
		}
	}

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

		fileBuffer.Write((uint)Dir);

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

		Dir = (NavDirType)fileBuffer.ReadUInt32();

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

	public NavDirType GetDir() {
		throw new NotImplementedException();
	}

	public Vector3 GetNormal() {
		throw new NotImplementedException();
	}
}