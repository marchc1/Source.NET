using Source.Common;
using Source;
using System.Numerics;

namespace Game.Client;

using FIELD = FIELD<C_ScriptIntro>;

[NetworkName("CScriptIntro")]
public class C_ScriptIntro : C_BaseEntity
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

	public static readonly RecvTable DT_ScriptIntro = new(DT_BaseEntity, [
		RecvPropVector(FIELD.OF(nameof(CameraView))),
		RecvPropVector(FIELD.OF(nameof(CameraViewAngles))),
		RecvPropInt(FIELD.OF(nameof(BlendMode))),
		RecvPropInt(FIELD.OF(nameof(NextBlendMode))),
		RecvPropVectorXY(FIELD.OF(nameof(NextBlendTime))),
		RecvPropVectorXY(FIELD.OF(nameof(BlendStartTime))),
		RecvPropBool(FIELD.OF(nameof(Active))),
		RecvPropInt(FIELD.OF(nameof(FOV))),
		RecvPropInt(FIELD.OF(nameof(NextFOV))),
		RecvPropInt(FIELD.OF(nameof(StartFOV))),
		RecvPropVectorXY(FIELD.OF(nameof(NextFOVBlendTime))),
		RecvPropVectorXY(FIELD.OF(nameof(FOVBlendStartTime))),
		RecvPropBool(FIELD.OF(nameof(AlternateFOV))),
		RecvPropFloat(FIELD.OF(nameof(FadeAlpha))),
		RecvPropFloat(FIELD.OF_ARRAYINDEX(nameof(FadeColor), 0)),
		RecvPropArray(FIELD.OF_ARRAY(nameof(FadeColor))),
		RecvPropFloat(FIELD.OF(nameof(FadeDuration))),
		RecvPropEHandle(FIELD.OF(nameof(CameraEntity))),
	]);
	public static new readonly ClientClass ClientClass = new ClientClass(DT_ScriptIntro);
}
