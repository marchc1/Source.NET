using Source.Common;
using Source;

using Game.Shared;

namespace Game.Server;


using FIELD = FIELD<FuncRotating>;

public class FuncRotating : BaseEntity
{
	public static readonly SendTable DT_FuncRotating = new(DT_BaseEntity, [
		SendPropExclude(nameof(DT_BaseEntity), nameof(Rotation)),
		SendPropExclude(nameof(DT_BaseEntity), nameof(Origin)),
		SendPropExclude(nameof(DT_BaseEntity), nameof(SimulationTime)),

		SendPropVector(FIELD.OF(nameof(Origin)), 0, PropFlags.Coord | PropFlags.ChangesOften, 0, Constants.HIGH_DEFAULT, SendProxy_FuncRotatingOrigin),
		SendPropAngle(FIELD.OF_VECTORELEM(nameof(Rotation), 0), 13, PropFlags.RoundDown | PropFlags.ChangesOften, proxyFn: SendProxy_FuncRotatingAngle),
		SendPropAngle(FIELD.OF_VECTORELEM(nameof(Rotation), 1), 13, PropFlags.RoundDown | PropFlags.ChangesOften, proxyFn: SendProxy_FuncRotatingAngle),
		SendPropAngle(FIELD.OF_VECTORELEM(nameof(Rotation), 2), 13, PropFlags.RoundDown | PropFlags.ChangesOften, proxyFn: SendProxy_FuncRotatingAngle),
		SendPropInt(FIELD.OF(nameof(SimulationTime)), SIMULATION_TIME_WINDOW_BITS, PropFlags.Unsigned | PropFlags.ChangesOften | PropFlags.EncodedAgainstTickCount, SendProxy_FuncRotatingSimulationTime)
	]);

	private static void SendProxy_FuncRotatingOrigin(SendProp prop, object instance, IFieldAccessor field, ref DVariant outData, int element, int objectID) {
		throw new NotImplementedException();
	}

	private static void SendProxy_FuncRotatingAngle(SendProp prop, object instance, IFieldAccessor field, ref DVariant outData, int element, int objectID) {
		throw new NotImplementedException();
	}

	private static void SendProxy_FuncRotatingSimulationTime(SendProp prop, object instance, IFieldAccessor field, ref DVariant outData, int element, int objectID) {
		throw new NotImplementedException();
	}

	public static readonly new ServerClass ServerClass = new ServerClass("FuncRotating", DT_FuncRotating).WithManualClassID(StaticClassIndices.CFuncRotating);
}
