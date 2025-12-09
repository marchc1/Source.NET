using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<SmokeTrail>;
public class SmokeTrail : BaseParticleEntity
{
	public static readonly SendTable DT_SmokeTrail = new(DT_BaseParticleEntity, [
		SendPropFloat(FIELD.OF(nameof(SpawnRate)), 8, 0),
		SendPropVector(FIELD.OF(nameof(StartColor)), 8, 0),
		SendPropVector(FIELD.OF(nameof(EndColor)), 8, 0),
		SendPropFloat(FIELD.OF(nameof(ParticleLifetime)), 16, PropFlags.RoundUp),
		SendPropFloat(FIELD.OF(nameof(StopEmitTime)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(MinSpeed)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(MaxSpeed)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(MinDirectedSpeed)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(MaxDirectedSpeed)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(StartSize)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(EndSize)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(SpawnRadius)), 0, PropFlags.NoScale),
		SendPropBool(FIELD.OF(nameof(Emit))),
		SendPropInt(FIELD.OF(nameof(Attachment)), 32, 0),
		SendPropFloat(FIELD.OF(nameof(Opacity)), 0, PropFlags.NoScale),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass("SmokeTrail", DT_SmokeTrail).WithManualClassID(StaticClassIndices.SmokeTrail);

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
