using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_TEKillPlayerAttachments>;
[NetworkName("CTEKillPlayerAttachments")]
public class C_TEKillPlayerAttachments : C_BaseTempEntity
{
	public static readonly RecvTable DT_TEKillPlayerAttachments = new(DT_BaseTempEntity, [
		RecvPropInt(FIELD.OF(nameof(Player))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass(DT_TEKillPlayerAttachments);

	[NetworkName("m_nPlayer")]
	public int Player;
}

public static partial class TempEnts
{
	public static void TE_KillPlayerAttachments(IRecipientFilter filter, float delay, int player) {
		throw new NotImplementedException();
	}
}
