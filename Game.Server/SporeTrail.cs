using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<SporeTrail>;
[LinkEntityToClass("env_sporetrail")]
[NetworkName("SporeTrail")]
public partial class SporeTrail : BaseParticleEntity
{
	public static readonly SendTable DT_SporeTrail = new(DT_BaseParticleEntity, [
		SendPropFloat(NetworkVarFields.SpawnRate, 8, 0, 1, 1024),
		SendPropVector(NetworkVarFields.EndColor, 8, 0, 0, 1),
		SendPropFloat(NetworkVarFields.ParticleLifetime, 16, PropFlags.RoundUp, 0.1f, 100),
		SendPropFloat(NetworkVarFields.StartSize, -1, PropFlags.NoScale),
		SendPropFloat(NetworkVarFields.EndSize, -1, PropFlags.NoScale),
		SendPropFloat(NetworkVarFields.SpawnRadius, -1, PropFlags.NoScale),
		SendPropBool(NetworkVarFields.Emit),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_SporeTrail);

	[NetworkName("m_flSpawnRate")]
	[NetworkVar] public partial float SpawnRate { get; set; }
	[NetworkName("m_vecEndColor")]
	[NetworkVar] public partial Vector3 EndColor { get; set; }
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
}
