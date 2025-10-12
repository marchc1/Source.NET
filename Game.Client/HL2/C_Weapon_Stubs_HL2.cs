using Game.Client.HL2;

using Source.Common;
using Source.Engine;

namespace Game.Client.HL2;
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
