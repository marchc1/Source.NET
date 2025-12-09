using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<SporeExplosion>;
public class SporeExplosion : BaseParticleEntity
{
	public static readonly SendTable DT_SporeExplosion = new(DT_BaseParticleEntity, [
		SendPropFloat(FIELD.OF(nameof(SpawnRate)), 8, 0),
		SendPropFloat(FIELD.OF(nameof(ParticleLifetime)), 16, PropFlags.RoundUp),
		SendPropFloat(FIELD.OF(nameof(StartSize)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(EndSize)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(SpawnRadius)), 0, PropFlags.NoScale),
		SendPropBool(FIELD.OF(nameof(Emit))),
		SendPropBool(FIELD.OF(nameof(DontRemove))),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass("SporeExplosion", DT_SporeExplosion).WithManualClassID(StaticClassIndices.SporeExplosion);

	public float SpawnRate;
	public float ParticleLifetime;
	public float StartSize;
	public float EndSize;
	public float SpawnRadius;
	public bool Emit;
	public bool DontRemove;
}
