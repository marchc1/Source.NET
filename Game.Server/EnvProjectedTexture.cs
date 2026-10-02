using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<EnvProjectedTexture>;
[LinkEntityToClass("env_projectedtexture")]
[NetworkName("CEnvProjectedTexture")]
public class EnvProjectedTexture : BaseEntity
{
	public static readonly SendTable DT_EnvProjectedTexture = new(DT_BaseEntity, [
		SendPropEHandle(FIELD.OF(nameof(HTargetEntity))),
		SendPropBool(FIELD.OF(nameof(State))),
		SendPropFloat(FIELD.OF(nameof(LightFOV)), 0, PropFlags.NoScale),
		SendPropBool(FIELD.OF(nameof(EnableShadows))),
		SendPropBool(FIELD.OF(nameof(LightOnlyTarget))),
		SendPropBool(FIELD.OF(nameof(LightWorld))),
		SendPropBool(FIELD.OF(nameof(CameraSpace))),
		SendPropVector(FIELD.OF(nameof(LinearFloatLightColor)), 0, PropFlags.NoScale),
		SendPropString(FIELD.OF(nameof(SpotlightTextureName))),
		SendPropInt(FIELD.OF(nameof(SpotlightTextureFrame)), 14, 0),
		SendPropFloat(FIELD.OF(nameof(NearZ)), 16, PropFlags.RoundDown, 0.0f, 500.0f),
		SendPropFloat(FIELD.OF(nameof(FarZ)), 18, PropFlags.RoundDown, 0.0f, 56756.0f),
		SendPropBool(FIELD.OF(nameof(ShadowQuality))),
		SendPropInt(FIELD.OF(nameof(Style)), 8, 0),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_EnvProjectedTexture);

	[NetworkName("m_hTargetEntity")]
	public EHANDLE HTargetEntity = new();
	[NetworkName("m_bState")]
	public bool State;
	[NetworkName("m_flLightFOV")]
	public float LightFOV;
	[NetworkName("m_bEnableShadows")]
	public bool EnableShadows;
	[NetworkName("m_bLightOnlyTarget")]
	public bool LightOnlyTarget;
	[NetworkName("m_bLightWorld")]
	public bool LightWorld;
	[NetworkName("m_bCameraSpace")]
	public bool CameraSpace;
	[NetworkName("m_LinearFloatLightColor")]
	public Vector3 LinearFloatLightColor;
	[NetworkName("m_SpotlightTextureName")]
	public InlineArrayMaxPath<char> SpotlightTextureName;
	[NetworkName("m_nSpotlightTextureFrame")]
	public int SpotlightTextureFrame;
	[NetworkName("m_flNearZ")]
	public float NearZ;
	[NetworkName("m_flFarZ")]
	public float FarZ;
	[NetworkName("m_nShadowQuality")]
	public bool ShadowQuality;
	[NetworkName("m_iStyle")]
	public int Style;
}
