using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<RocketTrail>;
public class RocketTrail : BaseParticleEntity
{
	public static readonly SendTable DT_RocketTrail = new(DT_BaseParticleEntity, [
		SendPropFloat(FIELD.OF(nameof(SpawnRate)), 8, 0),
		SendPropVector(FIELD.OF(nameof(StartColor)), 8, 0),
		SendPropVector(FIELD.OF(nameof(EndColor)), 8, 0),
		SendPropFloat(FIELD.OF(nameof(ParticleLifetime)), 16, PropFlags.RoundUp),
		SendPropFloat(FIELD.OF(nameof(StopEmitTime)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(MinSpeed)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(MaxSpeed)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(StartSize)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(EndSize)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(SpawnRadius)), 0, PropFlags.NoScale),
		SendPropBool(FIELD.OF(nameof(Emit))),
		SendPropInt(FIELD.OF(nameof(Attachment)), 32, 0),
		SendPropFloat(FIELD.OF(nameof(Opacity)), 0, PropFlags.NoScale),
		SendPropBool(FIELD.OF(nameof(Damaged))),
		SendPropFloat(FIELD.OF(nameof(FlareScale)), 0, PropFlags.NoScale),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass("RocketTrail", DT_RocketTrail).WithManualClassID(StaticClassIndices.RocketTrail);

	public float SpawnRate;
	public Vector3 StartColor;
	public Vector3 EndColor;
	public float ParticleLifetime;
	public float StopEmitTime;
	public float MinSpeed;
	public float MaxSpeed;
	public float StartSize;
	public float EndSize;
	public float SpawnRadius;
	public bool Emit;
	public int Attachment;
	public float Opacity;
	public bool Damaged;
	public float FlareScale;
}
