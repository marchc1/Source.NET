using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_VGuiScreen>;
[NetworkName("CVGuiScreen")]
public class C_VGuiScreen : C_BaseEntity
{
	public static readonly RecvTable DT_VGuiScreen = new(DT_BaseEntity, [
		RecvPropFloat(FIELD.OF(nameof(Width))),
		RecvPropFloat(FIELD.OF(nameof(Height))),
		RecvPropInt(FIELD.OF(nameof(AttachmentIndex))),
		RecvPropInt(FIELD.OF(nameof(PanelName))),
		RecvPropInt(FIELD.OF(nameof(ScreenFlags))),
		RecvPropInt(FIELD.OF(nameof(OverlayMaterial))),
		RecvPropEHandle(FIELD.OF(nameof(HPlayerOwner))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass(DT_VGuiScreen);

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
