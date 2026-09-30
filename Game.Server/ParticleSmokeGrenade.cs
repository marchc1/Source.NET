using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<ParticleSmokeGrenade>;
[NetworkName("ParticleSmokeGrenade")]
public partial class ParticleSmokeGrenade : BaseParticleEntity
{
	public static readonly SendTable DT_ParticleSmokeGrenade = new(DT_BaseParticleEntity, [
		SendPropFloat(NetworkVarFields.SpawnTime, 0, PropFlags.NoScale),
		SendPropFloat(NetworkVarFields.FadeStartTime, 0, PropFlags.NoScale),
		SendPropFloat(NetworkVarFields.FadeEndTime, 0, PropFlags.NoScale),
		SendPropBool(NetworkVarFields.CurrentStage),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_ParticleSmokeGrenade);

	[NetworkName("m_flSpawnTime")]
	[NetworkVar] public partial float SpawnTime { get; set; }
	[NetworkName("m_FadeStartTime")]
	[NetworkVar] public partial float FadeStartTime { get; set; }
	[NetworkName("m_FadeEndTime")]
	[NetworkVar] public partial float FadeEndTime { get; set; }
	[NetworkName("m_CurrentStage")]
	[NetworkVar] public partial bool CurrentStage { get; set; }
}
