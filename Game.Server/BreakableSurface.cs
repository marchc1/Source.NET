using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<BreakableSurface>;
public class BreakableSurface : BaseEntity
{
	public static readonly SendTable DT_BreakableSurface = new(DT_BaseEntity, [
		SendPropInt(FIELD.OF(nameof(NumWide)), 8, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(NumHigh)), 8, PropFlags.Unsigned),
		SendPropFloat(FIELD.OF(nameof(PanelWidth)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(PanelHeight)), 0, PropFlags.NoScale),
		SendPropVector(FIELD.OF(nameof(VNormal)), 0, PropFlags.Coord),
		SendPropVector(FIELD.OF(nameof(VCorner)), 0, PropFlags.Coord),
		SendPropInt(FIELD.OF(nameof(IsBroken)), 1, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(SurfaceType)), 2, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(RawPanelBitVec)), 1, PropFlags.ProxyAlwaysYes | PropFlags.Unsigned),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass("BreakableSurface", DT_BreakableSurface).WithManualClassID(StaticClassIndices.CBreakableSurface);

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
