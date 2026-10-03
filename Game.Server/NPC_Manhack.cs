using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<NPC_Manhack>;
[LinkEntityToClass("npc_manhack")]
[NetworkName("CNPC_Manhack")]
public class NPC_Manhack : AI_BaseNPC
{
	public static readonly SendTable DT_NPC_Manhack = new(DT_AI_BaseNPC, [
		SendPropInt(FIELD.OF(nameof(EnginePitch1)), 8, PropFlags.Unsigned),
		SendPropFloat(FIELD.OF(nameof(EnginePitch1Time)), 0, PropFlags.NoScale),
		SendPropInt(FIELD.OF(nameof(EnginePitch2)), 8, PropFlags.Unsigned),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_NPC_Manhack);

	[NetworkName("m_nEnginePitch1")]
	public int EnginePitch1;
	[NetworkName("m_flEnginePitch1Time")]
	public float EnginePitch1Time;
	[NetworkName("m_nEnginePitch2")]
	public int EnginePitch2;
}
