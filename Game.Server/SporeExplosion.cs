using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<SporeExplosion>;
[NetworkName("SporeExplosion")]
public partial class SporeExplosion : BaseParticleEntity
{
	public static readonly SendTable DT_SporeExplosion = new(DT_BaseParticleEntity, [
		SendPropFloat(NetworkVarFields.SpawnRate, 8, 0, 1, 1024),
		SendPropFloat(NetworkVarFields.ParticleLifetime, 16, PropFlags.RoundUp, 0.1f, 100),
		SendPropFloat(NetworkVarFields.StartSize, 0, PropFlags.NoScale),
		SendPropFloat(NetworkVarFields.EndSize, 0, PropFlags.NoScale),
		SendPropFloat(NetworkVarFields.SpawnRadius, 0, PropFlags.NoScale),
		SendPropBool(NetworkVarFields.Emit),
		SendPropBool(NetworkVarFields.DontRemove),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_SporeExplosion);

	[NetworkName("m_flSpawnRate")]
	[NetworkVar] public partial float SpawnRate { get; set; }
	[NetworkName("m_flParticleLifetime")]
	[NetworkVar] public partial float ParticleLifetime { get; set; }
	[NetworkName("m_flStartSize")]
	[NetworkVar] public partial float StartSize { get; set; }
	[NetworkName("m_flEndSize")]
	[NetworkVar] public partial float EndSize { get; set; }
	[NetworkName("m_flSpawnRadius")]
	[NetworkVar] public partial float SpawnRadius { get; set; }
	[NetworkName("m_bEmit")]
	[NetworkVar] public partial bool Emit { get; set; }
	[NetworkName("m_bDontRemove")]
	[NetworkVar] public partial bool DontRemove { get; set; }
}
