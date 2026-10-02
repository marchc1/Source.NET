using Game.Shared;
using Source.Common;
using Source;
using System.Numerics;

namespace Game.Server;

using FIELD = FIELD<ScriptIntro>;

[LinkEntityToClass("script_intro")]
[NetworkName("CScriptIntro")]
public class ScriptIntro : BaseEntity
{
	[NetworkName("m_vecCameraView")]
	public Vector3 CameraView;
	[NetworkName("m_vecCameraViewAngles")]
	public Vector3 CameraViewAngles;
	[NetworkName("m_iBlendMode")]
	public int BlendMode;
	[NetworkName("m_iNextBlendMode")]
	public int NextBlendMode;
	[NetworkName("m_flNextBlendTime")]
	public Vector3 NextBlendTime;
	[NetworkName("m_flBlendStartTime")]
	public Vector3 BlendStartTime;
	[NetworkName("m_bActive")]
	public bool Active;
	[NetworkName("m_iFOV")]
	public int FOV;
	[NetworkName("m_iNextFOV")]
	public int NextFOV;
	[NetworkName("m_iStartFOV")]
	public int StartFOV;
	[NetworkName("m_flNextFOVBlendTime")]
	public Vector3 NextFOVBlendTime;
	[NetworkName("m_flFOVBlendStartTime")]
	public Vector3 FOVBlendStartTime;
	[NetworkName("m_bAlternateFOV")]
	public bool AlternateFOV;
	[NetworkName("m_flFadeAlpha")]
	public float FadeAlpha;
	[NetworkName("m_flFadeColor")]
	public InlineArray3<float> FadeColor;
	[NetworkName("m_flFadeDuration")]
	public float FadeDuration;
	[NetworkName("m_hCameraEntity")]
	public EHANDLE CameraEntity;

	public static readonly SendTable DT_ScriptIntro = new(DT_BaseEntity, [
		SendPropVector(FIELD.OF(nameof(CameraView)), 0, PropFlags.Coord),
		SendPropVector(FIELD.OF(nameof(CameraViewAngles)), 0, PropFlags.Coord),
		SendPropInt(FIELD.OF(nameof(BlendMode)), 5),
		SendPropInt(FIELD.OF(nameof(NextBlendMode)), 5),
		SendPropVectorXY(FIELD.OF(nameof(NextBlendTime)), 0, PropFlags.NoScale),
		SendPropVectorXY(FIELD.OF(nameof(BlendStartTime)), 0, PropFlags.NoScale),
		SendPropBool(FIELD.OF(nameof(Active))),
		SendPropInt(FIELD.OF(nameof(FOV)), 9),
		SendPropInt(FIELD.OF(nameof(NextFOV)), 9),
		SendPropInt(FIELD.OF(nameof(StartFOV)), 9),
		SendPropVectorXY(FIELD.OF(nameof(NextFOVBlendTime)), 0, PropFlags.NoScale),
		SendPropVectorXY(FIELD.OF(nameof(FOVBlendStartTime)), 0, PropFlags.NoScale),
		SendPropBool(FIELD.OF(nameof(AlternateFOV))),
		SendPropFloat(FIELD.OF(nameof(FadeAlpha)), 10),
		SendPropFloat(FIELD.OF_SENDINFO_ARRAY(nameof(FadeColor)), 0, PropFlags.NoScale),
		SendPropArray(FIELD.OF_ARRAY(nameof(FadeColor))),
		SendPropFloat(FIELD.OF(nameof(FadeDuration)), 10, PropFlags.RoundDown, 0.0f, 255.0f),
		SendPropEHandle(FIELD.OF(nameof(CameraEntity))),
	]);
	public static new readonly ServerClass ServerClass = new ServerClass(DT_ScriptIntro);
}
