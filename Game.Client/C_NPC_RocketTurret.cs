using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_NPC_RocketTurret>;
[NetworkName("CNPC_RocketTurret")]
public class C_NPC_RocketTurret : C_AI_BaseNPC
{
	public static readonly RecvTable DT_NPC_RocketTurret = new(DT_AI_BaseNPC, [
		RecvPropInt(FIELD.OF(nameof(LaserState))),
		RecvPropInt(FIELD.OF(nameof(SiteHalo))),
		RecvPropVector(FIELD.OF(nameof(CurrentAngles))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass(DT_NPC_RocketTurret);

	[NetworkName("m_iLaserState")]
	public int LaserState;
	[NetworkName("m_nSiteHalo")]
	public int SiteHalo;
	[NetworkName("m_vecCurrentAngles")]
	public Vector3 CurrentAngles;
}
