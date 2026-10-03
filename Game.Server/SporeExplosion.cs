using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<SporeExplosion>;
[LinkEntityToClass("env_sporeexplosion")]
[NetworkName("SporeExplosion")]
public class SporeExplosion : BaseParticleEntity
{
	public static readonly SendTable DT_SporeExplosion = new(DT_BaseParticleEntity, [
		SendPropFloat(FIELD.OF(nameof(SpawnRate)), 8, 0, 1, 1024),
		SendPropFloat(FIELD.OF(nameof(ParticleLifetime)), 16, PropFlags.RoundUp, 0.1f, 100),
		SendPropFloat(FIELD.OF(nameof(StartSize)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(EndSize)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(SpawnRadius)), 0, PropFlags.NoScale),
		SendPropBool(FIELD.OF(nameof(Emit))),
		SendPropBool(FIELD.OF(nameof(DontRemove))),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_SporeExplosion);

	[NetworkName("m_flSpawnRate")]
	public float SpawnRate;
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
	[NetworkName("m_bDontRemove")]
	public bool DontRemove;
}
