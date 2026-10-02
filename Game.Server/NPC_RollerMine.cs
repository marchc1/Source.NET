using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<NPC_RollerMine>;
[LinkEntityToClass("npc_rollermine")]
[NetworkName("CNPC_RollerMine")]
public class NPC_RollerMine : AI_BaseNPC
{
	public static readonly SendTable DT_RollerMine = new(DT_AI_BaseNPC, [
		SendPropBool(FIELD.OF(nameof(IsOpen))),
		SendPropFloat(FIELD.OF(nameof(ActiveTime)), 0, PropFlags.NoScale),
		SendPropBool(FIELD.OF(nameof(HackedByAlyx))),
		SendPropBool(FIELD.OF(nameof(PowerDown))),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_RollerMine);

	[NetworkName("m_bIsOpen")]
	public bool IsOpen;
	[NetworkName("m_flActiveTime")]
	public float ActiveTime;
	[NetworkName("m_bHackedByAlyx")]
	public bool HackedByAlyx;
	[NetworkName("m_bPowerDown")]
	public bool PowerDown;
}
