using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<NPC_RollerMine>;
public class NPC_RollerMine : AI_BaseNPC
{
	public static readonly SendTable DT_NPC_RollerMine = new(DT_AI_BaseNPC, [
		SendPropBool(FIELD.OF(nameof(IsOpen))),
		SendPropFloat(FIELD.OF(nameof(ActiveTime)), 0, PropFlags.NoScale),
		SendPropBool(FIELD.OF(nameof(HackedByAlyx))),
		SendPropBool(FIELD.OF(nameof(PowerDown))),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass("NPC_RollerMine", DT_NPC_RollerMine).WithManualClassID(StaticClassIndices.CNPC_RollerMine);

	public bool IsOpen;
	public float ActiveTime;
	public bool HackedByAlyx;
	public bool PowerDown;
}
