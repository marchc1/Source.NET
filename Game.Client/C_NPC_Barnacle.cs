using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_NPC_Barnacle>;
public class C_NPC_Barnacle : C_AI_BaseNPC
{
	public static readonly RecvTable DT_NPC_Barnacle = new(DT_AI_BaseNPC, [
		RecvPropFloat(FIELD.OF(nameof(Altitude))),
		RecvPropVector(FIELD.OF(nameof(Root))),
		RecvPropVector(FIELD.OF(nameof(Tip))),
		RecvPropVector(FIELD.OF(nameof(TipDrawOffset))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass("NPC_Barnacle", DT_NPC_Barnacle).WithManualClassID(StaticClassIndices.CNPC_Barnacle);

	public float Altitude;
	public Vector3 Root;
	public Vector3 Tip;
	public Vector3 TipDrawOffset;
}
