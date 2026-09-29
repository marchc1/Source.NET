using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_NPC_Manhack>;
[NetworkName("CNPC_Manhack")]
public class C_NPC_Manhack : C_AI_BaseNPC
{
	public static readonly RecvTable DT_NPC_Manhack = new(DT_AI_BaseNPC, [
		RecvPropInt(FIELD.OF(nameof(EnginePitch1))),
		RecvPropFloat(FIELD.OF(nameof(EnginePitch1Time))),
		RecvPropInt(FIELD.OF(nameof(EnginePitch2))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass(DT_NPC_Manhack);

	[NetworkName("m_nEnginePitch1")]
	public int EnginePitch1;
	[NetworkName("m_flEnginePitch1Time")]
	public float EnginePitch1Time;
	[NetworkName("m_nEnginePitch2")]
	public int EnginePitch2;
}
