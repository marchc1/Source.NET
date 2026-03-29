using Game.Shared;

using Source;
using Source.Common;
using Source.Common.Mathematics;

namespace Game.Client;

using FIELD = FIELD<PlayerLocalData>;
using DEFINE = Source.DEFINE<Game.Client.PlayerLocalData>;


public class PlayerLocalData
{
	public static readonly DataMap PredMap = new(nameof(PlayerLocalData), [
		DEFINE.FIELD( nameof(StepSide), FieldType.Integer ),
		DEFINE.PRED_FIELD( nameof(HideHUD), FieldType.Integer, FieldTypeDescFlags.InSendTable ),
		DEFINE.PRED_FIELD_TOL( nameof(PunchAngle), FieldType.Vector, FieldTypeDescFlags.InSendTable, 0.125f ),
		DEFINE.PRED_FIELD_TOL( nameof(PunchAngleVel), FieldType.Vector, FieldTypeDescFlags.InSendTable, 0.125f ),
		DEFINE.PRED_FIELD( nameof(DrawViewmodel), FieldType.Boolean, FieldTypeDescFlags.InSendTable ),
		DEFINE.PRED_FIELD( nameof(WearingSuit), FieldType.Boolean, FieldTypeDescFlags.InSendTable ),
		DEFINE.PRED_FIELD( nameof(Poisoned), FieldType.Boolean, FieldTypeDescFlags.InSendTable ),
		DEFINE.PRED_FIELD( nameof(AllowAutoMovement), FieldType.Boolean, FieldTypeDescFlags.InSendTable ),
		DEFINE.PRED_FIELD( nameof(Ducked), FieldType.Boolean, FieldTypeDescFlags.InSendTable ),
		DEFINE.PRED_FIELD( nameof(Ducking), FieldType.Boolean, FieldTypeDescFlags.InSendTable ),
		DEFINE.PRED_FIELD( nameof(InDuckJump), FieldType.Boolean, FieldTypeDescFlags.InSendTable ),
		DEFINE.PRED_FIELD( nameof(DuckTime), FieldType.Float, FieldTypeDescFlags.InSendTable ),
		DEFINE.PRED_FIELD( nameof(DuckJumpTime), FieldType.Float, FieldTypeDescFlags.InSendTable ),
		DEFINE.PRED_FIELD( nameof(JumpTime), FieldType.Float, FieldTypeDescFlags.InSendTable ),
		DEFINE.PRED_FIELD_TOL( nameof(FallVelocity), FieldType.Float, FieldTypeDescFlags.InSendTable, 0.5f ),
		DEFINE.FIELD( nameof(OldButtons), FieldType.Integer ),
		DEFINE.FIELD( nameof(OldForwardMove), FieldType.Float ),
		DEFINE.PRED_FIELD( nameof(StepSize), FieldType.Float, FieldTypeDescFlags.InSendTable ),
		DEFINE.FIELD( nameof(FOVRate), FieldType.Float ),
	]);

	public static readonly RecvTable DT_Local = new([
		RecvPropArray3(FIELD.OF_ARRAY(nameof(AreaBits)), RecvPropInt(FIELD.OF_ARRAYINDEX(nameof(AreaBits)))),
		RecvPropArray3(FIELD.OF_ARRAY(nameof(AreaPortalBits)), RecvPropInt(FIELD.OF_ARRAYINDEX(nameof(AreaPortalBits)))),
		RecvPropInt(FIELD.OF(nameof(HideHUD))),
		RecvPropFloat(FIELD.OF(nameof(FOVRate))),
		RecvPropInt(FIELD.OF(nameof(Ducked))),
		RecvPropInt(FIELD.OF(nameof(Ducking))),
		RecvPropInt(FIELD.OF(nameof(InDuckJump))),
		RecvPropFloat(FIELD.OF(nameof(DuckTime))),
		RecvPropFloat(FIELD.OF(nameof(DuckJumpTime))),
		RecvPropFloat(FIELD.OF(nameof(JumpTime))),
		RecvPropFloat(FIELD.OF(nameof(FallVelocity))),
		RecvPropVector(FIELD.OF(nameof(PunchAngle))),
		RecvPropVector(FIELD.OF(nameof(PunchAngleVel))),
		RecvPropInt(FIELD.OF(nameof(DrawViewmodel))),
		RecvPropInt(FIELD.OF(nameof(WearingSuit))),
		RecvPropBool(FIELD.OF(nameof(Poisoned))),
		RecvPropFloat(FIELD.OF(nameof(StepSize))),
		RecvPropInt(FIELD.OF(nameof(AllowAutoMovement))),

		RecvPropInt(FIELD.OF("Skybox3D.Scale")),
		RecvPropVector(FIELD.OF("Skybox3D.Origin")),
		RecvPropInt(FIELD.OF("Skybox3D.Area")),
		RecvPropInt(FIELD.OF("Skybox3D.Fog.Enable")),
		RecvPropInt(FIELD.OF("Skybox3D.Fog.Blend")),
		RecvPropInt(FIELD.OF("Skybox3D.Fog.Radial")),
		RecvPropVector(FIELD.OF("Skybox3D.Fog.DirPrimary")),
		RecvPropInt(FIELD.OF("Skybox3D.Fog.ColorPrimary")),
		RecvPropInt(FIELD.OF("Skybox3D.Fog.ColorSecondary")),
		RecvPropInt(FIELD.OF("Skybox3D.Fog.ColorPrimaryHDR")),
		RecvPropInt(FIELD.OF("Skybox3D.Fog.ColorSecondaryHDR")),
		RecvPropFloat(FIELD.OF("Skybox3D.Fog.Start")),
		RecvPropFloat(FIELD.OF("Skybox3D.Fog.End")),
		RecvPropFloat(FIELD.OF("Skybox3D.Fog.MaxDensity")),
		RecvPropFloat(FIELD.OF("Skybox3D.Fog.HDRColorScale")),

		RecvPropEHandle( FIELD.OF("PlayerFog.Ctrl")),

		RecvPropVector(FIELD.OF_ARRAYINDEX("Audio.LocalSound", 0)),
		RecvPropVector(FIELD.OF_ARRAYINDEX("Audio.LocalSound", 1)),
		RecvPropVector(FIELD.OF_ARRAYINDEX("Audio.LocalSound", 2)),
		RecvPropVector(FIELD.OF_ARRAYINDEX("Audio.LocalSound", 3)),
		RecvPropVector(FIELD.OF_ARRAYINDEX("Audio.LocalSound", 4)),
		RecvPropVector(FIELD.OF_ARRAYINDEX("Audio.LocalSound", 5)),
		RecvPropVector(FIELD.OF_ARRAYINDEX("Audio.LocalSound", 6)),
		RecvPropVector(FIELD.OF_ARRAYINDEX("Audio.LocalSound", 7)),
		RecvPropInt(FIELD.OF("Audio.SoundscapeIndex")),
		RecvPropInt(FIELD.OF("Audio.LocalBits")),
		RecvPropEHandle( FIELD.OF("Audio.Ent")),

		// gmod data
		RecvPropFloat(FIELD.OF(nameof(SprintSpeed))),
		RecvPropFloat(FIELD.OF(nameof(WalkSpeed))),
		RecvPropFloat(FIELD.OF(nameof(SlowWalkSpeed))),
		RecvPropFloat(FIELD.OF(nameof(LadderSpeed))),
		RecvPropFloat(FIELD.OF(nameof(CrouchedWalkSpeed))),
		RecvPropFloat(FIELD.OF(nameof(DuckSpeed))),
		RecvPropFloat(FIELD.OF(nameof(UnDuckSpeed))),
		RecvPropBool(FIELD.OF(nameof(DuckToggled))),
	]); public static readonly ClientClass CC_Local = new ClientClass("Local", null, null, DT_Local);

	public float SprintSpeed;
	public float WalkSpeed;
	public float SlowWalkSpeed;
	public float LadderSpeed;
	public float CrouchedWalkSpeed;
	public float DuckSpeed;
	public float UnDuckSpeed;
	public bool DuckToggled;

	// TODO: NETWORK VARS!!!!!
	public InlineArrayMaxAreaStateBytes<byte> AreaBits;
	public InlineArrayMaxAreaPortalStateBytes<byte> AreaPortalBits;
	public int HideHUD;
	public float FOVRate;
	public bool Ducked;
	public bool Ducking;
	public bool InDuckJump;
	public double DuckTime;
	public double DuckJumpTime;
	public double JumpTime;
	public int StepSide;
	public float FallVelocity;
	public int OldButtons;
	public int OldForwardMove;
	public QAngle PunchAngle;
	public readonly InterpolatedVar<QAngle> iv_PunchAngle;
	public QAngle PunchAngleVel;
	public readonly InterpolatedVar<QAngle> iv_PunchAngleVel;
	public bool DrawViewmodel;
	public bool WearingSuit;
	public bool Poisoned;
	public float StepSize;
	public bool AllowAutoMovement;
	public bool SlowMovement;

	public Sky3DParams Skybox3D = new();
	public FogPlayerParams PlayerFog = new();
	public AudioParams Audio = new();

	public static readonly DynamicAccessor FIELD_PUNCHANGLE = FIELD.OF(nameof(PunchAngle));
	public static readonly DynamicAccessor FIELD_PUNCHANGLEVEL = FIELD.OF(nameof(PunchAngleVel));

	public PlayerLocalData() {
		iv_PunchAngle = new("PlayerLocalData.iv_PunchAngle");
		iv_PunchAngleVel = new("PlayerLocalData.iv_PunchAngleVel");

		iv_PunchAngle.Setup(this, FIELD_PUNCHANGLE, LatchFlags.LatchSimulationVar);
		iv_PunchAngleVel.Setup(this, FIELD_PUNCHANGLEVEL, LatchFlags.LatchSimulationVar);
		FOVRate = 0;
	}
}
