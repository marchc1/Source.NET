using Game.Shared;
using Source.Common;
using Source;
using System.Numerics;

namespace Game.Server;

using FIELD = FIELD<ScriptIntro>;

[NetworkName("CScriptIntro")]
public partial class ScriptIntro : BaseEntity
{
	[NetworkName("m_vecCameraView")]
	[NetworkVar] public partial Vector3 CameraView { get; set; }
	[NetworkName("m_vecCameraViewAngles")]
	[NetworkVar] public partial Vector3 CameraViewAngles { get; set; }
	[NetworkName("m_iBlendMode")]
	[NetworkVar] public partial int BlendMode { get; set; }
	[NetworkName("m_iNextBlendMode")]
	[NetworkVar] public partial int NextBlendMode { get; set; }
	[NetworkName("m_flNextBlendTime")]
	[NetworkVar] public partial Vector3 NextBlendTime { get; set; }
	[NetworkName("m_flBlendStartTime")]
	[NetworkVar] public partial Vector3 BlendStartTime { get; set; }
	[NetworkName("m_bActive")]
	[NetworkVar] public partial bool Active { get; set; }
	[NetworkName("m_iFOV")]
	[NetworkVar] public partial int FOV { get; set; }
	[NetworkName("m_iNextFOV")]
	[NetworkVar] public partial int NextFOV { get; set; }
	[NetworkName("m_iStartFOV")]
	[NetworkVar] public partial int StartFOV { get; set; }
	[NetworkName("m_flNextFOVBlendTime")]
	[NetworkVar] public partial Vector3 NextFOVBlendTime { get; set; }
	[NetworkName("m_flFOVBlendStartTime")]
	[NetworkVar] public partial Vector3 FOVBlendStartTime { get; set; }
	[NetworkName("m_bAlternateFOV")]
	[NetworkVar] public partial bool AlternateFOV { get; set; }
	[NetworkName("m_flFadeAlpha")]
	[NetworkVar] public partial float FadeAlpha { get; set; }
	[NetworkName("m_flFadeColor")]
	[NetworkVar] public partial NetworkArray<InlineArray3<float>, float> FadeColor { get; }
	[NetworkName("m_flFadeDuration")]
	[NetworkVar] public partial float FadeDuration { get; set; }
	[NetworkName("m_hCameraEntity")]
	[NetworkVar] public partial NetworkHandle<BaseEntity> CameraEntity { get; }

	public static readonly SendTable DT_ScriptIntro = new(DT_BaseEntity, [
		SendPropVector(NetworkVarFields.CameraView, 0, PropFlags.Coord),
		SendPropVector(NetworkVarFields.CameraViewAngles, 0, PropFlags.Coord),
		SendPropInt(NetworkVarFields.BlendMode, 5),
		SendPropInt(NetworkVarFields.NextBlendMode, 5),
		SendPropVectorXY(NetworkVarFields.NextBlendTime, 0, PropFlags.NoScale),
		SendPropVectorXY(NetworkVarFields.BlendStartTime, 0, PropFlags.NoScale),
		SendPropBool(NetworkVarFields.Active),
		SendPropInt(NetworkVarFields.FOV, 9),
		SendPropInt(NetworkVarFields.NextFOV, 9),
		SendPropInt(NetworkVarFields.StartFOV, 9),
		SendPropVectorXY(NetworkVarFields.NextFOVBlendTime, 0, PropFlags.NoScale),
		SendPropVectorXY(NetworkVarFields.FOVBlendStartTime, 0, PropFlags.NoScale),
		SendPropBool(NetworkVarFields.AlternateFOV),
		SendPropFloat(NetworkVarFields.FadeAlpha, 10),
		SendPropFloat(FIELD.OF_SENDINFO_ARRAY(ScriptIntro.NetworkVarFields.FadeColor), 0, PropFlags.NoScale),
		SendPropArray(ScriptIntro.NetworkVarFields.FadeColor),
		SendPropFloat(NetworkVarFields.FadeDuration, 10, PropFlags.RoundDown, 0.0f, 255.0f),
		SendPropEHandle(NetworkVarFields.CameraEntity),
	]);
	public static new readonly ServerClass ServerClass = new ServerClass(DT_ScriptIntro);
}
