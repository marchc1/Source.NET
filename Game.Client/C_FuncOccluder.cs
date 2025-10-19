using Game.Shared;

using Source;
using Source.Common;

namespace Game.Client;
using FIELD = FIELD<C_FuncOccluder>;

public class C_FuncOccluder : C_BaseEntity
{
	public static readonly RecvTable DT_FuncOccluder = new([
		RecvPropBool(FIELD.OF(nameof(Active))),
		RecvPropInt(FIELD.OF(nameof(OccluderIndex))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass("FuncOccluder", DT_FuncOccluder).WithManualClassID(StaticClassIndices.CFuncOccluder);

	public bool Active;
	public int OccluderIndex;
}

