using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<ParticleSmokeGrenade>;
public class ParticleSmokeGrenade : BaseParticleEntity
{
	public static readonly SendTable DT_ParticleSmokeGrenade = new(DT_BaseParticleEntity, [
		SendPropFloat(FIELD.OF(nameof(SpawnTime)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(FadeStartTime)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(FadeEndTime)), 0, PropFlags.NoScale),
		SendPropBool(FIELD.OF(nameof(CurrentStage))),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass("ParticleSmokeGrenade", DT_ParticleSmokeGrenade).WithManualClassID(StaticClassIndices.ParticleSmokeGrenade);

	public float SpawnTime;
	public float FadeStartTime;
	public float FadeEndTime;
	public bool CurrentStage;
}
