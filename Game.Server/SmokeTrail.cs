using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<SmokeTrail>;
[LinkEntityToClass("env_smoketrail")]
[NetworkName("SmokeTrail")]
public class SmokeTrail : BaseParticleEntity
{
	public static readonly SendTable DT_SmokeTrail = new(DT_BaseParticleEntity, [
		SendPropFloat(FIELD.OF(nameof(SpawnRate)), 8, 0, 1, 1024),
		SendPropVector(FIELD.OF(nameof(StartColor)), 8, 0, 0, 1),
		SendPropVector(FIELD.OF(nameof(EndColor)), 8, 0, 0, 1),
		SendPropFloat(FIELD.OF(nameof(ParticleLifetime)), 16, PropFlags.RoundUp, 0.1f, 100),
		SendPropFloat(FIELD.OF(nameof(StopEmitTime)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(MinSpeed)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(MaxSpeed)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(MinDirectedSpeed)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(MaxDirectedSpeed)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(StartSize)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(EndSize)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(SpawnRadius)), 0, PropFlags.NoScale),
		SendPropBool(FIELD.OF(nameof(Emit))),
		SendPropInt(FIELD.OF(nameof(Attachment)), 12, 0),
		SendPropFloat(FIELD.OF(nameof(Opacity)), 0, PropFlags.NoScale),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_SmokeTrail);

	[NetworkName("m_SpawnRate")]
	public float SpawnRate;
	[NetworkName("m_StartColor")]
	public Vector3 StartColor;
	[NetworkName("m_EndColor")]
	public Vector3 EndColor;
	[NetworkName("m_ParticleLifetime")]
	public float ParticleLifetime;
	[NetworkName("m_StopEmitTime")]
	public float StopEmitTime;
	[NetworkName("m_MinSpeed")]
	public float MinSpeed;
	[NetworkName("m_MaxSpeed")]
	public float MaxSpeed;
	[NetworkName("m_MinDirectedSpeed")]
	public float MinDirectedSpeed;
	[NetworkName("m_MaxDirectedSpeed")]
	public float MaxDirectedSpeed;
	[NetworkName("m_StartSize")]
	public float StartSize;
	[NetworkName("m_EndSize")]
	public float EndSize;
	[NetworkName("m_SpawnRadius")]
	public float SpawnRadius;
	[NetworkName("m_bEmit")]
	public bool Emit;
	[NetworkName("m_nAttachment")]
	public int Attachment;
	[NetworkName("m_Opacity")]
	public float Opacity;
}
