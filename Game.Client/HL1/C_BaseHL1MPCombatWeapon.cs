using Source.Common;
using Game.Shared;

namespace Game.Client.HL1;

public class C_BaseHL1MPCombatWeapon : C_BaseHL1CombatWeapon
{
	public static readonly RecvTable DT_BaseHL1MPCombatWeapon = new(DT_BaseHL1CombatWeapon, []);
	public static readonly new ClientClass ClientClass = new ClientClass("BaseHL1MPCombatWeapon", DT_BaseHL1MPCombatWeapon).WithManualClassID(StaticClassIndices.CBaseHL1MPCombatWeapon);
}
