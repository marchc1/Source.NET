using Game.Shared;

using Source.Common;

namespace Game.Client;
using FIELD = Source.FIELD<C_InfoOverlayAccessor>;

[NetworkName("CInfoOverlayAccessor")]
public partial class C_InfoOverlayAccessor : C_BaseEntity
{
	public static readonly RecvTable DT_InfoOverlayAccessor = new( [
		RecvPropInt(FIELD.OF(nameof(TextureFrameIndex))),
		RecvPropInt(FIELD.OF(nameof(OverlayID)))
	]);
	public static readonly new ClientClass ClientClass = new ClientClass(DT_InfoOverlayAccessor);

	[NetworkName("m_iOverlayID")]
	public int OverlayID;
}
