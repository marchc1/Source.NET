using Game.Shared;

using Source.Common;

namespace Game.Server;

public class FuncMonitor : FuncBrush
{
	public static readonly SendTable DT_FuncMonitor = new(DT_BaseEntity, []);
	public static readonly new ServerClass ServerClass = new ServerClass("FuncMonitor", DT_FuncMonitor).WithManualClassID(StaticClassIndices.CFuncMonitor);
}
