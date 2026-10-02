using Game.Shared;
using Source.Common;

namespace Game.Server.HL2;

[LinkEntityToClass("weapon_alyxgun")]
[NetworkName("CWeaponAlyxGun")]
public class WeaponAlyxGun : HLSelectFireMachineGun
{
	public static readonly SendTable DT_WeaponAlyxGun = new(DT_HLSelectFireMachineGun, []);
	public static new readonly ServerClass ServerClass = new ServerClass(DT_WeaponAlyxGun);
}
