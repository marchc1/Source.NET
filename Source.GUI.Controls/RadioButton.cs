using Source.Common.Formats.Keyvalues;
using Source.Common.GUI;
using Source.Common.Input;

namespace Source.GUI.Controls;

enum Direction
{
	Up = -1,
	Down = 1
}

public class RadioButton : ToggleButton
{
	// RadioImage RadioBoxImage;
	int OldTabPosition;
	int SubTabPosition;

	public RadioButton(Panel parent, string name, string text) : base(parent, name, text) {
		SetContentAlignment(Alignment.West);

		// RadioBoxImage = new(this);

		OldTabPosition = 0;
		SubTabPosition = 0;

		// SetTextImageIndex(1);
		// SetImageAtIndex(1, RadioBoxImage, 0);

		SetButtonActivationType(ActivationType.OnPressed);
	}

	public override void ApplySchemeSettings(IScheme scheme) {
		base.ApplySchemeSettings(scheme);
	}

	public override IBorder? GetBorder(bool depressed, bool armed, bool selected, bool keyfocus) {
		return null;
	}

	public int GetSubTabPosition() => SubTabPosition;
	public void SetSubTabPosition(int pos) => SubTabPosition = pos;
	public int GetRadioTabPosition() => OldTabPosition;

	public override void SetSelected(bool state) => InternalSetSelected(state, true);

	public void InternalSetSelected(bool state, bool fireEvents) {
		if (state) {
			if (!IsEnabled())
				return;

			SetTabPosition(OldTabPosition);

			if (fireEvents) {
				KeyValues msg = new("RadioButtonChecked");
				msg.SetPtr("panel", this);
				msg.SetInt("tabposition", OldTabPosition);

				Panel? radioParent = GetParent();
				if (radioParent != null) {
					for (int i = 0; i < radioParent.GetChildCount(); i++) {
						RadioButton? child = (RadioButton)radioParent.GetChild(i);
						if (child != null && child != this)
							child.PostMessage(radioParent, msg.MakeCopy());
					}
				}

				RequestFocus();
				PostActionSignal(msg);
			}
		}
		else {
			if (GetTabPosition() != 0)
				OldTabPosition = GetTabPosition();
			SetTabPosition(0);
		}

		InvalidateLayout();
		Repaint();

		base.SetSelected(state);
	}

	public void SilentSetSelected(bool state) => InternalSetSelected(state, false);

	public override void PerformLayout() {
		if (IsSelected())
			SetFgColor(SelectedFgColor);
		else
			SetFgColor(GetButtonFgColor());

		base.PerformLayout();
	}

	public override void ApplySettings(KeyValues resourceData) {
		base.ApplySettings(resourceData);

		SetTextColorState(ColorState.Normal);
		SubTabPosition = resourceData.GetInt("SubTabPosition");
		OldTabPosition = resourceData.GetInt("TabPosition");
		// SetImageAtIndex(0, RadioButtonImage, 0);
	}

	public override void GetSettings(KeyValues resourceData) {
		base.GetSettings(resourceData);

		resourceData.SetInt("SubTabPosition", SubTabPosition);
		resourceData.SetInt("TabPosition", GetRadioTabPosition());
	}

	public void OnRadioButtonChecked(int tabPosition) {
		if (tabPosition != OldTabPosition)
			return;

		SetSelected(false);
	}

	public override void Paint() {
		base.Paint();
	}

	public override void DoClick() => SetSelected(true);

	public override void OnKeyCodeTyped(ButtonCode code) {
		switch (code) {
			case ButtonCode.KeyEnter:
			case ButtonCode.KeySpace:
				if (!IsSelected())
					SetSelected(true);
				else
					base.OnKeyCodeTyped(code);
				break;
			case ButtonCode.KeyDown:
			case ButtonCode.KeyRight:
				RadioButton? bestRadio = FindBestRadioButton((int)Direction.Down);
				if (bestRadio != null)
					bestRadio.SetSelected(true);
				break;
			case ButtonCode.KeyUp:
			case ButtonCode.KeyLeft:
				bestRadio = FindBestRadioButton((int)Direction.Up);
				if (bestRadio != null)
					bestRadio.SetSelected(true);
				break;
			default:
				base.OnKeyCodeTyped(code);
				break;
		}
	}

	public RadioButton? FindBestRadioButton(int direction) {
		RadioButton? bestRadio = null;
		int highestRadio = 0;
		Panel? pr = GetParent();
		if (pr != null) {
			for (int i = 0; i < pr.GetChildCount(); i++) {
				RadioButton? child = (RadioButton)pr.GetChild(i);
				if (child != null && child.GetSubTabPosition() == OldTabPosition) {
					if (child.GetSubTabPosition() == SubTabPosition + direction) {
						bestRadio = child;
						break;
					}

					if (child.GetSubTabPosition() == 0 && direction == (int)Direction.Down) {
						bestRadio = child;
						continue;
					}

					if (child.GetSubTabPosition() > highestRadio && direction == (int)Direction.Up) {
						bestRadio = child;
						highestRadio = child.GetSubTabPosition();
						continue;
					}

					if (bestRadio == null)
						bestRadio = child;
				}
			}

			if (bestRadio != null)
				bestRadio.RequestFocus();

			InvalidateLayout();
			Repaint();
		}

		return bestRadio;
	}

	public override void OnMessage(KeyValues message, IPanel? from) {
		if (message.Name == "RadioButtonChecked") {
			OnRadioButtonChecked(message.GetInt("tabposition", -1));
			return;
		}

		base.OnMessage(message, from);
	}
}
