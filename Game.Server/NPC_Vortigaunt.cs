using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<NPC_Vortigaunt>;
[NetworkName("CNPC_Vortigaunt")]
public partial class NPC_Vortigaunt : AI_BaseNPC
{
	public static readonly SendTable DT_NPC_Vortigaunt = new(DT_AI_BaseNPC, [
		SendPropFloat(NetworkVarFields.BlueEndFadeTime, 0, PropFlags.NoScale),
		SendPropBool(NetworkVarFields.IsBlue),
		SendPropBool(NetworkVarFields.IsBlack),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_NPC_Vortigaunt);

	[NetworkName("m_flBlueEndFadeTime")]
	[NetworkVar] public partial float BlueEndFadeTime { get; set; }
	[NetworkName("m_bIsBlue")]
	[NetworkVar] public partial bool IsBlue { get; set; }
	[NetworkName("m_bIsBlack")]
	[NetworkVar] public partial bool IsBlack { get; set; }
}
