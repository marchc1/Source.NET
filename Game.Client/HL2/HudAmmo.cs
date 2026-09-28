using Game.Client.HUD;
using Game.Shared;

namespace Game.Client.HL2;

[DeclareHudElement(Name = "CHudAmmo")]
public class HudAmmo : HudNumericDisplay, IHudElement
{
	BaseCombatWeapon? CurrentActiveWeapon;
	C_BaseEntity? CurrentVehicle;
	int Ammo;
	int Ammo2;
	HudTexture? IconPrimaryAmmo;

	public HudAmmo(string? panelName) : base(null, "HudAmmo") {
		ElementName = panelName;
		((IHudElement)this).SetHiddenBits(HideHudBits.Health | HideHudBits.PlayerDead | HideHudBits.NeedSuit | HideHudBits.WeaponSelection);

#if !GMOD_DLL
		hudlcd.SetGlobalStat("(ammo_primary)", "0");
		hudlcd.SetGlobalStat("(ammo_secondary)", "0");
		hudlcd.SetGlobalStat("(weapon_print_name)", "");
		hudlcd.SetGlobalStat("(weapon_name)", "");
#endif
	}

	public void Init() {
		Ammo = -1;
		Ammo2 = -1;
		IconPrimaryAmmo = null;

		ReadOnlySpan<char> tempString = Localize.Find("#Valve_Hud_AMMO");
		if (!tempString.IsEmpty)
			SetLabelText(tempString);
		else
			SetLabelText("AMMO");
	}

	public void VidInit() { }

	public override void Reset() {
		base.Reset();

		Blur = 0;

		CurrentActiveWeapon = null;
		CurrentVehicle = null;
		Ammo = 0;
		Ammo2 = 0;

		UpdateAmmoDisplays();
	}

#if GMOD_DLL
	public static bool WeaponChangedAnimation(bool usesSecondaryAmmo, bool usesClips, bool usesSecondaryClips) {
		if (!usesSecondaryAmmo) {
			if (usesClips) {
				clientMode.GetViewportAnimationController()!.StartAnimationSequence("WeaponUsesClips");
				return true;
			}
			clientMode.GetViewportAnimationController()!.StartAnimationSequence("WeaponDoesNotUseClips");
			return false;
		}
		if (usesClips) {
			if (usesSecondaryClips)
				clientMode.GetViewportAnimationController()!.StartAnimationSequence("WeaponUsesClipsAndSecondaryClips");
			else
				clientMode.GetViewportAnimationController()!.StartAnimationSequence("WeaponUsesClipsAndSecondaryAmmo");
			return true;
		}
		if (usesSecondaryClips)
			clientMode.GetViewportAnimationController()!.StartAnimationSequence("WeaponDoesNotUseClipsAndUsesSecondaryClips");
		else
			clientMode.GetViewportAnimationController()!.StartAnimationSequence("WeaponDoesNotUseClipsButUsesSecondaryAmmo");
		return false;
	}

	void UpdatePlayerAmmo(BasePlayer? player) {
		CurrentVehicle = null;
		BaseCombatWeapon? weapon = BaseCombatWeapon.GetActiveWeapon();
		if (weapon != null && !weapon.IsBaseCombatWeapon())
			weapon = null;

		// todo: lua hook (CustomAmmoDisplay)

		if (weapon == null || player == null || !weapon.UsesPrimaryAmmo()) {
			SetPaintEnabled(false);
			SetPaintBackgroundEnabled(false);
			CurrentActiveWeapon = weapon;
			return;
		}

		SetPaintEnabled(true);
		SetPaintBackgroundEnabled(true);

		int ammo1 = weapon.Clip1();
		int ammo2;

		if (ammo1 < 0) {
			ammo1 = player.GetAmmoCount(weapon.GetPrimaryAmmoType());
			ammo2 = 0;
		}
		else
			ammo2 = player.GetAmmoCount(weapon.GetPrimaryAmmoType());

		if (weapon != CurrentActiveWeapon) {
			SetShouldDisplaySecondaryValue(WeaponChangedAnimation(weapon.UsesSecondaryAmmo(), weapon.UsesClipsForAmmo1(), weapon.UsesClipsForAmmo2()));
			clientMode.GetViewportAnimationController()!.StartAnimationSequence("WeaponChanged");
			CurrentActiveWeapon = weapon;
			Ammo = -1;
			Ammo2 = -1;
			SetAmmo(ammo1, false);
			SetAmmo2(ammo2, false);
		}
		else {
			SetAmmo(ammo1, true);
			SetAmmo2(ammo2, true);
		}
	}
#else
	void UpdatePlayerAmmo(BasePlayer? player) {
		CurrentVehicle = null;
		BaseCombatWeapon? weapon = BaseCombatWeapon.GetActiveWeapon();

		hudlcd.SetGlobalStat("(weapon_print_name)", weapon != null ? weapon.GetPrintName() : " ");
		hudlcd.SetGlobalStat("(weapon_name)", weapon != null ? weapon.GetName() : " ");

		if (weapon == null || player == null || !weapon.UsesPrimaryAmmo()) {
			hudlcd.SetGlobalStat("(ammo_primary)", "n/a");
			hudlcd.SetGlobalStat("(ammo_secondary)", "n/a");

			SetPaintEnabled(false);
			SetPaintBackgroundEnabled(false);
			return;
		}

		SetPaintEnabled(true);
		SetPaintBackgroundEnabled(true);

		IconPrimaryAmmo = gWR.GetAmmoIconFromWeapon(weapon.GetPrimaryAmmoType());

		int ammo1 = weapon.Clip1();
		int ammo2;

		if (ammo1 < 0) {
			ammo1 = player.GetAmmoCount(weapon.GetPrimaryAmmoType());
			ammo2 = 0;
		}
		else
			ammo2 = player.GetAmmoCount(weapon.GetPrimaryAmmoType());

		hudlcd.SetGlobalStat("(ammo_primary)", ammo1.ToString());
		hudlcd.SetGlobalStat("(ammo_secondary)", ammo2.ToString());

		if (weapon == CurrentActiveWeapon) {
			SetAmmo(ammo1, true);
			SetAmmo2(ammo2, true);
		}
		else {
			SetAmmo(ammo1, false);
			SetAmmo2(ammo2, false);

			if (weapon.UsesClipsForAmmo1()) {
				SetShouldDisplaySecondaryValue(true);
				clientMode.GetViewportAnimationController()!.StartAnimationSequence("WeaponUsesClips");
			}
			else {
				clientMode.GetViewportAnimationController()!.StartAnimationSequence("WeaponDoesNotUseClips");
				SetShouldDisplaySecondaryValue(false);
			}

			clientMode.GetViewportAnimationController()!.StartAnimationSequence("WeaponChanged");
			CurrentActiveWeapon = weapon;
		}
	}
#endif

	// void UpdateVehicleAmmo(BasePlayer player, IClientVehicle vehicle) {

	// }

	public override void OnThink() => UpdateAmmoDisplays();

	void UpdateAmmoDisplays() {
		BasePlayer? player = C_BasePlayer.GetLocalPlayer();
		// IClientVehicle? vehicle = player != null ? player.GetVehicle() : null;

		if (/* vehicle != null */ false) {
			// UpdateVehicleAmmo(player, vehicle);
		}
		else
			UpdatePlayerAmmo(player);
	}

	void SetAmmo(int ammo, bool playAnimation) {
		if (ammo != Ammo) {
			if (ammo == 0)
				clientMode.GetViewportAnimationController()!.StartAnimationSequence("AmmoEmpty");
			else if (ammo < Ammo)
				clientMode.GetViewportAnimationController()!.StartAnimationSequence("AmmoDecreased");
			else
				clientMode.GetViewportAnimationController()!.StartAnimationSequence("AmmoIncreased");
			Ammo = ammo;
		}

		SetDisplayValue(ammo);
	}

	void SetAmmo2(int ammo2, bool playAnimation) {
		if (ammo2 != Ammo2) {
			if (ammo2 == 0)
				clientMode.GetViewportAnimationController()!.StartAnimationSequence("Ammo2Empty");
			else if (ammo2 < Ammo2)
				clientMode.GetViewportAnimationController()!.StartAnimationSequence("Ammo2Decreased");
			else
				clientMode.GetViewportAnimationController()!.StartAnimationSequence("Ammo2Increased");
			Ammo2 = ammo2;
		}

		SetSecondaryValue(ammo2);
	}

	public override void Paint() {
		base.Paint();
#if !GMOD_DLL
		if (IconPrimaryAmmo != null) {//todo && vehicle null
			Surface.GetTextSize(TextFont, LabelText, out int labelWide, out int labelTall);
			int x = (int)text_xpos + (labelWide - IconPrimaryAmmo.Width()) / 2;
			int y = (int)text_ypos - (labelTall + (IconPrimaryAmmo.Height() / 2));
			IconPrimaryAmmo.DrawSelf(x, y, GetFgColor());
		}
#endif
	}
}


[DeclareHudElement(Name = "CHudAmmoSecondary")]
public class HudAmmoSecondary : HudNumericDisplay, IHudElement
{
	BaseCombatWeapon? CurrentActiveWeapon;
	int Ammo;
#if GMOD_DLL
	int Ammo2;
	bool UsesSecondaryAmmo;
	bool UsesSecondaryClips;
	bool CustomAmmo;
	bool CustomUsesSecondaryAmmo;
	bool CustomUsesSecondaryClips;
#else
	HudTexture? IconSecondaryAmmo;
#endif

	public HudAmmoSecondary(string? panelName) : base(null, "HudAmmoSecondary") {
		ElementName = panelName;
		Ammo = -1;
#if GMOD_DLL
		Ammo2 = -1;
#endif
		((IHudElement)this).SetHiddenBits(HideHudBits.Health | HideHudBits.PlayerDead | HideHudBits.NeedSuit | HideHudBits.WeaponSelection);
	}

	public void Init() {
		ReadOnlySpan<char> tempString = Localize.Find("#Valve_Hud_AMMO_ALT");
		if (!tempString.IsEmpty)
			SetLabelText(tempString);
		else
			SetLabelText("ALT");
	}

	public void VidInit() { }

	void SetAmmo(int ammo) {
		if (ammo != Ammo) {
			if (ammo == 0)
				clientMode.GetViewportAnimationController()!.StartAnimationSequence("AmmoSecondaryEmpty");
			else if (ammo < Ammo)
				clientMode.GetViewportAnimationController()!.StartAnimationSequence("AmmoSecondaryDecreased");
			else
				clientMode.GetViewportAnimationController()!.StartAnimationSequence("AmmoSecondaryIncreased");
			Ammo = ammo;
		}
		SetDisplayValue(ammo);
	}

#if GMOD_DLL
	void SetAmmo2(int ammo2) {
		if (ammo2 != Ammo2) {
			if (ammo2 == 0)
				clientMode.GetViewportAnimationController()!.StartAnimationSequence("Ammo2SecondaryEmpty");
			else if (ammo2 < Ammo2)
				clientMode.GetViewportAnimationController()!.StartAnimationSequence("Ammo2SecondaryDecreased");
			else
				clientMode.GetViewportAnimationController()!.StartAnimationSequence("Ammo2SecondaryIncreased");
			Ammo2 = ammo2;
		}
		SetSecondaryValue(ammo2);
	}
#endif

	public override void Reset() {
		base.Reset();
		Ammo = 0;
#if GMOD_DLL
		Ammo2 = 0;
#endif
		CurrentActiveWeapon = null;
		SetAlpha(0);
		UpdateAmmoState();
	}

	public override void Paint() {
		base.Paint();

#if !GMOD_DLL
		if (IconSecondaryAmmo != null) {
			Surface.GetTextSize(TextFont, LabelText, out int labelWide, out int labelTall);
			int x = (int)text_xpos + (labelWide - IconSecondaryAmmo.Width()) / 2;
			int y = (int)text_ypos - (labelTall + (IconSecondaryAmmo.Height() / 2));
			IconSecondaryAmmo.DrawSelf(x, y, GetFgColor());
		}
#endif
	}

	public override void OnThink() {
		BaseCombatWeapon? weapon = BaseCombatWeapon.GetActiveWeapon();
#if GMOD_DLL
		CustomUsesSecondaryAmmo = false;
		CustomUsesSecondaryClips = false;
		CustomAmmo = false;
		// todo: lua hook (CustomAmmoDisplay)
		// CustomAmmo = true;
		// if (!draw || (secondaryAmmo < 0 && secondaryClip < 0)) {
		// 	CurrentActiveWeapon = null;
		// 	SetPaintEnabled(false);
		// 	SetPaintBackgroundEnabled(false);
		// 	UpdateAmmoState();
		// 	return;
		// }
		// SetAmmo(secondaryAmmo);
		// SetPaintEnabled(true);
		// SetPaintBackgroundEnabled(true);
		// CustomUsesSecondaryAmmo = true;
		// if (secondaryClip >= 0) {
		// 	SetAmmo(secondaryClip);
		// 	SetAmmo2(secondaryAmmo);
		// 	CustomUsesSecondaryClips = true;
		// 	SetShouldDisplaySecondaryValue(true);
		// }
#endif
		BasePlayer? player = BasePlayer.GetLocalPlayer();
		// IClientVehicle? vehicle = player != null ? player.GetVehicle() : null;

		if (weapon == null || player == null || /* vehicle != null */ false) {
			CurrentActiveWeapon = null;
			SetPaintEnabled(false);
			SetPaintBackgroundEnabled(false);
#if GMOD_DLL
			UsesSecondaryAmmo = false;
#endif
			return;
		}
		else {
			SetPaintEnabled(true);
			SetPaintBackgroundEnabled(true);
		}

		UpdateAmmoState();
	}


#if GMOD_DLL
	void UpdateAmmoState() {
		BaseCombatWeapon? weapon = BaseCombatWeapon.GetActiveWeapon();
		BasePlayer? player = BasePlayer.GetLocalPlayer();

		bool usesClips = weapon != null && weapon.UsesClipsForAmmo1();
		bool usesSecondaryClips = weapon != null && weapon.UsesClipsForAmmo2();
		bool usesSecondaryAmmo = weapon != null && weapon.UsesSecondaryAmmo();

		if (CustomAmmo) {
			usesSecondaryAmmo = CustomUsesSecondaryAmmo;
			usesSecondaryClips = CustomUsesSecondaryClips;
		}

		if (UsesSecondaryAmmo != usesSecondaryAmmo || CurrentActiveWeapon != weapon || UsesSecondaryClips != usesSecondaryClips) {
			if (usesSecondaryAmmo)
				clientMode.GetViewportAnimationController()!.StartAnimationSequence("WeaponUsesSecondaryAmmo");
			else
				clientMode.GetViewportAnimationController()!.StartAnimationSequence("WeaponDoesNotUseSecondaryAmmo");

			if (!CustomAmmo)
				HudAmmo.WeaponChangedAnimation(usesSecondaryAmmo, usesClips, usesSecondaryClips);

			if (!usesSecondaryClips) {
				clientMode.GetViewportAnimationController()!.StartAnimationSequence("WeaponDoesNotUseSecondaryClips");
				SetShouldDisplaySecondaryValue(false);
			}
			else {
				SetShouldDisplaySecondaryValue(true);
				clientMode.GetViewportAnimationController()!.StartAnimationSequence("WeaponUsesSecondaryClips");
			}

			UsesSecondaryAmmo = usesSecondaryAmmo;
			CurrentActiveWeapon = weapon;
			UsesSecondaryClips = usesSecondaryClips;
			Ammo = -1;
		}

		if (player != null && weapon != null && weapon.UsesSecondaryAmmo() && !CustomUsesSecondaryAmmo) {
			if (usesSecondaryClips) {
				SetAmmo(weapon.Clip2());
				SetAmmo2(player.GetAmmoCount(weapon.GetSecondaryAmmoType()));
				return;
			}
			SetAmmo(player.GetAmmoCount(weapon.GetSecondaryAmmoType()));
		}
	}
#else
	void UpdateAmmoState() {
		BaseCombatWeapon? weapon = BaseCombatWeapon.GetActiveWeapon();
		BasePlayer? player = BasePlayer.GetLocalPlayer();

		if (player != null && weapon != null && weapon.UsesSecondaryAmmo())
			SetAmmo(player.GetAmmoCount(weapon.GetSecondaryAmmoType()));

		if (weapon != CurrentActiveWeapon) {
			if (weapon != null && weapon.UsesSecondaryAmmo())
				clientMode.GetViewportAnimationController()!.StartAnimationSequence("WeaponUsesSecondaryAmmo");
			else
				clientMode.GetViewportAnimationController()!.StartAnimationSequence("WeaponDoesNotUseSecondaryAmmo");
			CurrentActiveWeapon = weapon;
			IconSecondaryAmmo = gWR.GetAmmoIconFromWeapon(weapon!.GetSecondaryAmmoType());
		}
	}
#endif
}
