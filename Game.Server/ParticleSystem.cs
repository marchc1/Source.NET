using Source.Common;
using Source;

using Game.Shared;
using System.Runtime.CompilerServices;

namespace Game.Server;


using FIELD = FIELD<ParticleSystem>;

[LinkEntityToClass("info_particle_system")]
[NetworkName("CParticleSystem")]
public class ParticleSystem : BaseEntity
{
	public static readonly SendTable DT_ParticleSystem = new([
		SendPropVector(NetworkVarFields.Origin, 0, PropFlags.Coord | PropFlags.ChangesOften),
		SendPropEHandle(FIELD.OF(nameof(OwnerEntity))),
		SendPropEHandle(FIELD.OF(nameof(MoveParent))),
		SendPropInt(FIELD.OF(nameof(ParentAttachment)), 8, PropFlags.Unsigned),
		SendPropQAngles(FIELD.OF(nameof(Rotation)), 13, PropFlags.RoundDown | PropFlags.ChangesOften),
		SendPropInt(FIELD.OF(nameof(EffectIndex)), 12, PropFlags.Unsigned),
		SendPropBool(FIELD.OF(nameof(Active))),
		SendPropFloat(FIELD.OF(nameof(StartTime)), 0, PropFlags.NoScale),
		SendPropArray3(FIELD.OF_ARRAY(nameof(ControlPointEnts)), SendPropEHandle(FIELD.OF_ARRAYINDEX(nameof(ControlPointEnts), 0))),
		SendPropArray3(FIELD.OF_ARRAY(nameof(ControlPointParents)), SendPropInt(FIELD.OF_ARRAYINDEX(nameof(ControlPointParents), 0), 3, PropFlags.Unsigned)),
		SendPropBool(FIELD.OF(nameof(WeatherEffect))),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_ParticleSystem);
	[NetworkName("m_iEffectIndex")]
	public int EffectIndex;
	[NetworkName("m_bActive")]
	public bool Active;
	[NetworkName("m_flStartTime")]
	public TimeUnit_t StartTime;
	[NetworkName("m_bWeatherEffect")]
	public bool WeatherEffect;

	[NetworkName("m_hControlPointEnts")]
	public InlineArrayNewMaxControlPoints<EHANDLE> ControlPointEnts = new();
	[NetworkName("m_iControlPointParents")]
	public InlineArrayNewMaxControlPoints<byte> ControlPointParents = new();
}
