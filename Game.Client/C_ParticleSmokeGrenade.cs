using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_ParticleSmokeGrenade>;
public class C_ParticleSmokeGrenade : C_BaseParticleEntity
{
	public static readonly RecvTable DT_ParticleSmokeGrenade = new(DT_BaseParticleEntity, [
		RecvPropFloat(FIELD.OF(nameof(SpawnTime))),
		RecvPropFloat(FIELD.OF(nameof(FadeStartTime))),
		RecvPropFloat(FIELD.OF(nameof(FadeEndTime))),
		RecvPropBool(FIELD.OF(nameof(CurrentStage))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass("ParticleSmokeGrenade", DT_ParticleSmokeGrenade).WithManualClassID(StaticClassIndices.ParticleSmokeGrenade);

	public float SpawnTime;
	public float FadeStartTime;
	public float FadeEndTime;
	public bool CurrentStage;
}
