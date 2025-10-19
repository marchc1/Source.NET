using Game.Shared;

using Source.Common;

namespace Game.Server;

public class FuncReflectiveGlass : Breakable
{
	public static readonly SendTable DT_FuncReflectiveGlass = new(DT_BaseEntity, []);
	public static readonly new ServerClass ServerClass = new ServerClass("FuncReflectiveGlass", DT_FuncReflectiveGlass).WithManualClassID(StaticClassIndices.CFuncReflectiveGlass);
}
