#if CLIENT_DLL || GAME_DLL

using Source.Common;
using Game.Shared;

#if CLIENT_DLL
namespace Game.Client;
#else
namespace Game.Server;
#endif

public partial class
#if CLIENT_DLL
		C_BaseCombatCharacter
#elif GAME_DLL
	BaseCombatCharacter
#endif
{
	public int GetAmmoCount(int ammoIndex) {
		if (ammoIndex == -1)
			return 0;

		// TODO, it is 4 am, I do not want to do this right now
		// FIXME FIX ME FIX ME
#if CLIENT_DLL
		return Ammo[ammoIndex];
#else
		return 0;
#endif
	}

	public virtual bool Weapon_CanSwitchTo(BaseCombatWeapon weapon) {
		if (IsPlayer()) {
			BasePlayer player = (BasePlayer)this!;
#if !CLIENT_DLL
			IServerVehicle? vehicle = player.GetVehicle();
#else
			IClientVehicle? vehicle = player.GetVehicle();
#endif

			if (vehicle != null && !player.UsingStandardWeaponsInVehicle())
				return false;
		}

		if (!weapon.HasAnyAmmo() && 0 == GetAmmoCount(weapon.PrimaryAmmoType))
			return false;

		if (!weapon.CanDeploy())
			return false;

		if (ActiveWeapon.Get() != null) {
			if (!ActiveWeapon.Get()!.CanHolster() && !weapon.ForceWeaponSwitch())
				return false;

			if (IsPlayer()) {
				BasePlayer? player = (BasePlayer)this!;
				// check if active weapon force the last weapon to switch
				if (ActiveWeapon.Get()!.ForceWeaponSwitch()) {
					// last weapon wasn't allowed to switch, don't allow to switch to new weapon
					BaseCombatWeapon? lastWeapon = player.GetLastWeapon();
					if (lastWeapon != null && weapon != lastWeapon && !lastWeapon!.CanHolster() && !weapon.ForceWeaponSwitch()) 
						return false;
				}
			}
		}

		return true;
	}
}
#endif
