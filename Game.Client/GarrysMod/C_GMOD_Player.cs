using Game.Client.HL2MP;
using Game.Shared;

using Source;
using Source.Common;
using Source.Common.Commands;

using System.Numerics;

namespace Game.Client.GarrysMod;
using FIELD = FIELD<C_GMOD_Player>;


public class C_GMOD_Player() : C_HL2MP_Player()
{
	static ConVar cl_playercolor = new("1.0 0.0 0.0", FCvar.UserInfo | FCvar.Archive | FCvar.ServerCanExecute, "Default Player Model");

	public static readonly RecvTable DT_GMOD_Player = new(DT_HL2MP_Player, [
		RecvPropInt(FIELD.OF(nameof(GModPlayerFlags))),
		RecvPropEHandle(FIELD.OF(nameof(HoveredWidget))),
		RecvPropEHandle(FIELD.OF(nameof(PressedWidget))),
		RecvPropEHandle(FIELD.OF(nameof(Driving))),
		RecvPropInt(FIELD.OF(nameof(DrivingMode))),
		RecvPropInt(FIELD.OF(nameof(PlayerClass))),
		RecvPropBool(FIELD.OF(nameof(CanZoom))),
		RecvPropBool(FIELD.OF(nameof(CanWalk))),
		RecvPropBool(FIELD.OF(nameof(IsTyping))),
		RecvPropFloat(FIELD.OF(nameof(StepSize))),
		RecvPropFloat(FIELD.OF(nameof(JumpPower))),
		RecvPropVector(FIELD.OF(nameof(ViewOffset))),
		RecvPropVector(FIELD.OF(nameof(ViewOffsetDucked))),
		RecvPropFloat(FIELD.OF(nameof(GestureEndTime))),
		RecvPropVector(FIELD.OF(nameof(PlayerColor))),
		RecvPropVector(FIELD.OF(nameof(WeaponColor))),
		RecvPropEHandle(FIELD.OF(nameof(Hands))),
		RecvPropInt(FIELD.OF(nameof(WaterLevel))),
		RecvPropInt(FIELD.OF(nameof(MaxArmor))),
		RecvPropFloat(FIELD.OF(nameof(Gravity))),
		RecvPropBool(FIELD.OF(nameof(SprintEnabled))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass("GMOD_Player", null, null, DT_GMOD_Player)
															.WithManualClassID(StaticClassIndices.CGMOD_Player);

	public int GModPlayerFlags;
	public EHANDLE HoveredWidget = new();
	public EHANDLE PressedWidget = new();
	public EHANDLE Driving = new();
	public int DrivingMode;
	public int PlayerClass;
	public bool CanZoom;
	public bool CanWalk;
	public bool IsTyping;
	public float StepSize;
	public float JumpPower;
	public Vector3 ViewOffsetDucked;
	public float GestureEndTime;
	public Vector3 PlayerColor;
	public Vector3 WeaponColor;
	public EHANDLE Hands = new();
	public int MaxArmor;
	public float Gravity;
	public bool SprintEnabled;
}
