using Source.Common;
using Source;
using Game.Shared.HL2;

namespace Game.Server;

[NetworkName("CHLMachineGun")]
public class HLMachineGun : BaseHLCombatWeapon
{
	public static readonly SendTable DT_HLMachineGun = new(DT_BaseHLCombatWeapon, [

	]);
	public static new readonly ServerClass ServerClass = new ServerClass(DT_HLMachineGun);
}


[NetworkName("CHLSelectFireMachineGun")]
public class HLSelectFireMachineGun : HLMachineGun
{
	public static readonly SendTable DT_HLSelectFireMachineGun = new(DT_HLMachineGun, [

	]);
	public static new readonly ServerClass ServerClass = new ServerClass(DT_HLSelectFireMachineGun);
}
