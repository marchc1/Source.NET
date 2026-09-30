using Source.Common;
using Source;

using Game.Shared;

namespace Game.Server;

using FIELD = FIELD<FuncConveyor>;

[NetworkName("CFuncConveyor")]
public partial class FuncConveyor : FuncWall
{
	public static readonly SendTable DT_FuncConveyor = new(DT_BaseEntity, [
		SendPropFloat(NetworkVarFields.ConveyorSpeed, 0, PropFlags.NoScale),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_FuncConveyor);

	[NetworkName("m_flConveyorSpeed")]
	[NetworkVar] public partial float ConveyorSpeed { get; set; }
}
