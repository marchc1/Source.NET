using Game.Shared;

using Source;
using Source.Common;

using System.Numerics;

namespace Game.Server;
using FIELD = Source.FIELD<Sun>;

[LinkEntityToClass("env_sun")]
[NetworkName("CSun")]
public partial class Sun : BaseEntity
{
	public static readonly SendTable DT_Sun = new([
		SendPropInt(NetworkVarFields.Render, 32, PropFlags.Unsigned, SendProxy_Color32ToInt),
		SendPropInt(NetworkVarFields.Overlay, 32, PropFlags.Unsigned, SendProxy_Color32ToInt),
		SendPropVector(NetworkVarFields.Direction, 0, PropFlags.Normal),
		SendPropInt(NetworkVarFields.On, 1, PropFlags.Unsigned),
		SendPropInt(NetworkVarFields.Size, 10, PropFlags.Unsigned),
		SendPropInt(NetworkVarFields.OverlaySize, 10, PropFlags.Unsigned),
		SendPropInt(NetworkVarFields.Material, 32, PropFlags.Unsigned),
		SendPropInt(NetworkVarFields.OverlayMaterial, 32, PropFlags.Unsigned),
		SendPropFloat(NetworkVarFields.HDRColorScale, 0, PropFlags.NoScale, 0.0f, 100.0f),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_Sun);

	[NetworkName("m_clrRender")]
	[NetworkVar] public partial Color Render { get; set; }
	[NetworkName("m_clrOverlay")]
	[NetworkVar] public partial Color Overlay { get; set; }
	[NetworkName("m_vDirection")]
	[NetworkVar] public partial Vector3 Direction { get; set; }
	[NetworkName("m_bOn")]
	[NetworkVar] public partial bool On { get; set; }
	[NetworkName("m_nSize")]
	[NetworkVar] public partial int Size { get; set; }
	[NetworkName("m_nOverlaySize")]
	[NetworkVar] public partial int OverlaySize { get; set; }
	[NetworkName("m_nMaterial")]
	[NetworkVar] public partial int Material { get; set; }
	[NetworkName("m_nOverlayMaterial")]
	[NetworkVar] public partial int OverlayMaterial { get; set; }
	[NetworkVar] public partial int HDRColorScale { get; set; }
}
