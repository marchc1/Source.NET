using Source.Common.Formats.Keyvalues;
using Source.Common.GUI;
using Source.Common.Input;

namespace Source.GUI.Controls;

public class ToggleButton : Button
{
	Color SelectedColor;

	public ToggleButton(Panel parent, string name, string text) : base(parent, name, text) {
		SetButtonActivationType(ActivationType.OnPressed);
	}

	public override void OnMouseDoublePressed(ButtonCode code) {
		OnMousePressed(code);
	}

	public override Color GetButtonFgColor() {
		if (IsSelected())
			return SelectedColor;

		return base.GetButtonFgColor();
	}

	public override bool CanBeDefaultButton() {
		return false;
	}

	public override void DoClick() {
		if (IsSelected())
			ForceDepressed(true);
		else
			ForceDepressed(false);

		SetSelected(!IsSelected());
		FireActionSignal();

		KeyValues msg = new("ButtonToggled");
		msg.SetInt("state", IsSelected() ? 1 : 0);
		PostActionSignal(msg);

		Repaint();
	}

	public override void ApplySchemeSettings(IScheme scheme) {
		base.ApplySchemeSettings(scheme);
		SelectedColor = GetSchemeColor("ToggleButton.SelectedTextColor", scheme);
	}

	public override void OnKeyCodePressed(ButtonCode code) {
		if (code == ButtonCode.KeyEnter)
			base.OnKeyCodePressed(code);
	}
}
