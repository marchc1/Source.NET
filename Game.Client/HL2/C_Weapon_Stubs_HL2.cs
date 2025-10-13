using Game.Client.HL2;

using Source.Common;
using Source.Engine;

namespace Game.Client.HL2;

public class C_WeaponCycler : C_BaseCombatWeapon
{
	public static readonly RecvTable DT_WeaponCycler = new(DT_BaseCombatWeapon, []);
	public static new readonly ClientClass ClientClass = new ClientClass("WeaponCycler", DT_WeaponCycler).WithManualClassID(Shared.StaticClassIndices.CWeaponCycler);
}

public class C_WeaponAnnabelle : C_BaseHLCombatWeapon
{
	public static readonly RecvTable DT_WeaponAnnabelle = new(DT_BaseHLCombatWeapon, []);
	public static new readonly ClientClass ClientClass = new ClientClass("WeaponAnnabelle", DT_WeaponAnnabelle).WithManualClassID(Shared.StaticClassIndices.CWeaponAnnabelle);
}

public class C_WeaponAlyxGun : C_HLSelectFireMachineGun
{
	public static readonly RecvTable DT_WeaponAlyxGun = new(DT_HLSelectFireMachineGun, []);
	public static new readonly ClientClass ClientClass = new ClientClass("WeaponAlyxGun", DT_WeaponAlyxGun).WithManualClassID(Shared.StaticClassIndices.CWeaponAlyxGun);
}

public class C_WeaponCitizenPackage : C_BaseHLCombatWeapon
{
	public static readonly RecvTable DT_WeaponCitizenPackage = new(DT_BaseHLCombatWeapon, []);
	public static new readonly ClientClass ClientClass = new ClientClass("WeaponCitizenPackage", DT_WeaponCitizenPackage).WithManualClassID(Shared.StaticClassIndices.CWeaponCitizenPackage);
}

public class C_WeaponCitizenSuitcase : C_WeaponCitizenPackage
{
	public static readonly RecvTable DT_WeaponCitizenSuitcase = new(DT_WeaponCitizenPackage, []);
	public static new readonly ClientClass ClientClass = new ClientClass("WeaponCitizenSuitcase", DT_WeaponCitizenSuitcase).WithManualClassID(Shared.StaticClassIndices.CWeaponCitizenSuitcase);
}

public class C_WeaponCubemap : C_BaseCombatWeapon
{
	public static readonly RecvTable DT_WeaponCubemap = new(DT_BaseCombatWeapon, []);
	public static new readonly ClientClass ClientClass = new ClientClass("WeaponCubemap", DT_WeaponCubemap).WithManualClassID(Shared.StaticClassIndices.CWeaponCubemap);
}


public class C_WeaponOldManHarpoon : C_WeaponCitizenPackage
{
	public static readonly RecvTable DT_WeaponOldManHarpoon = new(DT_WeaponCitizenPackage, []);
	public static new readonly ClientClass ClientClass = new ClientClass("WeaponOldManHarpoon", DT_WeaponOldManHarpoon).WithManualClassID(Shared.StaticClassIndices.CWeaponOldManHarpoon);
}
