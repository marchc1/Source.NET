using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_SmokeTrail>;
public class C_SmokeTrail : C_BaseParticleEntity
{
	public static readonly RecvTable DT_SmokeTrail = new(DT_BaseParticleEntity, [
		RecvPropFloat(FIELD.OF(nameof(SpawnRate))),
		RecvPropVector(FIELD.OF(nameof(StartColor))),
		RecvPropVector(FIELD.OF(nameof(EndColor))),
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
		RecvPropInt(FIELD.OF(nameof(Attachment))),
		RecvPropFloat(FIELD.OF(nameof(Opacity))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass("SmokeTrail", DT_SmokeTrail).WithManualClassID(StaticClassIndices.SmokeTrail);

	public float SpawnRate;
	public Vector3 StartColor;
	public Vector3 EndColor;
	public float ParticleLifetime;
	public float StopEmitTime;
	public float MinSpeed;
	public float MaxSpeed;
	public float MinDirectedSpeed;
	public float MaxDirectedSpeed;
	public float StartSize;
	public float EndSize;
	public float SpawnRadius;
	public bool Emit;
	public int Attachment;
	public float Opacity;
}
