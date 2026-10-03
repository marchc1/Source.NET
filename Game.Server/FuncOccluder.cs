using Source.Common;
using Source;

using Game.Shared;
using System.Numerics;
using Source.Common.MaterialSystem;
using System;

namespace Game.Server;


using FIELD = FIELD<FuncOccluder>;

[LinkEntityToClass("func_occluder")]
[NetworkName("CFuncOccluder")]
public class FuncOccluder : BaseEntity
{
	public static readonly SendTable DT_FuncOccluder = new([
		SendPropBool(FIELD.OF(nameof(Active))),
		SendPropInt(FIELD.OF(nameof(OccluderIndex)), 10, PropFlags.Unsigned),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_FuncOccluder);

	[NetworkName("m_bActive")]
	public bool Active;
	[NetworkName("m_nOccluderIndex")]
	public int OccluderIndex;
}
