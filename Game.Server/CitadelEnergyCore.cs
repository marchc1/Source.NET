using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<CitadelEnergyCore>;
public class CitadelEnergyCore : BaseEntity
{
	public static readonly SendTable DT_CitadelEnergyCore = new(DT_BaseEntity, [
		SendPropFloat(FIELD.OF(nameof(Scale)), 0, PropFlags.NoScale),
		SendPropInt(FIELD.OF(nameof(State)), 8, PropFlags.Unsigned),
		SendPropFloat(FIELD.OF(nameof(Duration)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(StartTime)), 0, PropFlags.NoScale),
		SendPropInt(FIELD.OF(nameof(Spawnflags)), 32, PropFlags.Unsigned),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass("CitadelEnergyCore", DT_CitadelEnergyCore).WithManualClassID(StaticClassIndices.CCitadelEnergyCore);

	public float Scale;
	public int State;
	public float Duration;
	public float StartTime;
	public int Spawnflags;
}
