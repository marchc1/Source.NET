using Source.Common;
using Source;

using Game.Shared;
using System.Numerics;
using Source.Common.MaterialSystem;

namespace Game.Server;


using FIELD = FIELD<LightGlow>;

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
		SendPropVector(FIELD.OF(nameof(Origin)), 0, PropFlags.Coord),
		SendPropVector(FIELD.OF(nameof(Rotation)), 13, PropFlags.RoundDown, 0, 360),
		SendPropEHandle(FIELD.OF(nameof(MoveParent))),
		SendPropFloat(FIELD.OF(nameof(GlowProxySize)), 6, PropFlags.RoundUp, 1, 64),
		SendPropFloat(FIELD.OF(nameof(HDRColorScale)), 0, PropFlags.NoScale),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass("LightGlow", DT_LightGlow).WithManualClassID(StaticClassIndices.CLightGlow);

	public Color RenderColor;
	public int HorizontalSize;
	public int VerticalSize;
	public int MinDist;
	public int MaxDist;
	public int OuterMaxDist;
	public float GlowProxySize;
	public float HDRColorScale;
}
