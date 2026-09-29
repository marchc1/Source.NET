using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<TEKillPlayerAttachments>;
[NetworkName("CTEKillPlayerAttachments")]
public class TEKillPlayerAttachments(ReadOnlySpan<char> name) : BaseTempEntity(name)
{
	public static readonly SendTable DT_TEKillPlayerAttachments = new(DT_BaseTempEntity, [
		SendPropInt(FIELD.OF(nameof(Player)), 5, PropFlags.Unsigned),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_TEKillPlayerAttachments);

	[NetworkName("m_nPlayer")]
	public int Player;
}
