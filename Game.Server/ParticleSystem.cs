using Source.Common;
using Source;

using Game.Shared;
using System.Runtime.CompilerServices;

namespace Game.Server;


using FIELD = FIELD<ParticleSystem>;

public class ParticleSystem : BaseEntity
{
	public static readonly SendTable DT_ParticleSystem = new([
		SendPropVector(FIELD.OF(nameof(Origin)), 0, PropFlags.Coord | PropFlags.ChangesOften),
		SendPropEHandle(FIELD.OF(nameof(OwnerEntity))),
		SendPropEHandle(FIELD.OF(nameof(MoveParent))),
		SendPropInt(FIELD.OF(nameof(ParentAttachment)), 8, PropFlags.Unsigned),
		SendPropVector(FIELD.OF(nameof(Rotation)), 13, PropFlags.RoundDown | PropFlags.ChangesOften),
		SendPropInt(FIELD.OF(nameof(EffectIndex)), 12, PropFlags.Unsigned),
		SendPropBool(FIELD.OF(nameof(Active))),
		SendPropFloat(FIELD.OF(nameof(StartTime)), 0, PropFlags.NoScale),
		SendPropArray3(FIELD.OF_ARRAY(nameof(ControlPointEnts)), SendPropEHandle(FIELD.OF_ARRAYINDEX(nameof(ControlPointEnts), 0))),
		SendPropArray3(FIELD.OF_ARRAY(nameof(ControlPointParents)), SendPropInt(FIELD.OF_ARRAYINDEX(nameof(ControlPointParents), 0), 3, PropFlags.Unsigned)),
		SendPropBool(FIELD.OF(nameof(WeatherEffect))),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass("ParticleSystem", DT_ParticleSystem).WithManualClassID(StaticClassIndices.CParticleSystem);
	public int EffectIndex;
	public bool Active;
	public TimeUnit_t StartTime;
	public bool WeatherEffect;

	public InlineArrayNewMaxControlPoints<EHANDLE> ControlPointEnts = new();
	public InlineArrayNewMaxControlPoints<byte> ControlPointParents = new();
}
