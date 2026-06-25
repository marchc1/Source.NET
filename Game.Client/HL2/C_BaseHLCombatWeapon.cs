using Game.Server;
using Game.Shared;
using Game.Shared.HL2;

using Source;
using Source.Common;
using Source.Common.Commands;

namespace Game.Client.HL2;

public class C_HLMachineGun : BaseHLCombatWeapon
{
	public static readonly RecvTable DT_HLMachineGun = new(DT_BaseHLCombatWeapon, [

	]);
	public static readonly new ClientClass ClientClass = new ClientClass("HLMachineGun", DT_HLMachineGun).WithManualClassID(StaticClassIndices.CHLMachineGun);
}

public class C_HLSelectFireMachineGun : C_HLMachineGun
{
	public static readonly RecvTable DT_HLSelectFireMachineGun = new(DT_HLMachineGun, [

	]);
	public static readonly new ClientClass ClientClass = new ClientClass("HLSelectFireMachineGun", DT_HLSelectFireMachineGun).WithManualClassID(StaticClassIndices.CHLSelectFireMachineGun);
}

public class C_BaseHLBludgeonWeapon : BaseHLCombatWeapon
{
	public static readonly RecvTable DT_BaseHLBludgeonWeapon = new(DT_BaseHLCombatWeapon, [

	]);
	public static readonly new ClientClass ClientClass = new ClientClass("BaseHLBludgeonWeapon", DT_BaseHLBludgeonWeapon).WithManualClassID(StaticClassIndices.CBaseHLBludgeonWeapon);
}
