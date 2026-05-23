#if (CLIENT_DLL || GAME_DLL) && GMOD_DLL

using Source.Common;
namespace Game.Shared.GarrysMod;
using FIELD = Source.FIELD<HL2MPMachineGun>;
public class HL2MPMachineGun : WeaponHL2MPBase
{
	public static readonly
#if CLIENT_DLL
		RecvTable
#else
		SendTable
#endif
		DT_HL2MPMachineGun = new(DT_WeaponHL2MPBase, [
#if CLIENT_DLL
		RecvPropInt(FIELD.OF(nameof(ShotsFired))),
#else
		SendPropInt(FIELD.OF(nameof(ShotsFired)), 16),
#endif
		]);
#if CLIENT_DLL
	public static readonly new ClientClass ClientClass = new ClientClass("HL2MPMachineGun", null, null, DT_HL2MPMachineGun).WithManualClassID(StaticClassIndices.CHL2MPMachineGun);
#else
	public static readonly new ServerClass ServerClass = new ServerClass("HL2MPMachineGun", DT_HL2MPMachineGun).WithManualClassID(StaticClassIndices.CHL2MPMachineGun);
#endif
	public int ShotsFired;


	public override void PrimaryAttack() {
		BasePlayer? player = ToBasePlayer(GetOwner());
		if (player == null)
			return;

		// Abort here to handle burst and auto fire modes
		if ((UsesClipsForAmmo1() && iClip1 == 0) || (!UsesClipsForAmmo1() && 0 == player.GetAmmoCount(PrimaryAmmoType)))
			return;

		ShotsFired++;

		player.DoMuzzleFlash();

		// To make the firing framerate independent, we may have to fire more than one bullet here on low-framerate systems, 
		// especially if the weapon we're firing has a really fast rate of fire.
		int iBulletsToFire = 0;
		float fireRate = GetFireRate();

		while (NextPrimaryAttack <= gpGlobals.CurTime) {
			// MUST call sound before removing a round from the clip of a CHLMachineGun
			WeaponSound(Shared.WeaponSound.Single, NextPrimaryAttack);
			NextPrimaryAttack = NextPrimaryAttack + fireRate;
			iBulletsToFire++;
		}

		// Make sure we don't fire more than the amount in the clip, if this weapon uses clips
		if (UsesClipsForAmmo1()) {
			if (iBulletsToFire > iClip1)
				iBulletsToFire = iClip1;
			iClip1 -= iBulletsToFire;
		}

		HL2MP_Player hL2MPPlayer = ToHL2MPPlayer(player)!;

		// Fire the bullets
		FireBulletsInfo info = default; 
		info.Shots = iBulletsToFire;
		info.Src = hL2MPPlayer.Weapon_ShootPosition();
		info.DirShooting = player.GetAutoaimVector(0.08715574274766f);
		info.Spread = hL2MPPlayer.GetAttackSpread(this);
		info.Distance = MAX_TRACE_LENGTH;
		info.AmmoType = PrimaryAmmoType;
		info.TracerFreq = 2;
		FireBullets(info);

		//Factor in the view kick
		AddViewKick();

		if (0 == iClip1 && player.GetAmmoCount(PrimaryAmmoType) <= 0) 
			// HEV suit - indicate out of ammo condition
			player.SetSuitUpdate("!HEV_AMO0", false, 0);

		SendWeaponAnim(GetPrimaryAttackActivity());
		player.SetAnimation(PlayerAnim.Attack1);
	}
}
#endif
