using Game.Shared;

using Source;
using Source.Common;

namespace Game.Client;
using FIELD = FIELD<C_ParticleSystem>;

[NetworkName("CParticleSystem")]
public class C_ParticleSystem : C_BaseEntity
{
	public static readonly RecvTable DT_ParticleSystem = new([
		RecvPropVector(FIELD.OF(nameof(Origin))),
		RecvPropEHandle(FIELD.OF(nameof(OwnerEntity))),
		RecvPropEHandle(FIELD.OF(nameof(MoveParent))),
		RecvPropInt(FIELD.OF(nameof(ParentAttachment))),
		RecvPropVector(FIELD.OF(nameof(Rotation))),
		RecvPropInt(FIELD.OF(nameof(EffectIndex))),
		RecvPropBool(FIELD.OF(nameof(Active))),
		RecvPropFloat(FIELD.OF(nameof(StartTime))),
		RecvPropArray3(FIELD.OF_ARRAY(nameof(ControlPointEnts)), RecvPropEHandle(FIELD.OF_ARRAYINDEX(nameof(ControlPointEnts), 0))),
		RecvPropArray3(FIELD.OF_ARRAY(nameof(ControlPointParents)), RecvPropInt(FIELD.OF_ARRAYINDEX(nameof(ControlPointParents), 0))),
		RecvPropBool(FIELD.OF(nameof(WeatherEffect))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass(DT_ParticleSystem);
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

