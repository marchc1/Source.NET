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
		((IHudElement)this).SetHiddenBits(HideHudBits.Health | HideHudBits.PlayerDead | HideHudBits.NeedSuit | HideHudBits.WeaponSelection);
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

	public void Reset() {
		Blur = 0;

		CurrentActiveWeapon = null;
		CurrentVehicle = null;
		Ammo = 0;
		Ammo2 = 0;

		UpdateAmmoDisplays();
	}

	void UpdatePlayerAmmo(BasePlayer? player) {
		CurrentVehicle = null;
		BaseCombatWeapon? weapon = BaseCombatWeapon.GetActiveWeapon();

		LCD.SetGlobalStat("(weapon_print_name)", weapon != null ? weapon.GetPrintName() : "");
		LCD.SetGlobalStat("(weapon_name)", weapon != null ? weapon.GetName() : "");

		if (weapon == null || player == null || !weapon.UsesPrimaryAmmo()) {
			LCD.SetGlobalStat("(ammo_primary)", "n/a");
			LCD.SetGlobalStat("(ammo_secondary)", "n/a");

			SetPaintEnabled(false);
			SetPaintBackgroundEnabled(false);
			return;
		}

		SetPaintEnabled(true);
		SetPaintBackgroundEnabled(true);

		// IconPrimaryAmmo = gWR.GetAmmoIconFromWeapon(weapon.PrimaryAmmoType);

		int ammo1 = weapon.Clip1;
		int ammo2;

		if (ammo1 < 0) {
			ammo1 = player.GetAmmoCount(weapon.PrimaryAmmoType);
			ammo2 = 0;
		}
		else
			ammo2 = player.GetAmmoCount(weapon.PrimaryAmmoType);

		LCD.SetGlobalStat("(ammo_primary)", ammo1.ToString());
		LCD.SetGlobalStat("(ammo_secondary)", ammo2.ToString());

		if (weapon == CurrentActiveWeapon) {
			SetAmmo(ammo1, true);
			SetAmmo2(ammo2, true);
		}
		else {
			SetAmmo(ammo1, false);
			SetAmmo2(ammo2, false);

			if (weapon.UsesClipsForAmmo1()) {
				// SetShouldDisplaySecondaryValue(true);
				clientMode.GetViewportAnimationController()!.StartAnimationSequence("WeaponUsesClips");
			}
			else {
				clientMode.GetViewportAnimationController()!.StartAnimationSequence("WeaponDoesNotUseClips");
				// SetShouldDisplaySecondaryValue(false);
			}

			clientMode.GetViewportAnimationController()!.StartAnimationSequence("WeaponChanged");
			CurrentActiveWeapon = weapon;
		}
	}

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
	}

	public override void Paint() {
		base.Paint();
		if (IconPrimaryAmmo != null) {//todo && vehicle null
			Surface.GetTextSize(TextFont, LabelText, out int labelWide, out int labelTall);
			int x = (int)text_xpos + (labelWide - IconPrimaryAmmo.Width()) / 2;
			int y = (int)text_ypos - (labelTall + (IconPrimaryAmmo.Height() / 2));
			IconPrimaryAmmo.DrawSelf(x, y, GetFgColor());
		}
	}
}


[DeclareHudElement(Name = "CHudAmmoSecondary")]
public class HudAmmoSecondary : HudNumericDisplay, IHudElement
{
	BaseCombatWeapon? CurrentActiveWeapon;
	int Ammo;
	HudTexture? IconSecondaryAmmo;

	public HudAmmoSecondary(string? panelName) : base(null, "HudAmmoSecondary") {
		Ammo = -1;
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

	public void Reset() {
		Blur = 0;
		Ammo = 0;
		CurrentActiveWeapon = null;
		SetAlpha(0);
		UpdateAmmoState();
	}

	public override void Paint() {
		base.Paint();

		if (IconSecondaryAmmo != null) {
			Surface.GetTextSize(TextFont, LabelText, out int labelWide, out int labelTall);
			int x = (int)text_xpos + (labelWide - IconSecondaryAmmo.Width()) / 2;
			int y = (int)text_ypos + labelTall + (IconSecondaryAmmo.Height() / 2);
			IconSecondaryAmmo.DrawSelf(x, y, GetFgColor());
		}
	}

	public override void OnThink() {
		BaseCombatWeapon? weapon = BaseCombatWeapon.GetActiveWeapon();
		BasePlayer? player = BasePlayer.GetLocalPlayer();
		// IClientVehicle? vehicle = player != null ? player.GetVehicle() : null;

		if (weapon == null || player == null || /* vehicle != null */ false) {
			CurrentActiveWeapon = null;
			SetPaintEnabled(false);
			SetPaintBackgroundEnabled(false);
			return;
		}
		else {
			SetPaintEnabled(true);
			SetPaintBackgroundEnabled(true);
		}

		UpdateAmmoState();
	}


	void UpdateAmmoState() {
		BaseCombatWeapon? weapon = BaseCombatWeapon.GetActiveWeapon();
		BasePlayer? player = BasePlayer.GetLocalPlayer();

		if (player != null && weapon != null && weapon.UsesSecondaryAmmo())
			SetAmmo(player.GetAmmoCount(weapon.SecondaryAmmoType));

		if (weapon != CurrentActiveWeapon) {
			if (weapon.UsesSecondaryAmmo())
				clientMode.GetViewportAnimationController()!.StartAnimationSequence("WeaponUsesSecondaryAmmo");
			else
				clientMode.GetViewportAnimationController()!.StartAnimationSequence("WeaponDoesNotUseSecondaryAmmo");
			CurrentActiveWeapon = weapon;
			// IconSecondaryAmmo = gWR.GetAmmoIconFromWeapon(weapon.GetSecondaryAmmoType());//todo
		}
	}
}
