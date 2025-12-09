using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<TEKillPlayerAttachments>;
public class TEKillPlayerAttachments : BaseTempEntity
{
	public static readonly SendTable DT_TEKillPlayerAttachments = new(DT_BaseTempEntity, [
		SendPropInt(FIELD.OF(nameof(Player)), 5, PropFlags.Unsigned),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass("TEKillPlayerAttachments", DT_TEKillPlayerAttachments).WithManualClassID(StaticClassIndices.CTEKillPlayerAttachments);

	public int Player;
}
