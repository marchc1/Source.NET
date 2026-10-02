using Source.Common;
using Game.Shared;

namespace Game.Client.HL1;

[LinkEntityToClass("basehl1combatweapon")]
[NetworkName("CBaseHL1CombatWeapon")]
public class C_BaseHL1CombatWeapon : C_BaseCombatWeapon
{
	public static readonly RecvTable DT_BaseHL1CombatWeapon = new(DT_BaseCombatWeapon, []);
	public static readonly new ClientClass ClientClass = new ClientClass(DT_BaseHL1CombatWeapon);
}
