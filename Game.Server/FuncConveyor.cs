using Source.Common;
using Source;

using Game.Shared;

namespace Game.Server;

using FIELD = FIELD<FuncConveyor>;

[LinkEntityToClass("func_conveyor")]
[NetworkName("CFuncConveyor")]
public class FuncConveyor : FuncWall
{
	public static readonly SendTable DT_FuncConveyor = new(DT_BaseEntity, [
		SendPropFloat(FIELD.OF(nameof(ConveyorSpeed)), 0, PropFlags.NoScale),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_FuncConveyor);

	[NetworkName("m_flConveyorSpeed")]
	public float ConveyorSpeed;
}
