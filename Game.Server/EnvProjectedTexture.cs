using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<EnvProjectedTexture>;
[NetworkName("CEnvProjectedTexture")]
public partial class EnvProjectedTexture : BaseEntity
{
	public static readonly SendTable DT_EnvProjectedTexture = new(DT_BaseEntity, [
		SendPropEHandle(EnvProjectedTexture.NetworkVarFields.HTargetEntity),
		SendPropBool(NetworkVarFields.State),
		SendPropFloat(NetworkVarFields.LightFOV, 0, PropFlags.NoScale),
		SendPropBool(NetworkVarFields.EnableShadows),
		SendPropBool(NetworkVarFields.LightOnlyTarget),
		SendPropBool(NetworkVarFields.LightWorld),
		SendPropBool(NetworkVarFields.CameraSpace),
		SendPropVector(NetworkVarFields.LinearFloatLightColor, 0, PropFlags.NoScale),
		SendPropString(FIELD.OF(nameof(SpotlightTextureName))),
		SendPropInt(NetworkVarFields.SpotlightTextureFrame, 14, 0),
		SendPropFloat(NetworkVarFields.NearZ, 16, PropFlags.RoundDown, 0.0f, 500.0f),
		SendPropFloat(NetworkVarFields.FarZ, 18, PropFlags.RoundDown, 0.0f, 56756.0f),
		SendPropBool(NetworkVarFields.ShadowQuality),
		SendPropInt(NetworkVarFields.Style, 8, 0),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_EnvProjectedTexture);

	[NetworkName("m_hTargetEntity")]
	[NetworkVar] public partial NetworkHandle<BaseEntity> HTargetEntity { get; }
	[NetworkName("m_bState")]
	[NetworkVar] public partial bool State { get; set; }
	[NetworkName("m_flLightFOV")]
	[NetworkVar] public partial float LightFOV { get; set; }
	[NetworkName("m_bEnableShadows")]
	[NetworkVar] public partial bool EnableShadows { get; set; }
	[NetworkName("m_bLightOnlyTarget")]
	[NetworkVar] public partial bool LightOnlyTarget { get; set; }
	[NetworkName("m_bLightWorld")]
	[NetworkVar] public partial bool LightWorld { get; set; }
	[NetworkName("m_bCameraSpace")]
	[NetworkVar] public partial bool CameraSpace { get; set; }
	[NetworkName("m_LinearFloatLightColor")]
	[NetworkVar] public partial Vector3 LinearFloatLightColor { get; set; }
	[NetworkName("m_SpotlightTextureName")]
	public InlineArrayMaxPath<char> SpotlightTextureName;
	[NetworkName("m_nSpotlightTextureFrame")]
	[NetworkVar] public partial int SpotlightTextureFrame { get; set; }
	[NetworkName("m_flNearZ")]
	[NetworkVar] public partial float NearZ { get; set; }
	[NetworkName("m_flFarZ")]
	[NetworkVar] public partial float FarZ { get; set; }
	[NetworkName("m_nShadowQuality")]
	[NetworkVar] public partial bool ShadowQuality { get; set; }
	[NetworkName("m_iStyle")]
	[NetworkVar] public partial int Style { get; set; }
}
