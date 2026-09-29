using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_NPC_Barnacle>;
[NetworkName("CNPC_Barnacle")]
public class C_NPC_Barnacle : C_AI_BaseNPC
{
	public static readonly RecvTable DT_Barnacle = new(DT_AI_BaseNPC, [
		RecvPropFloat(FIELD.OF(nameof(Altitude))),
		RecvPropVector(FIELD.OF(nameof(Root))),
		RecvPropVector(FIELD.OF(nameof(Tip))),
		RecvPropVector(FIELD.OF(nameof(TipDrawOffset))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass(DT_Barnacle);

	[NetworkName("m_flAltitude")]
	public float Altitude;
	[NetworkName("m_vecRoot")]
	public Vector3 Root;
	[NetworkName("m_vecTip")]
	public Vector3 Tip;
	[NetworkName("m_vecTipDrawOffset")]
	public Vector3 TipDrawOffset;
}
