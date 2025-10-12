#if CLIENT_DLL || GAME_DLL
using Source.Common;

namespace Game.Shared.HL2;

public class BaseHLCombatWeapon : BaseCombatWeapon
{
	public static readonly
#if CLIENT_DLL
		RecvTable
#else
		SendTable
#endif
		DT_BaseHLCombatWeapon = new(DT_BaseCombatWeapon, []);
#if CLIENT_DLL
	public static readonly new ClientClass ClientClass = new ClientClass("BaseHLCombatWeapon", null, null, DT_BaseHLCombatWeapon).WithManualClassID(StaticClassIndices.CBaseHLCombatWeapon);
#else
	public static readonly new ServerClass ServerClass = new ServerClass("BaseHLCombatWeapon", DT_BaseHLCombatWeapon).WithManualClassID(StaticClassIndices.CBaseHLCombatWeapon);
#endif
}
#endif
