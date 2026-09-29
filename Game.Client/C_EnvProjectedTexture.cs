using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_EnvProjectedTexture>;
[NetworkName("CEnvProjectedTexture")]
public class C_EnvProjectedTexture : C_BaseEntity
{
	public static readonly RecvTable DT_EnvProjectedTexture = new(DT_BaseEntity, [
		RecvPropEHandle(FIELD.OF(nameof(HTargetEntity))),
		RecvPropBool(FIELD.OF(nameof(State))),
		RecvPropFloat(FIELD.OF(nameof(LightFOV))),
		RecvPropBool(FIELD.OF(nameof(EnableShadows))),
		RecvPropBool(FIELD.OF(nameof(LightOnlyTarget))),
		RecvPropBool(FIELD.OF(nameof(LightWorld))),
		RecvPropBool(FIELD.OF(nameof(CameraSpace))),
		RecvPropVector(FIELD.OF(nameof(LinearFloatLightColor))),
		RecvPropString(FIELD.OF(nameof(SpotlightTextureName))),
		RecvPropInt(FIELD.OF(nameof(SpotlightTextureFrame))),
		RecvPropFloat(FIELD.OF(nameof(NearZ))),
		RecvPropFloat(FIELD.OF(nameof(FarZ))),
		RecvPropBool(FIELD.OF(nameof(ShadowQuality))),
		RecvPropInt(FIELD.OF(nameof(Style))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass(DT_EnvProjectedTexture);

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
