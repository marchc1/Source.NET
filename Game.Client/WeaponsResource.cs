global using static Game.Client.WeaponsResource;

using Game.Client.HUD;
using Game.Shared;

using Source;

namespace Game.Client;

public class WeaponsResource
{
	public static readonly WeaponsResource gWR = new();

	public void LoadAllWeaponSprites() {
		C_BasePlayer? player = C_BasePlayer.GetLocalPlayer();
		if (player == null)
			return;

		for (int i = 0; i < MAX_WEAPONS; i++)
			if (player.GetWeapon(i) != null)
				LoadWeaponSprites(player.GetWeapon(i)!.GetWeaponFileInfoHandle());
	}

	public static HudTexture? FindHudTextureInDict(HudTextureDict list, ReadOnlySpan<char> psz) {
		if (list.TryGetValue(psz.Hash(false), out HudTexture? tex))
			return tex;

		return null;
	}

	internal void LoadWeaponSprites(WEAPON_FILE_INFO_HANDLE weaponFileInfo) {
		FileWeaponInfo? weaponInfo = WeaponParse.GetFileWeaponInfoFromHandle(weaponFileInfo);

		if (weaponInfo == null)
			return;

		// Already parsed the hud elements?
		if (weaponInfo.LoadedHudElements)
			return;

		weaponInfo.LoadedHudElements = true;

		weaponInfo.IconActive = null!;
		weaponInfo.IconInactive = null!;
		weaponInfo.IconAmmo = null!;
		weaponInfo.IconAmmo2 = null!;
		weaponInfo.IconCrosshair = null!;
		weaponInfo.IconAutoaim = null!;
		weaponInfo.IconSmall = null!;

		Span<char> sz = stackalloc char[128];
		sprintf(sz, "scripts/%s.txt").S(weaponInfo.ClassName);

		HudTextureDict tempList = new();

		Hud.LoadHudTextures(tempList, sz.SliceNullTerminatedString());

		if (tempList.Count == 0) {
			// no sprite description file for weapon, use default small blocks
			weaponInfo.IconActive = gHUD.GetIcon("selection")!;
			weaponInfo.IconInactive = gHUD.GetIcon("selection")!;
			weaponInfo.IconAmmo = gHUD.GetIcon("bucket1")!;
			return;
		}

		HudTexture? p;

		p = FindHudTextureInDict(tempList, "crosshair");
		if (p != null) {
			weaponInfo.IconCrosshair = gHUD.AddUnsearchableHudIconToList(p);
		}

		p = FindHudTextureInDict(tempList, "autoaim");
		if (p != null) {
			weaponInfo.IconAutoaim = gHUD.AddUnsearchableHudIconToList(p);
		}

		p = FindHudTextureInDict(tempList, "zoom");
		if (p != null) {
			weaponInfo.IconZoomedCrosshair = gHUD.AddUnsearchableHudIconToList(p);
		}
		else {
			weaponInfo.IconZoomedCrosshair = weaponInfo.IconCrosshair; //default to non-zoomed crosshair
		}

		p = FindHudTextureInDict(tempList, "zoom_autoaim");
		if (p != null) {
			weaponInfo.IconZoomedAutoaim = gHUD.AddUnsearchableHudIconToList(p);
		}
		else {
			weaponInfo.IconZoomedAutoaim = weaponInfo.IconZoomedCrosshair;  //default to zoomed crosshair
		}

		// HudHistoryResource? pHudHR = GET_HUDELEMENT<HudHistoryResource>();
		object? pHudHR = null; // ^^ Todo, when HudHistoryResource is available...
		if (pHudHR != null) {
			p = FindHudTextureInDict(tempList, "weapon");
			if (p != null) {
				weaponInfo.IconInactive = gHUD.AddUnsearchableHudIconToList(p);
				if (weaponInfo.IconInactive != null) {
					weaponInfo.IconInactive.Precache();
					// pHudHR.SetHistoryGap(weaponInfo.IconInactive.Height());
				}
			}

			p = FindHudTextureInDict(tempList, "weapon_s");
			if (p != null) {
				weaponInfo.IconActive = gHUD.AddUnsearchableHudIconToList(p);
				if (weaponInfo.IconActive != null) {
					weaponInfo.IconActive.Precache();
				}
			}

			p = FindHudTextureInDict(tempList, "weapon_small");
			if (p != null) {
				weaponInfo.IconSmall = gHUD.AddUnsearchableHudIconToList(p);
				if (weaponInfo.IconSmall != null) {
					weaponInfo.IconSmall.Precache();
				}
			}

			p = FindHudTextureInDict(tempList, "ammo");
			if (p != null) {
				weaponInfo.IconAmmo = gHUD.AddUnsearchableHudIconToList(p);
				if (weaponInfo.IconAmmo != null) {
					weaponInfo.IconAmmo.Precache();
					// pHudHR.SetHistoryGap(weaponInfo.IconAmmo.Height());
				}
			}

			p = FindHudTextureInDict(tempList, "ammo2");
			if (p != null) {
				weaponInfo.IconAmmo2 = gHUD.AddUnsearchableHudIconToList(p);
				if (weaponInfo.IconAmmo2 != null) {
					weaponInfo.IconAmmo2.Precache();
					// pHudHR.SetHistoryGap(weaponInfo.IconAmmo2.Height());
				}
			}
		}
	}

	public void Init() => Reset();
	public void Reset() { }

	public HudTexture? GetAmmoIconFromWeapon(int ammoId) {
		BasePlayer? player = BasePlayer.GetLocalPlayer();
		if (player == null)
			return null!;

		for (int i = 0; i < MAX_WEAPONS; i++) {
			BaseCombatWeapon? weapon = player.GetWeapon(i);
			if (weapon == null)
				continue;

			if (weapon.PrimaryAmmoType == ammoId)
				return weapon.GetWpnData().IconAmmo;

			if (weapon.SecondaryAmmoType == ammoId)
				return weapon.GetWpnData().IconAmmo2;
		}

		return null!;
	}

	public FileWeaponInfo? GetWeaponFromAmmo(int ammoId) {
		C_BasePlayer? player = C_BasePlayer.GetLocalPlayer();
		if (player == null)
			return null;

		for (int i = 0; i < MAX_WEAPONS; i++) {
			C_BaseCombatWeapon? weapon = player.GetWeapon(i);
			if (weapon == null)
				continue;

			if (weapon.GetPrimaryAmmoType() == ammoId) 
				return weapon.GetWpnData();
			else if (weapon.GetSecondaryAmmoType() == ammoId) 
				return weapon.GetWpnData();
		}

		return null;
	}

	// FileWeaponInfo GetWeaponFromAmmo(int iAmmoId) { }
}
