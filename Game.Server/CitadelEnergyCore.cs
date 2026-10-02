using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<CitadelEnergyCore>;
[LinkEntityToClass("env_citadel_energy_core")]
[NetworkName("CCitadelEnergyCore")]
public class CitadelEnergyCore : BaseEntity
{
	public static readonly SendTable DT_CitadelEnergyCore = new(DT_BaseEntity, [
		SendPropFloat(FIELD.OF(nameof(Scale)), 0, PropFlags.NoScale),
		SendPropInt(FIELD.OF(nameof(State)), 8, PropFlags.Unsigned),
		SendPropFloat(FIELD.OF(nameof(Duration)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(StartTime)), 0, PropFlags.NoScale),
		SendPropInt(FIELD.OF(nameof(Spawnflags)), 32, PropFlags.Unsigned),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_CitadelEnergyCore);

	[NetworkName("m_flScale")]
	public float Scale;
	[NetworkName("m_nState")]
	public int State;
	[NetworkName("m_flDuration")]
	public float Duration;
	[NetworkName("m_flStartTime")]
	public float StartTime;
	[NetworkName("m_spawnflags")]
	public int Spawnflags;
}
