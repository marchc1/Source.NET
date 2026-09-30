using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<NPC_RollerMine>;
[NetworkName("CNPC_RollerMine")]
public partial class NPC_RollerMine : AI_BaseNPC
{
	public static readonly SendTable DT_RollerMine = new(DT_AI_BaseNPC, [
		SendPropBool(NetworkVarFields.IsOpen),
		SendPropFloat(NetworkVarFields.ActiveTime, 0, PropFlags.NoScale),
		SendPropBool(NetworkVarFields.HackedByAlyx),
		SendPropBool(NetworkVarFields.PowerDown),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_RollerMine);

	[NetworkName("m_bIsOpen")]
	[NetworkVar] public partial bool IsOpen { get; set; }
	[NetworkName("m_flActiveTime")]
	[NetworkVar] public partial float ActiveTime { get; set; }
	[NetworkName("m_bHackedByAlyx")]
	[NetworkVar] public partial bool HackedByAlyx { get; set; }
	[NetworkName("m_bPowerDown")]
	[NetworkVar] public partial bool PowerDown { get; set; }
}
