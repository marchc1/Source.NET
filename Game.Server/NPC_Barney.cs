using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;

using FIELD = FIELD<NPC_Barney>;
[LinkEntityToClass("npc_barney")]
[NetworkName("CNPC_Barney")]
public class NPC_Barney : AI_BaseNPC
{
	public static readonly SendTable DT_NPC_Barney = new(DT_AI_BaseNPC, []);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_NPC_Barney);
}
