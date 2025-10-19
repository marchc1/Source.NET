using Source.Common;
using Source;

using Game.Shared;
using System.Numerics;
using Source.Common.MaterialSystem;
using System;

namespace Game.Server;


using FIELD = FIELD<FuncOccluder>;

public class FuncOccluder : BaseEntity
{
	public static readonly SendTable DT_FuncOccluder = new([
		SendPropBool(FIELD.OF(nameof(Active))),
		SendPropInt(FIELD.OF(nameof(OccluderIndex)), 10, PropFlags.Unsigned),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass("FuncOccluder", DT_FuncOccluder).WithManualClassID(StaticClassIndices.CFuncOccluder);

	public bool Active;
	public int OccluderIndex;
}
