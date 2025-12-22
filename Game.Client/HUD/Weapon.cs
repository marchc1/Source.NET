using Game.Client.HUD;
using Game.Shared;

using Source.Common.GUI;
using Source.GUI.Controls;

namespace Game.Client.HL2;

[DeclareHudElement(Name = "CHudWeapon")]
public class HudWeapon : EditableHudElement, IHudElement
{
	HudCrosshair? Crosshair;

	public HudWeapon(string? panelName) : base(null, "HudWeapon") {
		var parent = clientMode.GetViewport();
		SetParent(parent);

		Crosshair = null;

		((IHudElement)this).SetHiddenBits(HideHudBits.WeaponSelection);
	}

	public override void ApplySchemeSettings(IScheme scheme) {
		base.ApplySchemeSettings(scheme);
		SetPaintBackgroundEnabled(false);
		Crosshair = gHUD.FindElement("CHudCrosshair") as HudCrosshair;
	}

	public override void PerformLayout() {
		base.PerformLayout();

		Panel parent = GetParent()!;
		parent.GetSize(out int wide, out int tall);
		SetPos(0, 0);
		SetSize(wide, tall);
	}

	public override void Paint() {
		BasePlayer? player = BasePlayer.GetLocalPlayer();
		if (player == null)
			return;

		BaseCombatWeapon? weapon = player.GetActiveWeapon();
		if (weapon != null)
			weapon.Redraw();
		else
			Crosshair?.ResetCrosshair();
	}
}