using Source.Common;
using Source;

using Game.Shared;

namespace Game.Server;

using FIELD = FIELD<FuncConveyor>;

public class FuncConveyor : FuncWall
{
	public static readonly SendTable DT_FuncConveyor = new(DT_BaseEntity, [
		SendPropFloat(FIELD.OF(nameof(ConveyorSpeed)), 0, PropFlags.NoScale),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass("FuncConveyor", DT_FuncConveyor).WithManualClassID(StaticClassIndices.CFuncConveyor);

	public float ConveyorSpeed;
}
