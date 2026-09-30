using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<VGuiScreen>;
[NetworkName("CVGuiScreen")]
public partial class VGuiScreen : BaseEntity
{
	public static readonly SendTable DT_VGuiScreen = new(DT_BaseEntity, [
		SendPropFloat(NetworkVarFields.Width, 0, PropFlags.NoScale),
		SendPropFloat(NetworkVarFields.Height, 0, PropFlags.NoScale),
		SendPropInt(NetworkVarFields.AttachmentIndex, 5, PropFlags.Unsigned),
		SendPropInt(NetworkVarFields.PanelName, 8, PropFlags.Unsigned),
		SendPropInt(NetworkVarFields.ScreenFlags, 5, PropFlags.Unsigned),
		SendPropInt(NetworkVarFields.OverlayMaterial, 10, PropFlags.Unsigned),
		SendPropEHandle(VGuiScreen.NetworkVarFields.HPlayerOwner),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_VGuiScreen);

	[NetworkName("m_flWidth")]
	[NetworkVar] public partial float Width { get; set; }
	[NetworkName("m_flHeight")]
	[NetworkVar] public partial float Height { get; set; }
	[NetworkName("m_nAttachmentIndex")]
	[NetworkVar] public partial int AttachmentIndex { get; set; }
	[NetworkName("m_nPanelName")]
	[NetworkVar] public partial int PanelName { get; set; }
	[NetworkName("m_fScreenFlags")]
	[NetworkVar] public partial int ScreenFlags { get; set; }
	[NetworkName("m_nOverlayMaterial")]
	[NetworkVar] public partial int OverlayMaterial { get; set; }
	[NetworkName("m_hPlayerOwner")]
	[NetworkVar] public partial NetworkHandle<BaseEntity> HPlayerOwner { get; }
}
