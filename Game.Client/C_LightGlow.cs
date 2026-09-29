using Game.Shared;

using Source;
using Source.Common;

namespace Game.Client;
using FIELD = FIELD<C_LightGlow>;

[NetworkName("CLightGlow")]
public class C_LightGlow : C_BaseEntity
{
	public static readonly RecvTable DT_LightGlow = new([
		RecvPropInt(FIELD.OF(nameof(RenderColor)), 0, RecvProxy_IntToColor32),
		RecvPropInt(FIELD.OF(nameof(HorizontalSize))),
		RecvPropInt(FIELD.OF(nameof(VerticalSize))),
		RecvPropInt(FIELD.OF(nameof(MinDist))),
		RecvPropInt(FIELD.OF(nameof(MaxDist))),
		RecvPropInt(FIELD.OF(nameof(OuterMaxDist))),
		RecvPropInt(FIELD.OF(nameof(SpawnFlags))),
		RecvPropVector(FIELD.OF(nameof(Origin))),
		RecvPropVector(FIELD.OF(nameof(Rotation))),
		RecvPropEHandle(FIELD.OF(nameof(MoveParent))),
		RecvPropFloat(FIELD.OF(nameof(GlowProxySize))),
		// todo: RecvProxy_HDRColorScale
		RecvPropFloat(FIELD.OF(nameof(HDRColorScale))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass(DT_LightGlow);

	[NetworkName("m_clrRender")]
	public Color RenderColor;
	[NetworkName("m_nHorizontalSize")]
	public int HorizontalSize;
	[NetworkName("m_nVerticalSize")]
	public int VerticalSize;
	[NetworkName("m_nMinDist")]
	public int MinDist;
	[NetworkName("m_nMaxDist")]
	public int MaxDist;
	[NetworkName("m_nOuterMaxDist")]
	public int OuterMaxDist;
	[NetworkName("m_flGlowProxySize")]
	public float GlowProxySize;
	public float HDRColorScale;
}

