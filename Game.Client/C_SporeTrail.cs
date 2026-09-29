using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_SporeTrail>;
[NetworkName("SporeTrail")]
public class C_SporeTrail : C_BaseParticleEntity
{
	public static readonly RecvTable DT_SporeTrail = new(DT_BaseParticleEntity, [
		RecvPropFloat(FIELD.OF(nameof(SpawnRate))),
		RecvPropVector(FIELD.OF(nameof(EndColor))),
		RecvPropFloat(FIELD.OF(nameof(ParticleLifetime))),
		RecvPropFloat(FIELD.OF(nameof(StartSize))),
		RecvPropFloat(FIELD.OF(nameof(EndSize))),
		RecvPropFloat(FIELD.OF(nameof(SpawnRadius))),
		RecvPropInt(FIELD.OF(nameof(Emit))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass(DT_SporeTrail);

	[NetworkName("m_flSpawnRate")]
	public float SpawnRate;
	[NetworkName("m_vecEndColor")]
	public Vector3 EndColor;
	[NetworkName("m_flParticleLifetime")]
	public float ParticleLifetime;
	[NetworkName("m_flStartSize")]
	public float StartSize;
	[NetworkName("m_flEndSize")]
	public float EndSize;
	[NetworkName("m_flSpawnRadius")]
	public float SpawnRadius;
	[NetworkName("m_bEmit")]
	public bool Emit;
}
