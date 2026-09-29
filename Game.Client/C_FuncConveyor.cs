using Source.Common;
using Source;

using Game.Shared;

namespace Game.Client;

using FIELD = FIELD<C_FuncConveyor>;

[NetworkName("CFuncConveyor")]
public class C_FuncConveyor : C_BaseEntity
{
	public static readonly RecvTable DT_FuncConveyor = new(DT_BaseEntity, [
		RecvPropFloat(FIELD.OF(nameof(ConveyorSpeed))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass(DT_FuncConveyor);
	[NetworkName("m_flConveyorSpeed")]
	public float ConveyorSpeed;
}
