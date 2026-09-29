using Game.Client;
using Game.Shared;

using Source;
using Source.Common;

using System.Numerics;

namespace Game.Server;
using FIELD = Source.FIELD<C_Sun>;

[NetworkName("CSun")]
public class C_Sun : C_BaseEntity
{
	public static readonly RecvTable DT_Sun = new([
		RecvPropInt(FIELD.OF(nameof(Render)), 0, RecvProxy_IntToColor32),
		RecvPropInt(FIELD.OF(nameof(Overlay)), 0, RecvProxy_IntToColor32),
		RecvPropVector(FIELD.OF(nameof(Direction))),
		RecvPropInt(FIELD.OF(nameof(On))),
		RecvPropInt(FIELD.OF(nameof(Size))),
		RecvPropInt(FIELD.OF(nameof(OverlaySize))),
		RecvPropInt(FIELD.OF(nameof(Material))),
		RecvPropInt(FIELD.OF(nameof(OverlayMaterial))),
		// todo: RecvProxy_HDRColorScale
		RecvPropFloat(FIELD.OF(nameof(HDRColorScale))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass(DT_Sun);

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