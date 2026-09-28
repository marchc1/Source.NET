using Source.Common;
using Source;
using System.Numerics;

namespace Game.Server;

using FIELD = FIELD<ScriptIntro>;

public class ScriptIntro : BaseEntity
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
		SendPropFloat(FIELD.OF_ARRAYINDEX(nameof(FadeColor), 0), 0, PropFlags.NoScale),
		SendPropArray(FIELD.OF_ARRAY(nameof(FadeColor))),
		SendPropFloat(FIELD.OF(nameof(FadeDuration)), 10, PropFlags.RoundDown),
		SendPropEHandle(FIELD.OF(nameof(CameraEntity))),
	]);
	public static new readonly ServerClass ServerClass = new ServerClass("ScriptIntro", DT_ScriptIntro).WithManualClassID(Shared.StaticClassIndices.CScriptIntro);
}
