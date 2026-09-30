using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<Plasma>;
[NetworkName("CPlasma")]
public partial class Plasma : BaseEntity
{
	public static readonly SendTable DT_Plasma = new(DT_BaseEntity, [
		SendPropFloat(NetworkVarFields.Scale, 0, PropFlags.NoScale),
		SendPropFloat(NetworkVarFields.ScaleTime, 0, PropFlags.NoScale),
		SendPropInt(NetworkVarFields.Flags, 8, PropFlags.Unsigned),
		SendPropInt(NetworkVarFields.PlasmaModelIndex, 14, 0),
		SendPropInt(NetworkVarFields.PlasmaModelIndex2, 14, 0),
		SendPropInt(NetworkVarFields.GlowModelIndex, 14, 0),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_Plasma);

	[NetworkName("m_flScale")]
	[NetworkVar] public partial float Scale { get; set; }
	[NetworkName("m_flScaleTime")]
	[NetworkVar] public partial float ScaleTime { get; set; }
	[NetworkName("m_nFlags")]
	[NetworkVar] public partial int Flags { get; set; }
	[NetworkName("m_nPlasmaModelIndex")]
	[NetworkVar] public partial int PlasmaModelIndex { get; set; }
	[NetworkName("m_nPlasmaModelIndex2")]
	[NetworkVar] public partial int PlasmaModelIndex2 { get; set; }
	[NetworkName("m_nGlowModelIndex")]
	[NetworkVar] public partial int GlowModelIndex { get; set; }
}
