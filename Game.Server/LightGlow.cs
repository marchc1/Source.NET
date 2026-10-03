using Source.Common;
using Source;

using Game.Shared;
using System.Numerics;
using Source.Common.MaterialSystem;

namespace Game.Server;


using FIELD = FIELD<LightGlow>;

[LinkEntityToClass("env_lightglow")]
[NetworkName("CLightGlow")]
public class LightGlow : BaseEntity
{
	public static readonly SendTable DT_LightGlow = new([
		SendPropInt(FIELD.OF(nameof(RenderColor)), 32, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(HorizontalSize)), 16, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(VerticalSize)), 16, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(MinDist)), 16, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(MaxDist)), 16, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(OuterMaxDist)), 16, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(SpawnFlags)), 8, PropFlags.Unsigned),
		SendPropVector(NetworkVarFields.Origin, 0, PropFlags.Coord),
		SendPropQAngles(FIELD.OF(nameof(Rotation)), 13, PropFlags.RoundDown),
		SendPropEHandle(FIELD.OF(nameof(MoveParent))),
		SendPropFloat(FIELD.OF(nameof(GlowProxySize)), 6, PropFlags.RoundUp, 0.0f, 64.0f),
		SendPropFloat(FIELD.OF(nameof(HDRColorScale)), 0, PropFlags.NoScale),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_LightGlow);

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
