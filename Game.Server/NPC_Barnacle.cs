using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<NPC_Barnacle>;
[NetworkName("CNPC_Barnacle")]
public partial class NPC_Barnacle : AI_BaseNPC
{
	public static readonly SendTable DT_Barnacle = new(DT_AI_BaseNPC, [
		SendPropFloat(NetworkVarFields.Altitude, 0, PropFlags.NoScale),
		SendPropVector(NetworkVarFields.Root, 0, PropFlags.Coord),
		SendPropVector(NetworkVarFields.Tip, 0, PropFlags.Coord),
		SendPropVector(NetworkVarFields.TipDrawOffset, 0, PropFlags.NoScale),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_Barnacle);

	[NetworkName("m_flAltitude")]
	[NetworkVar] public partial float Altitude { get; set; }
	[NetworkName("m_vecRoot")]
	[NetworkVar] public partial Vector3 Root { get; set; }
	[NetworkName("m_vecTip")]
	[NetworkVar] public partial Vector3 Tip { get; set; }
	[NetworkName("m_vecTipDrawOffset")]
	[NetworkVar] public partial Vector3 TipDrawOffset { get; set; }
}
