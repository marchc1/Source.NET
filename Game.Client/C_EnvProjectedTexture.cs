using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_EnvProjectedTexture>;
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
	public static readonly new ClientClass ClientClass = new ClientClass("EnvProjectedTexture", DT_EnvProjectedTexture).WithManualClassID(StaticClassIndices.CEnvProjectedTexture);

	public EHANDLE HTargetEntity = new();
	public bool State;
	public float LightFOV;
	public bool EnableShadows;
	public bool LightOnlyTarget;
	public bool LightWorld;
	public bool CameraSpace;
	public Vector3 LinearFloatLightColor;
	public InlineArrayMaxPath<char> SpotlightTextureName;
	public int SpotlightTextureFrame;
	public float NearZ;
	public float FarZ;
	public bool ShadowQuality;
	public int Style;
}
