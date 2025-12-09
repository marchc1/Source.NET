using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_NPC_RollerMine>;
public class C_NPC_RollerMine : C_AI_BaseNPC
{
	public static readonly RecvTable DT_NPC_RollerMine = new(DT_AI_BaseNPC, [
		RecvPropBool(FIELD.OF(nameof(IsOpen))),
		RecvPropFloat(FIELD.OF(nameof(ActiveTime))),
		RecvPropBool(FIELD.OF(nameof(HackedByAlyx))),
		RecvPropBool(FIELD.OF(nameof(PowerDown))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass("NPC_RollerMine", DT_NPC_RollerMine).WithManualClassID(StaticClassIndices.CNPC_RollerMine);

	public bool IsOpen;
	public float ActiveTime;
	public bool HackedByAlyx;
	public bool PowerDown;
}
