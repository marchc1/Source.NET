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

[LinkEntityToClass("player")]
[NetworkName("CGMOD_Player")]
public partial class GMOD_Player : HL2MP_Player
{
	public static GMOD_Player? CreatePlayer(ReadOnlySpan<char> classname, Edict ed) {
		s_PlayerEdict = ed;
		return (GMOD_Player?)CreateEntityByName(classname);
	}
	public static readonly SendTable DT_GMOD_Player = new(DT_HL2MP_Player, [
		SendPropInt(NetworkVarFields.GModPlayerFlags, 5, 0),
		SendPropEHandle(FIELD.OF(nameof(HoveredWidget))),
		SendPropEHandle(FIELD.OF(nameof(PressedWidget))),
		SendPropEHandle(FIELD.OF(nameof(Driving))),
		SendPropInt(NetworkVarFields.DrivingMode, 15, 0),
		SendPropInt(NetworkVarFields.PlayerClass, 15, 0),
		SendPropBool(FIELD.OF(nameof(CanZoom))),
		SendPropBool(FIELD.OF(nameof(CanWalk))),
		SendPropBool(NetworkVarFields.IsTyping),
		SendPropFloat(FIELD.OF(nameof(StepSize)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(JumpPower)), 0, PropFlags.NoScale),
		SendPropVector(FIELD.OF_NAMED(nameof(ViewOffset), "m_ViewOffset"), 0, PropFlags.NoScale),
		SendPropVector(NetworkVarFields.ViewOffsetDucked, 0, PropFlags.NoScale),
		SendPropFloat(NetworkVarFields.GestureEndTime, 0, PropFlags.NoScale),
		SendPropVector(FIELD.OF(nameof(PlayerColor)), 0, PropFlags.NoScale),
		SendPropVector(FIELD.OF(nameof(WeaponColor)), 0, PropFlags.NoScale),
		SendPropEHandle(FIELD.OF(nameof(Hands))),
		SendPropInt(BaseEntity.NetworkVarFields.WaterLevel, 2, PropFlags.Unsigned),
		SendPropInt(NetworkVarFields.MaxArmor, 32, 0),
		SendPropFloat(NetworkVarFields.Gravity, 0, PropFlags.NoScale),
		SendPropBool(NetworkVarFields.SprintEnabled),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_GMOD_Player);

	public override void InitialSpawn() {
		base.InitialSpawn();
	}

	[NetworkName("m_iGModPlayerFlags")]
	[NetworkVar] public partial int GModPlayerFlags { get; set; }
	[NetworkName("m_HoveredWidget")]
	public EHANDLE HoveredWidget = new();
	[NetworkName("m_PressedWidget")]
	public EHANDLE PressedWidget = new();
	[NetworkName("m_Driving")]
	public EHANDLE Driving = new();
	[NetworkName("m_DrivingMode")]
	[NetworkVar] public partial int DrivingMode { get; set; }
	[NetworkName("m_PlayerClass")]
	[NetworkVar] public partial int PlayerClass { get; set; }
	[NetworkName("m_bCanZoom")]
	public bool CanZoom = true;
	[NetworkName("m_bCanWalk")]
	public bool CanWalk = true;
	[NetworkName("m_bIsTyping")]
	[NetworkVar] public partial bool IsTyping { get; set; }
	[NetworkName("m_StepSize")]
	public float StepSize = 18;
	[NetworkName("m_JumpPower")]
	public float JumpPower = 200;
	[NetworkName("m_ViewOffsetDucked")]
	[NetworkVar] public partial Vector3 ViewOffsetDucked { get; set; }
	[NetworkName("m_fGestureEndTime")]
	[NetworkVar] public partial float GestureEndTime { get; set; }
	[NetworkName("m_PlayerColor")]
	public Vector3 PlayerColor = new(255, 255, 255);
	[NetworkName("m_WeaponColor")]
	public Vector3 WeaponColor = new(255, 255, 255);
	[NetworkName("m_Hands")]
	public EHANDLE Hands = new();
	[NetworkName("m_iMaxArmor")]
	[NetworkVar] public partial int MaxArmor { get; set; }
	[NetworkName("m_flGravity")]
	[NetworkVar] public partial float Gravity { get; set; }
	[NetworkName("m_bSprintEnabled")]
	[NetworkVar] public partial bool SprintEnabled { get; set; }

	public override void PlayerRunCommand(UserCmd ucmd, IMoveHelper moveHelper) {
		base.PlayerRunCommand(ucmd, moveHelper);
	}

	public override void CheckChatText(ReadOnlySpan<char> text) {
		if (!stristr(text, "bloxwich").IsEmpty)
			AwardAchievement((int)GMODAchievementID.GMA_SAY_1, 1);
	}
}
