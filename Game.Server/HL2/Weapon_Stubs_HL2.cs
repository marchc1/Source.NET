using Game.Shared.HL2;

using Source.Common;

namespace Game.Server.HL2;

public class WeaponCycler : BaseCombatWeapon
{
	public static readonly SendTable DT_WeaponCycler = new(DT_BaseCombatWeapon, []);
	public static new readonly ServerClass ServerClass = new ServerClass("WeaponCycler", DT_WeaponCycler).WithManualClassID(Shared.StaticClassIndices.CWeaponCycler);
}

public class WeaponCubemap : BaseCombatWeapon
{
	public static readonly SendTable DT_WeaponCubemap = new(DT_BaseCombatWeapon, []);
	public static new readonly ServerClass ServerClass = new ServerClass("WeaponCubemap", DT_WeaponCubemap).WithManualClassID(Shared.StaticClassIndices.CWeaponCubemap);
}

public class WeaponCitizenPackage : BaseHLCombatWeapon
{
	public static readonly SendTable DT_WeaponCitizenPackage = new(DT_BaseHLCombatWeapon, []);
	public static new readonly ServerClass ServerClass = new ServerClass("WeaponCitizenPackage", DT_WeaponCitizenPackage).WithManualClassID(Shared.StaticClassIndices.CWeaponCitizenPackage);
}

public class WeaponCitizenSuitcase : WeaponCitizenPackage
{
	public static readonly SendTable DT_WeaponCitizenSuitcase = new(DT_WeaponCitizenPackage, []);
	public static new readonly ServerClass ServerClass = new ServerClass("WeaponCitizenSuitcase", DT_WeaponCitizenSuitcase).WithManualClassID(Shared.StaticClassIndices.CWeaponCitizenSuitcase);
}

public class WeaponOldManHarpoon : WeaponCitizenPackage
{
	public static readonly SendTable DT_WeaponOldManHarpoon = new(DT_WeaponCitizenPackage, []);
	public static new readonly ServerClass ServerClass = new ServerClass("WeaponOldManHarpoon", DT_WeaponOldManHarpoon).WithManualClassID(Shared.StaticClassIndices.CWeaponOldManHarpoon);
}
