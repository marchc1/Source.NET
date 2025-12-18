using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<EnvProjectedTexture>;
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
		SendPropInt(FIELD.OF(nameof(SpotlightTextureFrame)), 32, 0),
		SendPropFloat(FIELD.OF(nameof(NearZ)), 16, PropFlags.RoundDown),
		SendPropFloat(FIELD.OF(nameof(FarZ)), 18, PropFlags.RoundDown),
		SendPropBool(FIELD.OF(nameof(ShadowQuality))),
		SendPropInt(FIELD.OF(nameof(Style)), 32, 0),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass("EnvProjectedTexture", DT_EnvProjectedTexture).WithManualClassID(StaticClassIndices.CEnvProjectedTexture);

	public readonly EHANDLE HTargetEntity = new();
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
