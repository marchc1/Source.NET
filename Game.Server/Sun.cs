using Game.Shared;

using Source;
using Source.Common;

using System.Numerics;

namespace Game.Server;
using FIELD = Source.FIELD<Sun>;

[LinkEntityToClass("env_sun")]
[NetworkName("CSun")]
public class Sun : BaseEntity
{
	public static readonly SendTable DT_Sun = new([
		SendPropInt(FIELD.OF(nameof(Render)), 32, PropFlags.Unsigned, SendProxy_Color32ToInt),
		SendPropInt(FIELD.OF(nameof(Overlay)), 32, PropFlags.Unsigned, SendProxy_Color32ToInt),
		SendPropVector(FIELD.OF(nameof(Direction)), 0, PropFlags.Normal),
		SendPropInt(FIELD.OF(nameof(On)), 1, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(Size)), 10, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(OverlaySize)), 10, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(Material)), 32, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(OverlayMaterial)), 32, PropFlags.Unsigned),
		SendPropFloat(FIELD.OF(nameof(HDRColorScale)), 0, PropFlags.NoScale, 0.0f, 100.0f),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_Sun);

	[NetworkName("m_clrRender")]
	public Color Render;
	[NetworkName("m_clrOverlay")]
	public Color Overlay;
	[NetworkName("m_vDirection")]
	public Vector3 Direction;
	[NetworkName("m_bOn")]
	public bool On;
	[NetworkName("m_nSize")]
	public int Size;
	[NetworkName("m_nOverlaySize")]
	public int OverlaySize;
	[NetworkName("m_nMaterial")]
	public int Material;
	[NetworkName("m_nOverlayMaterial")]
	public int OverlayMaterial;
	public int HDRColorScale;
}
