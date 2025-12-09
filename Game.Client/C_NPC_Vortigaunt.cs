using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_NPC_Vortigaunt>;
public class C_NPC_Vortigaunt : C_AI_BaseNPC
{
	public static readonly RecvTable DT_NPC_Vortigaunt = new(DT_AI_BaseNPC, [
		RecvPropFloat(FIELD.OF(nameof(BlueEndFadeTime))),
		RecvPropBool(FIELD.OF(nameof(IsBlue))),
		RecvPropBool(FIELD.OF(nameof(IsBlack))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass("NPC_Vortigaunt", DT_NPC_Vortigaunt).WithManualClassID(StaticClassIndices.CNPC_Vortigaunt);

	public float BlueEndFadeTime;
	public bool IsBlue;
	public bool IsBlack;
}
