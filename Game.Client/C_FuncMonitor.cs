using Game.Shared;

using Source.Common;

namespace Game.Client;

public class C_FuncMonitor : C_BaseEntity
{
	public static readonly RecvTable DT_FuncMonitor = new(DT_BaseEntity, []);
	public static readonly new ClientClass ClientClass = new ClientClass("FuncMonitor", DT_FuncMonitor).WithManualClassID(StaticClassIndices.CFuncMonitor);
}
