using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_CitadelEnergyCore>;
public class C_CitadelEnergyCore : C_BaseEntity
{
	public static readonly RecvTable DT_CitadelEnergyCore = new(DT_BaseEntity, [
		RecvPropFloat(FIELD.OF(nameof(Scale))),
		RecvPropInt(FIELD.OF(nameof(State))),
		RecvPropFloat(FIELD.OF(nameof(Duration))),
		RecvPropFloat(FIELD.OF(nameof(StartTime))),
		RecvPropInt(FIELD.OF(nameof(Spawnflags))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass("CitadelEnergyCore", DT_CitadelEnergyCore).WithManualClassID(StaticClassIndices.CCitadelEnergyCore);

	public float Scale;
	public int State;
	public float Duration;
	public float StartTime;
	public int Spawnflags;
}
