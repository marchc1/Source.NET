using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_DustTrail>;
[NetworkName("DustTrail")]
public class C_DustTrail : C_BaseParticleEntity
{
	public static readonly RecvTable DT_DustTrail = new(DT_BaseParticleEntity, [
		RecvPropFloat(FIELD.OF(nameof(SpawnRate))),
		RecvPropVector(FIELD.OF(nameof(Color))),
		RecvPropFloat(FIELD.OF(nameof(ParticleLifetime))),
		RecvPropFloat(FIELD.OF(nameof(StopEmitTime))),
		RecvPropFloat(FIELD.OF(nameof(MinSpeed))),
		RecvPropFloat(FIELD.OF(nameof(MaxSpeed))),
		RecvPropFloat(FIELD.OF(nameof(MinDirectedSpeed))),
		RecvPropFloat(FIELD.OF(nameof(MaxDirectedSpeed))),
		RecvPropFloat(FIELD.OF(nameof(StartSize))),
		RecvPropFloat(FIELD.OF(nameof(EndSize))),
		RecvPropFloat(FIELD.OF(nameof(SpawnRadius))),
		RecvPropBool(FIELD.OF(nameof(Emit))),
		RecvPropFloat(FIELD.OF(nameof(Opacity))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass(DT_DustTrail);

	[NetworkName("m_SpawnRate")]
	public float SpawnRate;
	[NetworkName("m_Color")]
	public Vector3 Color;
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
	[NetworkName("m_Opacity")]
	public float Opacity;
}
