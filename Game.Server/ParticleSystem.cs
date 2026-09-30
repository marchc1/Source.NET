using Source.Common;
using Source;

using Game.Shared;
using System.Runtime.CompilerServices;

namespace Game.Server;


using FIELD = FIELD<ParticleSystem>;

[NetworkName("CParticleSystem")]
public partial class ParticleSystem : BaseEntity
{
	public static readonly SendTable DT_ParticleSystem = new([
		SendPropVector(BaseEntity.NetworkVarFields.Origin, 0, PropFlags.Coord | PropFlags.ChangesOften),
		SendPropEHandle(FIELD.OF(nameof(OwnerEntity))),
		SendPropEHandle(FIELD.OF(nameof(MoveParent))),
		SendPropInt(BaseEntity.NetworkVarFields.ParentAttachment, 8, PropFlags.Unsigned),
		SendPropQAngles(BaseEntity.NetworkVarFields.Rotation, 13, PropFlags.RoundDown | PropFlags.ChangesOften),
		SendPropInt(NetworkVarFields.EffectIndex, 12, PropFlags.Unsigned),
		SendPropBool(NetworkVarFields.Active),
		SendPropFloat(NetworkVarFields.StartTime, 0, PropFlags.NoScale),
		SendPropArray3(ParticleSystem.NetworkVarFields.ControlPointEnts, SendPropEHandle(ParticleSystem.NetworkVarFields.ControlPointEnts.AtIndex(0)!)),
		SendPropArray3(ParticleSystem.NetworkVarFields.ControlPointParents, SendPropInt(ParticleSystem.NetworkVarFields.ControlPointParents.AtIndex(0)!, 3, PropFlags.Unsigned)),
		SendPropBool(NetworkVarFields.WeatherEffect),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_ParticleSystem);
	[NetworkName("m_iEffectIndex")]
	[NetworkVar] public partial int EffectIndex { get; set; }
	[NetworkName("m_bActive")]
	[NetworkVar] public partial bool Active { get; set; }
	[NetworkName("m_flStartTime")]
	[NetworkVar] public partial TimeUnit_t StartTime { get; set; }
	[NetworkName("m_bWeatherEffect")]
	[NetworkVar] public partial bool WeatherEffect { get; set; }

	[NetworkName("m_hControlPointEnts")]
	[NetworkVar] public partial NetworkArray<InlineArrayNewMaxControlPoints<EHANDLE>, EHANDLE> ControlPointEnts { get; }
	[NetworkName("m_iControlPointParents")]
	[NetworkVar] public partial NetworkArray<InlineArrayNewMaxControlPoints<byte>, byte> ControlPointParents { get; }
}
