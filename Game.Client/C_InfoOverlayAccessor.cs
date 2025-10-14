using Game.Shared;

using Source.Common;

namespace Game.Client;
using FIELD = Source.FIELD<C_InfoOverlayAccessor>;

public partial class C_InfoOverlayAccessor : C_BaseEntity
{
	public static readonly RecvTable DT_InfoOverlayAccessor = new( [
		RecvPropInt(FIELD.OF(nameof(TextureFrameIndex))),
		RecvPropInt(FIELD.OF(nameof(OverlayID)))
	]);
	public static readonly new ClientClass ClientClass = new ClientClass("InfoOverlayAccessor", DT_InfoOverlayAccessor).WithManualClassID(StaticClassIndices.CInfoOverlayAccessor);

	public int OverlayID;
}
