using Game.Shared;

using Source.Common;

namespace Game.Server;

[LinkEntityToClass("func_monitor")]
[NetworkName("CFuncMonitor")]
public class FuncMonitor : FuncBrush
{
	public static readonly SendTable DT_FuncMonitor = new(DT_BaseEntity, []);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_FuncMonitor);
}
