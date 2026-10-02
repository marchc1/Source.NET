using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<NPC_RocketTurret>;
[LinkEntityToClass("npc_rocket_turret")]
[NetworkName("CNPC_RocketTurret")]
public class NPC_RocketTurret : AI_BaseNPC
{
	public static readonly SendTable DT_NPC_RocketTurret = new(DT_AI_BaseNPC, [
		SendPropInt(FIELD.OF(nameof(LaserState)), 2, 0),
		SendPropInt(FIELD.OF(nameof(SiteHalo)), 16, PropFlags.Unsigned),
		SendPropVector(FIELD.OF(nameof(CurrentAngles)), 0, PropFlags.NoScale),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_NPC_RocketTurret);

	[NetworkName("m_iLaserState")]
	public int LaserState;
	[NetworkName("m_nSiteHalo")]
	public int SiteHalo;
	[NetworkName("m_vecCurrentAngles")]
	public Vector3 CurrentAngles;
}
