using Source.Common;

namespace Game.Server.HL2;

public class WeaponAlyxGun : HLSelectFireMachineGun
{
	public static readonly SendTable DT_WeaponAlyxGun = new(DT_HLSelectFireMachineGun, []);
	public static new readonly ServerClass ServerClass = new ServerClass("WeaponAlyxGun", DT_WeaponAlyxGun).WithManualClassID(Shared.StaticClassIndices.CWeaponAlyxGun);
}
