using Source.Common;
using Source;
using Game.Shared.HL2;

namespace Game.Server;

public class BaseHLBludgeonWeapon : BaseHLCombatWeapon
{
	public static readonly SendTable DT_BaseHLBludgeonWeapon = new(DT_BaseHLCombatWeapon, [

	]);
	public static new readonly ServerClass ServerClass = new ServerClass("BaseHLBludgeonWeapon", DT_BaseHLBludgeonWeapon).WithManualClassID(Shared.StaticClassIndices.CBaseHLBludgeonWeapon);
}
