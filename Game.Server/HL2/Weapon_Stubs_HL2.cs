using Game.Shared;
using Game.Shared.HL2;

using Source.Common;

namespace Game.Server.HL2;

[LinkEntityToClass("cycler_weapon")]
[NetworkName("CWeaponCycler")]
public class WeaponCycler : BaseCombatWeapon
{
	public static readonly SendTable DT_WeaponCycler = new(DT_BaseCombatWeapon, []);
	public static new readonly ServerClass ServerClass = new ServerClass(DT_WeaponCycler);
}

[LinkEntityToClass("weapon_cubemap")]
[NetworkName("CWeaponCubemap")]
public class WeaponCubemap : BaseCombatWeapon
{
	public static readonly SendTable DT_WeaponCubemap = new(DT_BaseCombatWeapon, []);
	public static new readonly ServerClass ServerClass = new ServerClass(DT_WeaponCubemap);
}

[LinkEntityToClass("weapon_citizenpackage")]
[NetworkName("CWeaponCitizenPackage")]
public class WeaponCitizenPackage : BaseHLCombatWeapon
{
	public static readonly SendTable DT_WeaponCitizenPackage = new(DT_BaseHLCombatWeapon, []);
	public static new readonly ServerClass ServerClass = new ServerClass(DT_WeaponCitizenPackage);
}

[LinkEntityToClass("weapon_citizensuitcase")]
[NetworkName("CWeaponCitizenSuitcase")]
public class WeaponCitizenSuitcase : WeaponCitizenPackage
{
	public static readonly SendTable DT_WeaponCitizenSuitcase = new(DT_WeaponCitizenPackage, []);
	public static new readonly ServerClass ServerClass = new ServerClass(DT_WeaponCitizenSuitcase);
}

[LinkEntityToClass("weapon_oldmanharpoon")]
[NetworkName("CWeaponOldManHarpoon")]
public class WeaponOldManHarpoon : WeaponCitizenPackage
{
	public static readonly SendTable DT_WeaponOldManHarpoon = new(DT_WeaponCitizenPackage, []);
	public static new readonly ServerClass ServerClass = new ServerClass(DT_WeaponOldManHarpoon);
}
