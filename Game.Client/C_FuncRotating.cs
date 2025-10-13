using Game.Shared;

using Source;
using Source.Common;
namespace Game.Client;
using FIELD = FIELD<C_FuncRotating>;

public class C_FuncRotating : C_BaseEntity
{
	public static readonly RecvTable DT_FuncRotating = new(DT_BaseEntity, [
		RecvPropVector(FIELD.OF_NAMED(nameof(NetworkOrigin), "Origin")),
		RecvPropFloat(FIELD.OF_NAMED($"{nameof(NetworkAngles)}[0]", $"{nameof(Rotation)}[0]")),
		RecvPropFloat(FIELD.OF_NAMED($"{nameof(NetworkAngles)}[1]", $"{nameof(Rotation)}[1]")),
		RecvPropFloat(FIELD.OF_NAMED($"{nameof(NetworkAngles)}[2]", $"{nameof(Rotation)}[2]")),
		RecvPropInt(FIELD.OF(nameof(SimulationTime)), 0, RecvProxy_SimulationTime)
	]);
	public static readonly new ClientClass ClientClass = new ClientClass("FuncRotating", DT_FuncRotating).WithManualClassID(StaticClassIndices.CFuncRotating);
}

