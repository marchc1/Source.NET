using Game.Shared;

using Source;
using Source.Common;

namespace Game.Client;
using FIELD = FIELD<C_LightGlow>;

public class C_LightGlow : C_BaseEntity
{
	public static readonly RecvTable DT_LightGlow = new([
		RecvPropInt(FIELD.OF(nameof(RenderColor))),
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
		RecvPropFloat(FIELD.OF(nameof(HDRColorScale))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass("LightGlow", DT_LightGlow).WithManualClassID(StaticClassIndices.CLightGlow);

	public Color RenderColor;
	public int HorizontalSize;
	public int VerticalSize;
	public int MinDist;
	public int MaxDist;
	public int OuterMaxDist;
	public float GlowProxySize;
	public float HDRColorScale;
}

