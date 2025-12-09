using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;

using FIELD = FIELD<C_NPC_Barney>;
public class C_NPC_Barney : C_AI_BaseNPC
{
	public static readonly RecvTable DT_NPC_Barney = new(DT_AI_BaseNPC, []);
	public static readonly new ClientClass ClientClass = new ClientClass("NPC_Barney", DT_NPC_Barney).WithManualClassID(StaticClassIndices.CNPC_Barney);
}
