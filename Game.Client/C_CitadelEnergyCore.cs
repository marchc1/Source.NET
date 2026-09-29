using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_CitadelEnergyCore>;
[NetworkName("CCitadelEnergyCore")]
public class C_CitadelEnergyCore : C_BaseEntity
{
	public static readonly RecvTable DT_CitadelEnergyCore = new(DT_BaseEntity, [
		RecvPropFloat(FIELD.OF(nameof(Scale))),
		RecvPropInt(FIELD.OF(nameof(State))),
		RecvPropFloat(FIELD.OF(nameof(Duration))),
		RecvPropFloat(FIELD.OF(nameof(StartTime))),
		RecvPropInt(FIELD.OF(nameof(Spawnflags))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass(DT_CitadelEnergyCore);

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
