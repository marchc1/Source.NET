using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_BreakableSurface>;
public class C_BreakableSurface : C_BaseEntity
{
	public static readonly RecvTable DT_BreakableSurface = new(DT_BaseEntity, [
		RecvPropInt(FIELD.OF(nameof(NumWide))),
		RecvPropInt(FIELD.OF(nameof(NumHigh))),
		RecvPropFloat(FIELD.OF(nameof(PanelWidth))),
		RecvPropFloat(FIELD.OF(nameof(PanelHeight))),
		RecvPropVector(FIELD.OF(nameof(VNormal))),
		RecvPropVector(FIELD.OF(nameof(VCorner))),
		RecvPropInt(FIELD.OF(nameof(IsBroken))),
		RecvPropInt(FIELD.OF(nameof(SurfaceType))),
		RecvPropInt(FIELD.OF(nameof(RawPanelBitVec))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass("BreakableSurface", DT_BreakableSurface).WithManualClassID(StaticClassIndices.CBreakableSurface);

	public int NumWide;
	public int NumHigh;
	public float PanelWidth;
	public float PanelHeight;
	public Vector3 VNormal;
	public Vector3 VCorner;
	public int IsBroken;
	public int SurfaceType;
	public int RawPanelBitVec;
}
