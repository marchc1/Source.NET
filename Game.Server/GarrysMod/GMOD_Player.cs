global using static Game.Server.GarrysMod.GMOD_PlayerGlobals;

using Game.Server.HL2MP;
using Game.Shared;

using Source;
using Source.Common;
using Source.Common.Engine;

using System.Numerics;

namespace Game.Server.GarrysMod;

using FIELD = FIELD<GMOD_Player>;

public static class GMOD_PlayerGlobals
{
	public static GMOD_Player? ToGMODPlayer(BaseEntity? entity) {
		if (entity == null || !entity.IsPlayer())
			return null;
		return (GMOD_Player?)entity;
	}
}

public class GMOD_Player : HL2MP_Player
{
	static Edict? s_PlayerEdict;
	public static GMOD_Player CreatePlayer(ReadOnlySpan<char> classname, Edict ed) {
		s_PlayerEdict = ed;
		return (GMOD_Player?)CreateEntityByName(classname);
	}
	public static readonly SendTable DT_GMOD_Player = new(DT_HL2MP_Player, [
		SendPropInt(FIELD.OF(nameof(GModPlayerFlags)), 5, 0),
		SendPropEHandle(FIELD.OF(nameof(HoveredWidget))),
		SendPropEHandle(FIELD.OF(nameof(PressedWidget))),
		SendPropEHandle(FIELD.OF(nameof(Driving))),
		SendPropInt(FIELD.OF(nameof(DrivingMode)), 15, 0),
		SendPropInt(FIELD.OF(nameof(PlayerClass)), 15, 0),
		SendPropBool(FIELD.OF(nameof(CanZoom))),
		SendPropBool(FIELD.OF(nameof(CanWalk))),
		SendPropBool(FIELD.OF(nameof(IsTyping))),
		SendPropFloat(FIELD.OF(nameof(StepSize)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(JumpPower)), 0, PropFlags.NoScale),
		SendPropVector(FIELD.OF(nameof(ViewOffset)), 0, PropFlags.NoScale),
		SendPropVector(FIELD.OF(nameof(ViewOffsetDucked)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(GestureEndTime)), 0, PropFlags.NoScale),
		SendPropVector(FIELD.OF(nameof(PlayerColor)), 0, PropFlags.NoScale),
		SendPropVector(FIELD.OF(nameof(WeaponColor)), 0, PropFlags.NoScale),
		SendPropEHandle(FIELD.OF(nameof(Hands))),
		SendPropInt(FIELD.OF(nameof(WaterLevel)), 2, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(MaxArmor)), 32, 0),
		SendPropFloat(FIELD.OF(nameof(Gravity)), 0, PropFlags.NoScale),
		SendPropBool(FIELD.OF(nameof(SprintEnabled))),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass("GMOD_Player", DT_GMOD_Player)
															.WithManualClassID(StaticClassIndices.CGMOD_Player);

	public override void InitialSpawn() {
		base.InitialSpawn();
	}

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
