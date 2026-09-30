using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<RocketTrail>;
[NetworkName("RocketTrail")]
public partial class RocketTrail : BaseParticleEntity
{
	public static readonly SendTable DT_RocketTrail = new(DT_BaseParticleEntity, [
		SendPropFloat(NetworkVarFields.SpawnRate, 8, 0, 1, 1024),
		SendPropVector(NetworkVarFields.StartColor, 8, 0, 0, 1),
		SendPropVector(NetworkVarFields.EndColor, 8, 0, 0, 1),
		SendPropFloat(NetworkVarFields.ParticleLifetime, 16, PropFlags.RoundUp, 0.1f, 100),
		SendPropFloat(NetworkVarFields.StopEmitTime, 0, PropFlags.NoScale),
		SendPropFloat(NetworkVarFields.MinSpeed, 0, PropFlags.NoScale),
		SendPropFloat(NetworkVarFields.MaxSpeed, 0, PropFlags.NoScale),
		SendPropFloat(NetworkVarFields.StartSize, 0, PropFlags.NoScale),
		SendPropFloat(NetworkVarFields.EndSize, 0, PropFlags.NoScale),
		SendPropFloat(NetworkVarFields.SpawnRadius, 0, PropFlags.NoScale),
		SendPropBool(NetworkVarFields.Emit),
		SendPropInt(NetworkVarFields.Attachment, 12, 0),
		SendPropFloat(NetworkVarFields.Opacity, 0, PropFlags.NoScale),
		SendPropBool(NetworkVarFields.Damaged),
		SendPropFloat(NetworkVarFields.FlareScale, 0, PropFlags.NoScale),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_RocketTrail);

	[NetworkName("m_SpawnRate")]
	[NetworkVar] public partial float SpawnRate { get; set; }
	[NetworkName("m_StartColor")]
	[NetworkVar] public partial Vector3 StartColor { get; set; }
	[NetworkName("m_EndColor")]
	[NetworkVar] public partial Vector3 EndColor { get; set; }
	[NetworkName("m_ParticleLifetime")]
	[NetworkVar] public partial float ParticleLifetime { get; set; }
	[NetworkName("m_StopEmitTime")]
	[NetworkVar] public partial float StopEmitTime { get; set; }
	[NetworkName("m_MinSpeed")]
	[NetworkVar] public partial float MinSpeed { get; set; }
	[NetworkName("m_MaxSpeed")]
	[NetworkVar] public partial float MaxSpeed { get; set; }
	[NetworkName("m_StartSize")]
	[NetworkVar] public partial float StartSize { get; set; }
	[NetworkName("m_EndSize")]
	[NetworkVar] public partial float EndSize { get; set; }
	[NetworkName("m_SpawnRadius")]
	[NetworkVar] public partial float SpawnRadius { get; set; }
	[NetworkName("m_bEmit")]
	[NetworkVar] public partial bool Emit { get; set; }
	[NetworkName("m_nAttachment")]
	[NetworkVar] public partial int Attachment { get; set; }
	[NetworkName("m_Opacity")]
	[NetworkVar] public partial float Opacity { get; set; }
	[NetworkName("m_bDamaged")]
	[NetworkVar] public partial bool Damaged { get; set; }
	[NetworkName("m_flFlareScale")]
	[NetworkVar] public partial float FlareScale { get; set; }
}
