using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<NPC_Vortigaunt>;
public class NPC_Vortigaunt : AI_BaseNPC
{
	public static readonly SendTable DT_NPC_Vortigaunt = new(DT_AI_BaseNPC, [
		SendPropFloat(FIELD.OF(nameof(BlueEndFadeTime)), 0, PropFlags.NoScale),
		SendPropBool(FIELD.OF(nameof(IsBlue))),
		SendPropBool(FIELD.OF(nameof(IsBlack))),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass("NPC_Vortigaunt", DT_NPC_Vortigaunt).WithManualClassID(StaticClassIndices.CNPC_Vortigaunt);

	public float BlueEndFadeTime;
	public bool IsBlue;
	public bool IsBlack;
}
