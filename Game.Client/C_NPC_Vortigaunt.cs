using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_NPC_Vortigaunt>;
[NetworkName("CNPC_Vortigaunt")]
public class C_NPC_Vortigaunt : C_AI_BaseNPC
{
	public static readonly RecvTable DT_NPC_Vortigaunt = new(DT_AI_BaseNPC, [
		RecvPropFloat(FIELD.OF(nameof(BlueEndFadeTime))),
		RecvPropBool(FIELD.OF(nameof(IsBlue))),
		RecvPropBool(FIELD.OF(nameof(IsBlack))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass(DT_NPC_Vortigaunt);

	[NetworkName("m_flBlueEndFadeTime")]
	public float BlueEndFadeTime;
	[NetworkName("m_bIsBlue")]
	public bool IsBlue;
	[NetworkName("m_bIsBlack")]
	public bool IsBlack;
}
