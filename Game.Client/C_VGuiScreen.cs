using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_VGuiScreen>;
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
	public static readonly new ClientClass ClientClass = new ClientClass("VGuiScreen", DT_VGuiScreen).WithManualClassID(StaticClassIndices.CVGuiScreen);

	public float Width;
	public float Height;
	public int AttachmentIndex;
	public int PanelName;
	public int ScreenFlags;
	public int OverlayMaterial;
	public EHANDLE HPlayerOwner = new();
}
