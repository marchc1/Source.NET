using Source.Common;
using Game.Shared;

namespace Game.Server.HL1;

[NetworkName("CBaseHL1CombatWeapon")]
public class BaseHL1CombatWeapon : BaseCombatWeapon
{
	public static readonly SendTable DT_BaseHL1CombatWeapon = new(DT_BaseCombatWeapon, []);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_BaseHL1CombatWeapon);
}
