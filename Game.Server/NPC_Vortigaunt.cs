using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<NPC_Vortigaunt>;
[LinkEntityToClass("npc_vortigaunt")]
[NetworkName("CNPC_Vortigaunt")]
public class NPC_Vortigaunt : AI_BaseNPC
{
	public static readonly SendTable DT_NPC_Vortigaunt = new(DT_AI_BaseNPC, [
		SendPropFloat(FIELD.OF(nameof(BlueEndFadeTime)), 0, PropFlags.NoScale),
		SendPropBool(FIELD.OF(nameof(IsBlue))),
		SendPropBool(FIELD.OF(nameof(IsBlack))),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_NPC_Vortigaunt);

	[NetworkName("m_flBlueEndFadeTime")]
	public float BlueEndFadeTime;
	[NetworkName("m_bIsBlue")]
	public bool IsBlue;
	[NetworkName("m_bIsBlack")]
	public bool IsBlack;
}
