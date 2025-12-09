using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<NPC_Barnacle>;
public class NPC_Barnacle : AI_BaseNPC
{
	public static readonly SendTable DT_NPC_Barnacle = new(DT_AI_BaseNPC, [
		SendPropFloat(FIELD.OF(nameof(Altitude)), 0, PropFlags.NoScale),
		SendPropVector(FIELD.OF(nameof(Root)), 0, PropFlags.Coord),
		SendPropVector(FIELD.OF(nameof(Tip)), 0, PropFlags.Coord),
		SendPropVector(FIELD.OF(nameof(TipDrawOffset)), 0, PropFlags.NoScale),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass("NPC_Barnacle", DT_NPC_Barnacle).WithManualClassID(StaticClassIndices.CNPC_Barnacle);

	public float Altitude;
	public Vector3 Root;
	public Vector3 Tip;
	public Vector3 TipDrawOffset;
}
