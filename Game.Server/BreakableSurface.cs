using Source.Common;
using Source;

using Game.Shared;

using System.Numerics;
namespace Game.Server;

using FIELD = FIELD<BreakableSurface>;
[LinkEntityToClass("func_breakable_surf")]
[NetworkName("CBreakableSurface")]
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
		SendPropArray3(FIELD.OF_ARRAY(nameof(RawPanelBitVec)), SendPropInt((IFieldAccessor)null!, 1, PropFlags.Unsigned, sizeOfVar: sizeof(int))),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_BreakableSurface);

	[NetworkName("m_nNumWide")]
	public int NumWide;
	[NetworkName("m_nNumHigh")]
	public int NumHigh;
	[NetworkName("m_flPanelWidth")]
	public float PanelWidth;
	[NetworkName("m_flPanelHeight")]
	public float PanelHeight;
	[NetworkName("m_vNormal")]
	public Vector3 VNormal;
	[NetworkName("m_vCorner")]
	public Vector3 VCorner;
	[NetworkName("m_bIsBroken")]
	public int IsBroken;
	[NetworkName("m_nSurfaceType")]
	public int SurfaceType;
	[NetworkName("m_RawPanelBitVec")]
	public InlineArray256<int> RawPanelBitVec;
}
