using Game.Client.HUD;
using Game.Shared;

using Source;
using Source.Common.GUI;
using Source.GUI.Controls;

namespace Game.Client.HL2;

[DeclareHudElement(Name = "CHudPoisonDamageIndicator")]
public class HudPoisonDamageIndicator : EditableHudElement, IHudElement
{
	[PanelAnimationVar("TextFont", "Default", "Font")] protected IFont Font;
	[PanelAnimationVar("TextColor", "FgColor", "Color")] protected Color TextColor;
	[PanelAnimationVar("text_xpos", "8", "proportional_float")] protected float TextXPos;
	[PanelAnimationVar("text_ypos", "8", "proportional_float")] protected float TextYPos;
	[PanelAnimationVar("text_ygap", "14", "proportional_float")] protected float TextYGap;
	bool DamageIndicatorVisible;

	public HudPoisonDamageIndicator(string? panelName) : base(null, "HudPoisonDamageIndicator") {
		var parent = clientMode.GetViewport();
		SetParent(parent);

		((IHudElement)this).SetHiddenBits(HideHudBits.Health | HideHudBits.NeedSuit | HideHudBits.PlayerDead);
	}

	public void Reset() {
		DamageIndicatorVisible = false;
		SetAlpha(0);
	}

	public bool ShouldDraw() {
		BasePlayer? player = BasePlayer.GetLocalPlayer();
		if (player == null)
			return false;

		bool needsDraw = player.IsPoisoned() != DamageIndicatorVisible || (GetAlpha() > 0);
		return needsDraw;// && ((IHudElement)this).ShouldDraw(); FIXME: Stack overflow
	}

	public override void OnThink() {
		BasePlayer? player = BasePlayer.GetLocalPlayer();
		if (player == null)
			return;

		bool shouldIndicatorBeVisible = player.IsPoisoned();
		if (shouldIndicatorBeVisible == DamageIndicatorVisible)
			return;

		DamageIndicatorVisible = shouldIndicatorBeVisible;

		if (DamageIndicatorVisible) {
			SetVisible(true);
			clientMode.GetViewportAnimationController()!.StartAnimationSequence("PoisonDamageTaken");
		}
		else
			clientMode.GetViewportAnimationController()!.StartAnimationSequence("PoisonDamageCured");
	}

	public override void Paint() {
		surface.DrawSetTextFont(Font);
		surface.DrawSetTextColor(TextColor);
		surface.DrawSetTextPos((int)TextXPos, (int)TextYPos);
		int ypos = (int)TextYPos;

		ReadOnlySpan<char> labelText = Localize.Find("Valve_HudPoisonDamage");
		Assert(!labelText.IsEmpty);

		for (int i = 0; i < labelText.Length; i++) {
			if (labelText[i] == '\n') {
				ypos += (int)TextYGap;
				surface.DrawSetTextPos((int)TextXPos, ypos);
			}
			else
				surface.DrawChar(labelText[i]);
		}
	}
}
