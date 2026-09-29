using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_NPC_RollerMine>;
[NetworkName("CNPC_RollerMine")]
public class C_NPC_RollerMine : C_AI_BaseNPC
{
	public static readonly RecvTable DT_RollerMine = new(DT_AI_BaseNPC, [
		RecvPropBool(FIELD.OF(nameof(IsOpen))),
		RecvPropFloat(FIELD.OF(nameof(ActiveTime))),
		RecvPropBool(FIELD.OF(nameof(HackedByAlyx))),
		RecvPropBool(FIELD.OF(nameof(PowerDown))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass(DT_RollerMine);

	[NetworkName("m_bIsOpen")]
	public bool IsOpen;
	[NetworkName("m_flActiveTime")]
	public float ActiveTime;
	[NetworkName("m_bHackedByAlyx")]
	public bool HackedByAlyx;
	[NetworkName("m_bPowerDown")]
	public bool PowerDown;
}
