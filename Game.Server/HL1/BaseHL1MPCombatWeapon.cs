using Source.Common;
using Game.Shared;

namespace Game.Server.HL1;

public class BaseHL1MPCombatWeapon : BaseHL1CombatWeapon
{
	public static readonly SendTable DT_BaseHL1MPCombatWeapon = new(DT_BaseHL1CombatWeapon, []);
	public static readonly new ServerClass ServerClass = new ServerClass("BaseHL1MPCombatWeapon", DT_BaseHL1MPCombatWeapon).WithManualClassID(StaticClassIndices.CBaseHL1MPCombatWeapon);
}
