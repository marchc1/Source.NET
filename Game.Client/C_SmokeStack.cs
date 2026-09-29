using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_SmokeStack>;
[NetworkName("CSmokeStack")]
public class C_SmokeStack : C_BaseParticleEntity
{
	public static readonly RecvTable DT_SmokeStack = new(DT_BaseParticleEntity, [
		RecvPropFloat(FIELD.OF(nameof(SpreadSpeed))),
		RecvPropFloat(FIELD.OF(nameof(Speed))),
		RecvPropFloat(FIELD.OF(nameof(StartSize))),
		RecvPropFloat(FIELD.OF(nameof(EndSize))),
		RecvPropFloat(FIELD.OF(nameof(Rate))),
		RecvPropFloat(FIELD.OF(nameof(JetLength))),
		RecvPropBool(FIELD.OF(nameof(Emit))),
		RecvPropFloat(FIELD.OF(nameof(BaseSpread))),
		RecvPropFloat(FIELD.OF(nameof(RollSpeed))),
		RecvPropVector(FIELD.OF("DirLight.Pos")),
		RecvPropVector(FIELD.OF("DirLight.Color")),
		RecvPropFloat(FIELD.OF("DirLight.Intensity")),
		RecvPropVector(FIELD.OF("AmbientLight.Pos")),
		RecvPropVector(FIELD.OF("AmbientLight.Color")),
		RecvPropFloat(FIELD.OF("AmbientLight.Intensity")),
		RecvPropVector(FIELD.OF(nameof(Wind))),
		RecvPropFloat(FIELD.OF(nameof(Twist))),
		RecvPropIntWithMinusOneFlag(FIELD.OF(nameof(MaterialModel))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass(DT_SmokeStack);

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
	public ParticleLightInfo AmbientLight;
	[NetworkName("m_DirLight")]
	public ParticleLightInfo DirLight;
	[NetworkName("m_vWind")]
	public Vector3 Wind;
	[NetworkName("m_flTwist")]
	public float Twist;
	[NetworkName("m_iMaterialModel")]
	public int MaterialModel;
}
