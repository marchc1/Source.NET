using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<NPC_RocketTurret>;
[NetworkName("CNPC_RocketTurret")]
public partial class NPC_RocketTurret : AI_BaseNPC
{
	public static readonly SendTable DT_NPC_RocketTurret = new(DT_AI_BaseNPC, [
		SendPropInt(NetworkVarFields.LaserState, 2, 0),
		SendPropInt(NetworkVarFields.SiteHalo, 16, PropFlags.Unsigned),
		SendPropVector(NetworkVarFields.CurrentAngles, 0, PropFlags.NoScale),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_NPC_RocketTurret);

	[NetworkName("m_iLaserState")]
	[NetworkVar] public partial int LaserState { get; set; }
	[NetworkName("m_nSiteHalo")]
	[NetworkVar] public partial int SiteHalo { get; set; }
	[NetworkName("m_vecCurrentAngles")]
	[NetworkVar] public partial Vector3 CurrentAngles { get; set; }
}
