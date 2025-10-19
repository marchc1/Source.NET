using Game.Shared;

using Source;
using Source.Common;

namespace Game.Server;
using FIELD = FIELD<InfoOverlayAccessor>;
public partial class InfoOverlayAccessor : BaseEntity
{
	public static readonly SendTable DT_InfoOverlayAccessor = new([
		SendPropInt(FIELD.OF(nameof(TextureFrameIndex)), 8, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(OverlayID)), 8, PropFlags.Unsigned)
	]);
	public static readonly new ServerClass ServerClass = new ServerClass("InfoOverlayAccessor", DT_InfoOverlayAccessor).WithManualClassID(StaticClassIndices.CInfoOverlayAccessor);

	public int OverlayID;
}
