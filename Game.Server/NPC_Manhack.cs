using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<NPC_Manhack>;
[NetworkName("CNPC_Manhack")]
public partial class NPC_Manhack : AI_BaseNPC
{
	public static readonly SendTable DT_NPC_Manhack = new(DT_AI_BaseNPC, [
		SendPropInt(NetworkVarFields.EnginePitch1, 8, PropFlags.Unsigned),
		SendPropFloat(NetworkVarFields.EnginePitch1Time, 0, PropFlags.NoScale),
		SendPropInt(NetworkVarFields.EnginePitch2, 8, PropFlags.Unsigned),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_NPC_Manhack);

	[NetworkName("m_nEnginePitch1")]
	[NetworkVar] public partial int EnginePitch1 { get; set; }
	[NetworkName("m_flEnginePitch1Time")]
	[NetworkVar] public partial float EnginePitch1Time { get; set; }
	[NetworkName("m_nEnginePitch2")]
	[NetworkVar] public partial int EnginePitch2 { get; set; }
}
