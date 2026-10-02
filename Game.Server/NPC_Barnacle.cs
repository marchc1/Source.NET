using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<NPC_Barnacle>;
[LinkEntityToClass("npc_barnacle")]
[NetworkName("CNPC_Barnacle")]
public class NPC_Barnacle : AI_BaseNPC
{
	public static readonly SendTable DT_Barnacle = new(DT_AI_BaseNPC, [
		SendPropFloat(FIELD.OF(nameof(Altitude)), 0, PropFlags.NoScale),
		SendPropVector(FIELD.OF(nameof(Root)), 0, PropFlags.Coord),
		SendPropVector(FIELD.OF(nameof(Tip)), 0, PropFlags.Coord),
		SendPropVector(FIELD.OF(nameof(TipDrawOffset)), 0, PropFlags.NoScale),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_Barnacle);

	[NetworkName("m_flAltitude")]
	public float Altitude;
	[NetworkName("m_vecRoot")]
	public Vector3 Root;
	[NetworkName("m_vecTip")]
	public Vector3 Tip;
	[NetworkName("m_vecTipDrawOffset")]
	public Vector3 TipDrawOffset;
}
