using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_SporeExplosion>;
public class C_SporeExplosion : C_BaseParticleEntity
{
	public static readonly RecvTable DT_SporeExplosion = new(DT_BaseParticleEntity, [
		RecvPropFloat(FIELD.OF(nameof(SpawnRate))),
		RecvPropFloat(FIELD.OF(nameof(ParticleLifetime))),
		RecvPropFloat(FIELD.OF(nameof(StartSize))),
		RecvPropFloat(FIELD.OF(nameof(EndSize))),
		RecvPropFloat(FIELD.OF(nameof(SpawnRadius))),
		RecvPropBool(FIELD.OF(nameof(Emit))),
		RecvPropBool(FIELD.OF(nameof(DontRemove))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass("SporeExplosion", DT_SporeExplosion).WithManualClassID(StaticClassIndices.SporeExplosion);

	public float SpawnRate;
	public float ParticleLifetime;
	public float StartSize;
	public float EndSize;
	public float SpawnRadius;
	public bool Emit;
	public bool DontRemove;
}
