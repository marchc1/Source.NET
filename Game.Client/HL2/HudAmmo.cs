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

		SetDisplayValue(ammo);
	}

	void SetAmmo2(int ammo2, bool playAnimation) {


		SetSecondaryValue(ammo2);
	}

	public override void Paint() {
		base.Paint();
		if (IconPrimaryAmmo != null) {//todo && vehicle null
			Surface.GetTextSize(TextFont, LabelText, out int labelWide, out int labelTall);
			int x = (int)text_xpos + (labelWide - IconPrimaryAmmo.Width()) / 2;
			int y = (int)text_ypos + labelTall + (IconPrimaryAmmo.Height() / 2);
			IconPrimaryAmmo.DrawSelf(x, y, GetFgColor());
		}
	}
}


[DeclareHudElement(Name = "CHudAmmoSecondary")]
public class HudAmmoSecondary : HudNumericDisplay, IHudElement
{
	public HudAmmoSecondary(string? panelName) : base(null, "HudAmmoSecondary") {

	}

	public void Init() {

	}
}
