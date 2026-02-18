using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<VGuiScreen>;
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
	public static readonly new ServerClass ServerClass = new ServerClass("VGuiScreen", DT_VGuiScreen).WithManualClassID(StaticClassIndices.CVGuiScreen);

	public float Width;
	public float Height;
	public int AttachmentIndex;
	public int PanelName;
	public int ScreenFlags;
	public int OverlayMaterial;
	public EHANDLE HPlayerOwner = new();
}
