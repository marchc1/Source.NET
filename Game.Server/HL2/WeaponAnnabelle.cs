using Game.Shared.HL2;

using Source.Common;

namespace Game.Server.HL2;

public class WeaponAnnabelle : BaseHLCombatWeapon
{
	public static readonly SendTable DT_WeaponAnnabelle = new(DT_BaseHLCombatWeapon, []);
	public static new readonly ServerClass ServerClass = new ServerClass("WeaponAnnabelle", DT_WeaponAnnabelle).WithManualClassID(Shared.StaticClassIndices.CWeaponAnnabelle);
}
