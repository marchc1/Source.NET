using Source.Common;
using Source;

using Game.Shared;

using System.Numerics;
namespace Game.Server;

using FIELD = FIELD<BreakableSurface>;
[NetworkName("CBreakableSurface")]
public partial class BreakableSurface : BaseEntity
{
	public static readonly SendTable DT_BreakableSurface = new(DT_BaseEntity, [
		SendPropInt(NetworkVarFields.NumWide, 8, PropFlags.Unsigned),
		SendPropInt(NetworkVarFields.NumHigh, 8, PropFlags.Unsigned),
		SendPropFloat(NetworkVarFields.PanelWidth, 0, PropFlags.NoScale),
		SendPropFloat(NetworkVarFields.PanelHeight, 0, PropFlags.NoScale),
		SendPropVector(NetworkVarFields.VNormal, 0, PropFlags.Coord),
		SendPropVector(NetworkVarFields.VCorner, 0, PropFlags.Coord),
		SendPropInt(NetworkVarFields.IsBroken, 1, PropFlags.Unsigned),
		SendPropInt(NetworkVarFields.SurfaceType, 2, PropFlags.Unsigned),
		SendPropArray3(BreakableSurface.NetworkVarFields.RawPanelBitVec, SendPropInt((IFieldAccessor)null!, 1, PropFlags.Unsigned, sizeOfVar: sizeof(int))),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_BreakableSurface);

	[NetworkName("m_nNumWide")]
	[NetworkVar] public partial int NumWide { get; set; }
	[NetworkName("m_nNumHigh")]
	[NetworkVar] public partial int NumHigh { get; set; }
	[NetworkName("m_flPanelWidth")]
	[NetworkVar] public partial float PanelWidth { get; set; }
	[NetworkName("m_flPanelHeight")]
	[NetworkVar] public partial float PanelHeight { get; set; }
	[NetworkName("m_vNormal")]
	[NetworkVar] public partial Vector3 VNormal { get; set; }
	[NetworkName("m_vCorner")]
	[NetworkVar] public partial Vector3 VCorner { get; set; }
	[NetworkName("m_bIsBroken")]
	[NetworkVar] public partial int IsBroken { get; set; }
	[NetworkName("m_nSurfaceType")]
	[NetworkVar] public partial int SurfaceType { get; set; }
	[NetworkName("m_RawPanelBitVec")]
	[NetworkVar] public partial NetworkArray<InlineArray256<int>, int> RawPanelBitVec { get; }
}
