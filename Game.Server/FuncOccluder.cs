using Source.Common;
using Source;

using Game.Shared;
using System.Numerics;
using Source.Common.MaterialSystem;
using System;

namespace Game.Server;


using FIELD = FIELD<FuncOccluder>;

[NetworkName("CFuncOccluder")]
public partial class FuncOccluder : BaseEntity
{
	public static readonly SendTable DT_FuncOccluder = new([
		SendPropBool(NetworkVarFields.Active),
		SendPropInt(NetworkVarFields.OccluderIndex, 10, PropFlags.Unsigned),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_FuncOccluder);

	[NetworkName("m_bActive")]
	[NetworkVar] public partial bool Active { get; set; }
	[NetworkName("m_nOccluderIndex")]
	[NetworkVar] public partial int OccluderIndex { get; set; }
}
