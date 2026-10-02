using Game.Shared;
using Game.Client.HL2;

using Source.Common;
using Source.Engine;

namespace Game.Client.HL2;

[LinkEntityToClass("cycler_weapon")]
[NetworkName("CWeaponCycler")]
public class C_WeaponCycler : C_BaseCombatWeapon
{
	public static readonly RecvTable DT_WeaponCycler = new(DT_BaseCombatWeapon, []);
	public static new readonly ClientClass ClientClass = new ClientClass(DT_WeaponCycler);
}

[LinkEntityToClass("weapon_annabelle")]
[NetworkName("CWeaponAnnabelle")]
public class C_WeaponAnnabelle : C_BaseHLCombatWeapon
{
	public static readonly RecvTable DT_WeaponAnnabelle = new(DT_BaseHLCombatWeapon, []);
	public static new readonly ClientClass ClientClass = new ClientClass(DT_WeaponAnnabelle);
}

[LinkEntityToClass("weapon_alyxgun")]
[NetworkName("CWeaponAlyxGun")]
public class C_WeaponAlyxGun : C_HLSelectFireMachineGun
{
	public static readonly RecvTable DT_WeaponAlyxGun = new(DT_HLSelectFireMachineGun, []);
	public static new readonly ClientClass ClientClass = new ClientClass(DT_WeaponAlyxGun);
}

[LinkEntityToClass("weapon_citizenpackage")]
[NetworkName("CWeaponCitizenPackage")]
public class C_WeaponCitizenPackage : C_BaseHLCombatWeapon
{
	public static readonly RecvTable DT_WeaponCitizenPackage = new(DT_BaseHLCombatWeapon, []);
	public static new readonly ClientClass ClientClass = new ClientClass(DT_WeaponCitizenPackage);
}

[LinkEntityToClass("weapon_citizensuitcase")]
[NetworkName("CWeaponCitizenSuitcase")]
public class C_WeaponCitizenSuitcase : C_WeaponCitizenPackage
{
	public static readonly RecvTable DT_WeaponCitizenSuitcase = new(DT_WeaponCitizenPackage, []);
	public static new readonly ClientClass ClientClass = new ClientClass(DT_WeaponCitizenSuitcase);
}

[LinkEntityToClass("weapon_cubemap")]
[NetworkName("CWeaponCubemap")]
public class C_WeaponCubemap : C_BaseCombatWeapon
{
	public static readonly RecvTable DT_WeaponCubemap = new(DT_BaseCombatWeapon, []);
	public static new readonly ClientClass ClientClass = new ClientClass(DT_WeaponCubemap);
}


[LinkEntityToClass("weapon_oldmanharpoon")]
[NetworkName("CWeaponOldManHarpoon")]
public class C_WeaponOldManHarpoon : C_WeaponCitizenPackage
{
	public static readonly RecvTable DT_WeaponOldManHarpoon = new(DT_WeaponCitizenPackage, []);
	public static new readonly ClientClass ClientClass = new ClientClass(DT_WeaponOldManHarpoon);
}
