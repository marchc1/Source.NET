using static Game.Server.NavMesh.NavColors;
using static Game.Server.NavMesh.Nav;

using Game.Shared;

using Source;
using Source.Common.Formats.BSP;
using Source.Common.Mathematics;

using System.Numerics;
using Source.Common;

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
			else if (OppositeDirection(Dir) == dir)
				TopForwardArea = area;
			else if (DirectionLeft(Dir) == dir)
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
		AddDirectionVector(ref Normal, Dir, 1.0f);

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

	public void DrawLadder() {
		BasePlayer? player = Util.GetListenServerHost();
		if (player == null)
			return;

		Vector3 eye = player.EyePosition();
		// MathLib.AngleVectors(player.EyeAngles() + player.GetPunchAngle(), out Vector3 dir);
		// todo punch angle
		MathLib.AngleVectors(player.EyeAngles(), out Vector3 dir);

		float dx = eye.X - Bottom.X;
		float dy = eye.Y - Bottom.Y;

		Vector2 eyeDir = new(dx, dy);
		eyeDir.NormalizeInPlace();
		bool isSelected = this == NavMesh.Instance!.GetSelectedLadder();
		bool isMarked = this == NavMesh.Instance!.GetMarkedLadder();
		bool isFront = MathLib.DotProduct2D(eyeDir, GetNormal().AsVector2D()) > 0;

		if (NavMesh.Instance!.IsEditMode(NavMesh.EditModeType.PlacePainting)) {
			isSelected = isMarked = false;
			isFront = true;
		}

		BaseEntity? ladderEntity = LadderEntity.Get();
		// ladderEntity?.DrawAbsBoxOverlay(); // todo

		NavEditColor ladderColor = NavEditColor.NavNormalColor;
		if (isFront) {
			if (isMarked)
				ladderColor = NavEditColor.NavMarkedColor;
			else if (isSelected)
				ladderColor = NavEditColor.NavSelectedColor;
			else
				ladderColor = NavEditColor.NavSamePlaceColor;
		}
		else if (isMarked)
			ladderColor = NavEditColor.NavMarkedColor;
		else if (isSelected)
			ladderColor = NavEditColor.NavSelectedColor;

		MathLib.VectorVectors(GetNormal(), out AngularImpulse right, out AngularImpulse up);
		if (up.Z <= 0.0f) {
			AssertMsg(false, "A nav ladder has an invalid normal");
			up.Init(0, 0, 1);
		}

		right *= Width * 0.5f;

		Vector3 bottomLeft = Bottom - right;
		Vector3 bottomRight = Bottom + right;
		Vector3 topLeft = Top - right;
		Vector3 topRight = Top + right;

		int[] bgcolor = new int[4];
		ScanF scan = new(nav_area_bgcolor.GetString(), "%d %d %d %d");
		scan.Read(out bgcolor[0]).Read(out bgcolor[1]).Read(out bgcolor[2]).Read(out bgcolor[3]);
		if (4 == scan.ReadArguments) {
			for (int i = 0; i < 4; ++i)
				bgcolor[i] = Math.Clamp(bgcolor[i], 0, 255);

			if (bgcolor[3] > 0) {
				Vector3 offset = new(0, 0, 0);
				AddDirectionVector(ref offset, OppositeDirection(Dir), 1);
				Shared.DebugOverlay.Triangle(topLeft + offset, topRight + offset, bottomRight + offset, bgcolor[0], bgcolor[1], bgcolor[2], bgcolor[3], true, 0.15f);
				Shared.DebugOverlay.Triangle(bottomRight + offset, bottomLeft + offset, topLeft + offset, bgcolor[0], bgcolor[1], bgcolor[2], bgcolor[3], true, 0.15f);
			}
		}

		NavDrawLine(topLeft, bottomLeft, ladderColor);
		NavDrawLine(topRight, bottomRight, ladderColor);

		while (bottomRight.Z < topRight.Z) {
			NavDrawLine(bottomRight, bottomLeft, ladderColor);
			bottomRight += up * (GenerationStepSize / 2);
			bottomLeft += up * (GenerationStepSize / 2);
		}

		if (!NavMesh.Instance!.IsEditMode(NavMesh.EditModeType.PlacePainting)) {
			Vector3 bottom = Bottom;
			Vector3 top = Top;

			NavDrawLine(top, bottom, NavEditColor.NavConnectedTwoWaysColor);

			if (BottomArea != null) {
				float offset = GenerationStepSize;
				Vector3 areaBottom = BottomArea.GetCenter();

				if (top.Z - bottom.Z < GenerationStepSize * 1.5f)
					offset = 0.0f;

				if (bottom.Z - areaBottom.Z > GenerationStepSize * 1.5f)
					offset = 0.0f;

				NavDrawLine(bottom + new Vector3(0, 0, offset), areaBottom, BottomArea.IsConnected(this, LadderDirectionType.Down) ? NavEditColor.NavConnectedTwoWaysColor : NavEditColor.NavConnectedOneWayColor);
			}

			if (TopForwardArea != null)
				NavDrawLine(top, TopForwardArea.GetCenter(), TopForwardArea.IsConnected(this, LadderDirectionType.Down) ? NavEditColor.NavConnectedTwoWaysColor : NavEditColor.NavConnectedOneWayColor);

			if (TopLeftArea != null)
				NavDrawLine(top, TopLeftArea.GetCenter(), TopLeftArea.IsConnected(this, LadderDirectionType.Down) ? NavEditColor.NavConnectedTwoWaysColor : NavEditColor.NavConnectedOneWayColor);

			if (TopRightArea != null)
				NavDrawLine(top, TopRightArea.GetCenter(), TopRightArea.IsConnected(this, LadderDirectionType.Down) ? NavEditColor.NavConnectedTwoWaysColor : NavEditColor.NavConnectedOneWayColor);

			if (TopBehindArea != null)
				NavDrawLine(top, TopBehindArea.GetCenter(), TopBehindArea.IsConnected(this, LadderDirectionType.Down) ? NavEditColor.NavConnectedTwoWaysColor : NavEditColor.NavConnectedOneWayColor);
		}
	}

	public void DrawConnectedAreas() {
		List<NavArea> areas = [];
		if (TopForwardArea != null)
			areas.Add(TopForwardArea);
		if (TopLeftArea != null)
			areas.Add(TopLeftArea);
		if (TopRightArea != null)
			areas.Add(TopRightArea);
		if (TopBehindArea != null)
			areas.Add(TopBehindArea);
		if (BottomArea != null)
			areas.Add(BottomArea);

		foreach (NavArea area in areas) {
			area.Draw();

			if (!NavMesh.Instance!.IsEditMode(NavMesh.EditModeType.PlacePainting))
				area.DrawHidingSpots();
		}
	}

	void OnRoundRestart() => FindLadderEntity();

	void FindLadderEntity() {
		// todo
		// LadderEntity = gEntList.FindEntityByClassnameNearest( "func_simpleladder", (Top + Bottom) * 0.5f, HalfHumanWidth );
	}

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
		IsLadderFreeFunctor functor = new(this, ignore);
		// return ForEachPlayer(functor.Invoke) == false;
		return false; // TODO
	}

	Vector3 GetPosAtHeight(float height) {
		if (height < Bottom.Z)
			return Bottom;

		if (height > Top.Z)
			return Top;

		if (Top.Z == Bottom.Z)
			return Top;

		float percent = (height - Bottom.Z) / (Top.Z - Bottom.Z);

		return Top * percent + Bottom * (1.0f - percent);
	}

	bool IsUsableByTeam(int teamNumber) {
		if (LadderEntity.Get() == null)
			return true;

		// int ladderTeam = LadderEntity.Get().GetTeamNumber();
		int ladderTeam = Constants.TEAM_UNASSIGNED; // TODO
		return teamNumber == ladderTeam || ladderTeam == Constants.TEAM_UNASSIGNED;
	}

	public BaseEntity? GetLadderEntity() => LadderEntity.Get();

	public NavDirType GetDir() => Dir;

	public Vector3 GetNormal() => Normal;
}

class IsLadderFreeFunctor(NavLadder ladder, BasePlayer ignore)
{
	NavLadder Ladder = ladder;
	BasePlayer Ignore = ignore;

	public bool Invoke(BasePlayer player) {
		if (player == Ignore)
			return true;

		// if (!player.IsOnLadder()) // TODO
		// 	return true;

		Vector3 feet = player.GetAbsOrigin();

		if (feet.Z > Ladder.Top.Z + HalfHumanHeight)
			return true;

		if (feet.Z + HumanHeight < Ladder.Bottom.Z - HalfHumanHeight)
			return true;

		Vector2 away = new(Ladder.Bottom.X - feet.X, Ladder.Bottom.Y - feet.Y);
		const float onLadderRange = 50.0f;
		return away.Length() > onLadderRange;
	}
}