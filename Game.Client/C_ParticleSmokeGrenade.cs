using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_ParticleSmokeGrenade>;
[NetworkName("ParticleSmokeGrenade")]
public class C_ParticleSmokeGrenade : C_BaseParticleEntity
{
	public static readonly RecvTable DT_ParticleSmokeGrenade = new(DT_BaseParticleEntity, [
		RecvPropFloat(FIELD.OF(nameof(SpawnTime))),
		RecvPropFloat(FIELD.OF(nameof(FadeStartTime))),
		RecvPropFloat(FIELD.OF(nameof(FadeEndTime))),
		RecvPropBool(FIELD.OF(nameof(CurrentStage))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass(DT_ParticleSmokeGrenade);

	[NetworkName("m_flSpawnTime")]
	public float SpawnTime;
	[NetworkName("m_FadeStartTime")]
	public float FadeStartTime;
	[NetworkName("m_FadeEndTime")]
	public float FadeEndTime;
	[NetworkName("m_CurrentStage")]
	public bool CurrentStage;
}
