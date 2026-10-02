using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<SmokeStack>;

public struct SmokeStackLightInfo{
	[NetworkName("m_vPos")]
	public Vector3 Pos;
	[NetworkName("m_vColor")]
	public Vector3 Color;
	[NetworkName("m_flIntensity")]
	public float Intensity;
}

[LinkEntityToClass("env_smokestack")]
[NetworkName("CSmokeStack")]
public class SmokeStack : BaseParticleEntity
{
	public static readonly SendTable DT_SmokeStack = new(DT_BaseParticleEntity, [
		SendPropFloat(FIELD.OF(nameof(SpreadSpeed)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(Speed)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(StartSize)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(EndSize)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(Rate)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(JetLength)), 0, PropFlags.NoScale),
		SendPropBool(FIELD.OF(nameof(Emit))),
		SendPropFloat(FIELD.OF(nameof(BaseSpread)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(RollSpeed)), 0, PropFlags.NoScale),
		SendPropVector(FIELD.OF("DirLight.Pos"), 0, PropFlags.NoScale),
		SendPropVector(FIELD.OF("DirLight.Color"), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF("DirLight.Intensity"), 0, PropFlags.NoScale),
		SendPropVector(FIELD.OF("AmbientLight.Pos"), 0, PropFlags.NoScale),
		SendPropVector(FIELD.OF("AmbientLight.Color"), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF("AmbientLight.Intensity"), 0, PropFlags.NoScale),
		SendPropVector(FIELD.OF(nameof(Wind)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(Twist)), 0, PropFlags.NoScale),
		SendPropIntWithMinusOneFlag(FIELD.OF(nameof(MaterialModel)), 16),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_SmokeStack);

	[NetworkName("m_SpreadSpeed")]
	public float SpreadSpeed;
	[NetworkName("m_Speed")]
	public new float Speed;
	[NetworkName("m_StartSize")]
	public float StartSize;
	[NetworkName("m_EndSize")]
	public float EndSize;
	[NetworkName("m_Rate")]
	public float Rate;
	[NetworkName("m_JetLength")]
	public float JetLength;
	[NetworkName("m_bEmit")]
	public bool Emit;
	[NetworkName("m_flBaseSpread")]
	public float BaseSpread;
	[NetworkName("m_flRollSpeed")]
	public float RollSpeed;

	[NetworkName("m_AmbientLight")]
	public SmokeStackLightInfo AmbientLight;
	[NetworkName("m_DirLight")]
	public SmokeStackLightInfo DirLight;

	[NetworkName("m_vWind")]
	public Vector3 Wind;
	[NetworkName("m_flTwist")]
	public float Twist;
	[NetworkName("m_iMaterialModel")]
	public int MaterialModel;
}
