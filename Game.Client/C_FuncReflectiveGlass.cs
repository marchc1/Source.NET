using Game.Shared;

using Source.Common;

namespace Game.Client;

public class C_FuncReflectiveGlass : C_BaseEntity
{
	public static readonly RecvTable DT_FuncReflectiveGlass = new(DT_BaseEntity, []);
	public static readonly new ClientClass ClientClass = new ClientClass("FuncReflectiveGlass", DT_FuncReflectiveGlass).WithManualClassID(StaticClassIndices.CFuncReflectiveGlass);
}
