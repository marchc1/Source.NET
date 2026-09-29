using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<SporeTrail>;
[LinkEntityToClass("env_sporetrail")]
[NetworkName("SporeTrail")]
public class SporeTrail : BaseParticleEntity
{
	public static readonly SendTable DT_SporeTrail = new(DT_BaseParticleEntity, [
		SendPropFloat(FIELD.OF(nameof(SpawnRate)), 8, 0, 1, 1024),
		SendPropVector(FIELD.OF(nameof(EndColor)), 8, 0, 0, 1),
		SendPropFloat(FIELD.OF(nameof(ParticleLifetime)), 16, PropFlags.RoundUp, 0.1f, 100),
		SendPropFloat(FIELD.OF(nameof(StartSize)), -1, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(EndSize)), -1, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(SpawnRadius)), -1, PropFlags.NoScale),
		SendPropBool(FIELD.OF(nameof(Emit))),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_SporeTrail);

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
