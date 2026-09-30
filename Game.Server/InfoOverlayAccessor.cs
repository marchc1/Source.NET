using Game.Shared;

using Source;
using Source.Common;

namespace Game.Server;
using FIELD = FIELD<InfoOverlayAccessor>;
[NetworkName("CInfoOverlayAccessor")]
public partial class InfoOverlayAccessor : BaseEntity
{
	public static readonly SendTable DT_InfoOverlayAccessor = new([
		SendPropInt(BaseEntity.NetworkVarFields.TextureFrameIndex, 8, PropFlags.Unsigned),
		SendPropInt(NetworkVarFields.OverlayID, 32, PropFlags.Unsigned)
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_InfoOverlayAccessor);

	[NetworkName("m_iOverlayID")]
	[NetworkVar] public partial int OverlayID { get; set; }
}
