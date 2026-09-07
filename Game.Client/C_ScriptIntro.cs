using Source.Common;
using Source;
using System.Numerics;

namespace Game.Client;

using FIELD = FIELD<C_ScriptIntro>;

public class C_ScriptIntro : C_BaseEntity
{
	public Vector3 CameraView;
	public Vector3 CameraViewAngles;
	public int BlendMode;
	public int NextBlendMode;
	public Vector3 NextBlendTime;
	public Vector3 BlendStartTime;
	public bool Active;
	public int FOV;
	public int NextFOV;
	public int StartFOV;
	public Vector3 NextFOVBlendTime;
	public Vector3 FOVBlendStartTime;
	public bool AlternateFOV;
	public float FadeAlpha;
	public InlineArray3<float> FadeColor;
	public float FadeDuration;
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
	public static new readonly ClientClass ClientClass = new ClientClass("ScriptIntro", DT_ScriptIntro).WithManualClassID(Shared.StaticClassIndices.CScriptIntro);
}
