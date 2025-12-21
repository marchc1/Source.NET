using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_SmokeStack>;
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
	public static readonly new ClientClass ClientClass = new ClientClass("SmokeStack", DT_SmokeStack).WithManualClassID(StaticClassIndices.CSmokeStack);

	public float SpreadSpeed;
	public new float Speed;
	public float StartSize;
	public float EndSize;
	public float Rate;
	public float JetLength;
	public bool Emit;
	public float BaseSpread;
	public float RollSpeed;
	public ParticleLightInfo AmbientLight;
	public ParticleLightInfo DirLight;
	public Vector3 Wind;
	public float Twist;
	public int MaterialModel;
}
