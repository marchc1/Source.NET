using Source.Common;
using Source;

using Game.Shared;

using System.Numerics;
namespace Game.Client;

using FIELD = FIELD<C_BreakableSurface>;
[NetworkName("CBreakableSurface")]
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
		RecvPropArray3(FIELD.OF_ARRAY(nameof(RawPanelBitVec)), RecvPropInt(null!, null!, sizeOfVar: sizeof(int))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass(DT_BreakableSurface);

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
