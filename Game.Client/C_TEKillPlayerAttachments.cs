using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_TEKillPlayerAttachments>;
public class C_TEKillPlayerAttachments : C_BaseTempEntity
{
	public static readonly RecvTable DT_TEKillPlayerAttachments = new(DT_BaseTempEntity, [
		RecvPropInt(FIELD.OF(nameof(Player))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass("TEKillPlayerAttachments", DT_TEKillPlayerAttachments).WithManualClassID(StaticClassIndices.CTEKillPlayerAttachments);

	public int Player;
}
