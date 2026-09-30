using Source.Common;
using Source;

using Game.Shared;
using System.Numerics;
using Source.Common.MaterialSystem;

namespace Game.Server;


using FIELD = FIELD<LightGlow>;

[NetworkName("CLightGlow")]
public partial class LightGlow : BaseEntity
{
	public static readonly SendTable DT_LightGlow = new([
		SendPropInt(NetworkVarFields.RenderColor, 32, PropFlags.Unsigned),
		SendPropInt(NetworkVarFields.HorizontalSize, 16, PropFlags.Unsigned),
		SendPropInt(NetworkVarFields.VerticalSize, 16, PropFlags.Unsigned),
		SendPropInt(NetworkVarFields.MinDist, 16, PropFlags.Unsigned),
		SendPropInt(NetworkVarFields.MaxDist, 16, PropFlags.Unsigned),
		SendPropInt(NetworkVarFields.OuterMaxDist, 16, PropFlags.Unsigned),
		SendPropInt(BaseEntity.NetworkVarFields.SpawnFlags, 8, PropFlags.Unsigned),
		SendPropVector(BaseEntity.NetworkVarFields.Origin, 0, PropFlags.Coord),
		SendPropQAngles(BaseEntity.NetworkVarFields.Rotation, 13, PropFlags.RoundDown),
		SendPropEHandle(FIELD.OF(nameof(MoveParent))),
		SendPropFloat(NetworkVarFields.GlowProxySize, 6, PropFlags.RoundUp, 0.0f, 64.0f),
		SendPropFloat(NetworkVarFields.HDRColorScale, 0, PropFlags.NoScale),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_LightGlow);

	[NetworkName("m_clrRender")]
	[NetworkVar] public partial Color RenderColor { get; set; }
	[NetworkName("m_nHorizontalSize")]
	[NetworkVar] public partial int HorizontalSize { get; set; }
	[NetworkName("m_nVerticalSize")]
	[NetworkVar] public partial int VerticalSize { get; set; }
	[NetworkName("m_nMinDist")]
	[NetworkVar] public partial int MinDist { get; set; }
	[NetworkName("m_nMaxDist")]
	[NetworkVar] public partial int MaxDist { get; set; }
	[NetworkName("m_nOuterMaxDist")]
	[NetworkVar] public partial int OuterMaxDist { get; set; }
	[NetworkName("m_flGlowProxySize")]
	[NetworkVar] public partial float GlowProxySize { get; set; }
	[NetworkVar] public partial float HDRColorScale { get; set; }
}
