using Game.Client.HL2;
using Game.Shared;

using Source;

namespace Game.Client;

public partial class C_BaseCombatWeapon : C_BaseAnimating
{
	public override bool IsBaseCombatWeapon() => true;
	public static BaseCombatWeapon? GetActiveWeapon() {
		BasePlayer? player = C_BasePlayer.GetLocalPlayer();
		return player?.GetActiveWeapon();
	}

	public bool IsCarriedByLocalPlayer() {
		SharedBaseEntity? owner = GetOwner();
		if (owner == null)
			return false;
		return owner == C_BasePlayer.GetLocalPlayer();
	}

	public bool ShouldDrawUsingViewModel() => IsCarriedByLocalPlayer() && !C_BasePlayer.ShouldDrawLocalPlayer();

	public void Redraw() {
		// if (clientMode.ShouldDrawCrosshair()) todo
		DrawCrosshair();
	}

	void DrawCrosshair() {
		BasePlayer? player = BasePlayer.GetLocalPlayer();
		if (player == null)
			return;

		Color clr = gHUD.ClrNormal;

		if (gHUD.FindElement("CHudCrosshair") is not HudCrosshair crosshair)
			return;

		bool onTarget = State == 0x40; // WEAPON_IS_ONTARGET

		if (/*player.GetFOV() >= 90*/ false) { // todo

		}
		else {
			Color white = new(255, 255, 255, 255);

			FileWeaponInfo wepData = GetWpnData();
			if (onTarget && wepData.IconZoomedAutoaim != null)
				crosshair.SetCrosshair(wepData.IconZoomedAutoaim, white);
			else if (wepData.IconZoomedCrosshair != null)
				crosshair.SetCrosshair(wepData.IconZoomedCrosshair, white);
			else
				crosshair.ResetCrosshair();
		}
	}
}
