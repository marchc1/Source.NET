using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<NPC_RocketTurret>;
public class NPC_RocketTurret : AI_BaseNPC
{
	public static readonly SendTable DT_NPC_RocketTurret = new(DT_AI_BaseNPC, [
		SendPropInt(FIELD.OF(nameof(LaserState)), 2, 0),
		SendPropInt(FIELD.OF(nameof(SiteHalo)), 32, 0),
		SendPropVector(FIELD.OF(nameof(CurrentAngles)), 0, PropFlags.NoScale),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass("NPC_RocketTurret", DT_NPC_RocketTurret).WithManualClassID(StaticClassIndices.CNPC_RocketTurret);

	public int LaserState;
	public int SiteHalo;
	public Vector3 CurrentAngles;
}
