using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<CitadelEnergyCore>;
[NetworkName("CCitadelEnergyCore")]
public partial class CitadelEnergyCore : BaseEntity
{
	public static readonly SendTable DT_CitadelEnergyCore = new(DT_BaseEntity, [
		SendPropFloat(NetworkVarFields.Scale, 0, PropFlags.NoScale),
		SendPropInt(NetworkVarFields.State, 8, PropFlags.Unsigned),
		SendPropFloat(NetworkVarFields.Duration, 0, PropFlags.NoScale),
		SendPropFloat(NetworkVarFields.StartTime, 0, PropFlags.NoScale),
		SendPropInt(NetworkVarFields.Spawnflags, 32, PropFlags.Unsigned),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_CitadelEnergyCore);

	[NetworkName("m_flScale")]
	[NetworkVar] public partial float Scale { get; set; }
	[NetworkName("m_nState")]
	[NetworkVar] public partial int State { get; set; }
	[NetworkName("m_flDuration")]
	[NetworkVar] public partial float Duration { get; set; }
	[NetworkName("m_flStartTime")]
	[NetworkVar] public partial float StartTime { get; set; }
	[NetworkName("m_spawnflags")]
	[NetworkVar] public partial int Spawnflags { get; set; }
}
