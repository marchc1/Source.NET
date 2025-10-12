using Source.Common;
using Game.Shared;

namespace Game.Client.HL1;

public class C_BaseHL1CombatWeapon : C_BaseCombatWeapon
{
	public static readonly RecvTable DT_BaseHL1CombatWeapon = new(DT_BaseCombatWeapon, []);
	public static readonly new ClientClass ClientClass = new ClientClass("BaseHL1CombatWeapon", DT_BaseHL1CombatWeapon).WithManualClassID(StaticClassIndices.CBaseHL1CombatWeapon);
}
