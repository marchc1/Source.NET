using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<VGuiScreen>;
[LinkEntityToClass("vgui_screen")]
[LinkEntityToClass("vgui_screen_team")]
[NetworkName("CVGuiScreen")]
public class VGuiScreen : BaseEntity
{
	public static readonly SendTable DT_VGuiScreen = new(DT_BaseEntity, [
		SendPropFloat(FIELD.OF(nameof(Width)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(Height)), 0, PropFlags.NoScale),
		SendPropInt(FIELD.OF(nameof(AttachmentIndex)), 5, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(PanelName)), 8, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(ScreenFlags)), 5, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(OverlayMaterial)), 10, PropFlags.Unsigned),
		SendPropEHandle(FIELD.OF(nameof(HPlayerOwner))),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_VGuiScreen);

	[NetworkName("m_flWidth")]
	public float Width;
	[NetworkName("m_flHeight")]
	public float Height;
	[NetworkName("m_nAttachmentIndex")]
	public int AttachmentIndex;
	[NetworkName("m_nPanelName")]
	public int PanelName;
	[NetworkName("m_fScreenFlags")]
	public int ScreenFlags;
	[NetworkName("m_nOverlayMaterial")]
	public int OverlayMaterial;
	[NetworkName("m_hPlayerOwner")]
	public EHANDLE HPlayerOwner = new();
}
