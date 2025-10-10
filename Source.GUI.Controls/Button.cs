using Source.Common.Formats.Keyvalues;
using Source.Common.GUI;
using Source.Common.Input;
using Source.Common.Launcher;

namespace Source.GUI.Controls;

public enum ActivationType
{
	OnPressedAndReleased,
	OnPressed,
	OnReleased
}

public enum ButtonFlags
{
	Armed = 0x0001,
	Depressed = 0x0002,
	ForceDepressed = 0x0004,
	ButtonBorderEnabled = 0x0008,
	UseCaptureMouse = 0x0010,
	ButtonKeyDown = 0x0020,
	DefaultButton = 0x0040,
	Selected = 0x0080,
	DrawFocusBox = 0x0100,
	Blink = 0x0200,
	AllFlags = 0xFFFF
}

public class Button : Label
{
	public static Panel Create_Button() => new Button(null, null, "Button");
	KeyValues? ActionMessage;
	ActivationType ActivationType;
	ButtonFlags ButtonFlags;
	int MouseClickMask;
	bool StayArmedOnClick;
	bool StaySelectedOnClick;

	string? ArmedSoundName;
	string? DepressedSoundName;
	string? ReleasedSoundName;

	readonly public ISystem System = Singleton<ISystem>();

	public override void OnMessage(KeyValues message, IPanel? from) {
		switch (message.Name) {
			case "Hotkey": DoClick(); return;
			default: base.OnMessage(message, from); return;
		}
	}
	public void Init() {
		ButtonFlags |= ButtonFlags.UseCaptureMouse | ButtonFlags.ButtonBorderEnabled;
		MouseClickMask = 0;
		ActionMessage = null;
		StaySelectedOnClick = false;
		StayArmedOnClick = false;
		ArmedSoundName = null;
		DepressedSoundName = null;
		ReleasedSoundName = null;
		SetTextInset(6, 0);
		SetMouseClickEnabled(ButtonCode.MouseLeft, true);
		SetButtonActivationType(ActivationType.OnPressedAndReleased);
	}

	public void SetButtonActivationType(ActivationType type) {
		ActivationType = type;
	}

	public void SetButtonBorderEnabled(bool state)
	{
		if (state != (0 != (ButtonFlags & ButtonFlags.ButtonBorderEnabled))) {
			ButtonFlags ^= ButtonFlags.ButtonBorderEnabled;
			InvalidateLayout(false);
		}
	}

	public void SetMouseClickEnabled(ButtonCode code, bool state) {
		if (state)
			MouseClickMask |= unchecked(1 << unchecked((int)(code + 1)));
		else
			MouseClickMask &= ~unchecked(1 << unchecked((int)(code + 1)));

	}

	public Button(Panel parent, string name, string text) : base(parent, name, text) {
		Init();
	}

	public virtual void DoClick() {
		SetSelected(true);
		FireActionSignal();
		PlayButtonReleasedSound();

		// vgui_nav_lock?

		if (!StaySelectedOnClick)
			SetSelected(false);
	}

	public void SetArmedSound(ReadOnlySpan<char> fileName) {
		ArmedSoundName = string.Intern(new(fileName));
	}
	public void SetDepressedSound(ReadOnlySpan<char> fileName) {
		DepressedSoundName = string.Intern(new(fileName));
	}
	public void SetReleasedSound(ReadOnlySpan<char> fileName) {
		ReleasedSoundName = string.Intern(new(fileName));
	}

	public void FireActionSignal() {
		if (ActionMessage != null) {
			// TODO: URL messages?
			PostActionSignal(ActionMessage.MakeCopy());
		}
	}

	public void PlayButtonReleasedSound() {
		if (ReleasedSoundName != null)
			Surface.PlaySound(ReleasedSoundName);
	}

	public override void PerformLayout() {
		SetBorder(GetBorder((ButtonFlags & ButtonFlags.Depressed) != 0, (ButtonFlags & ButtonFlags.Armed) != 0, (ButtonFlags & ButtonFlags.Selected) != 0, HasFocus()));

		SetFgColor(GetButtonFgColor());
		SetBgColor(GetButtonBgColor());

		base.PerformLayout();
	}

	public virtual IBorder? GetBorder(bool depressed, bool armed, bool selected, bool keyfocus) {
		if (0 != (ButtonFlags & ButtonFlags.ButtonBorderEnabled)) {
			if (depressed)
				return DepressedBorder;

			if (keyfocus)
				return KeyFocusBorder;

			if (IsEnabled() && 0 != (ButtonFlags & ButtonFlags.DefaultButton))
				return KeyFocusBorder;

			return DefaultBorder;
		}
		else {
			if (depressed)
				return DepressedBorder;

			if (armed)
				return DefaultBorder;
		}

		return DefaultBorder;
	}


	public Color GetButtonFgColor() {
		if (0 == (ButtonFlags & ButtonFlags.Blink)) {
			if (0 != (ButtonFlags & ButtonFlags.Depressed))
				return DepressedFgColor;
			if (0 != (ButtonFlags & ButtonFlags.Armed))
				return ArmedFgColor;
			if (0 != (ButtonFlags & ButtonFlags.Selected))
				return SelectedFgColor;
			return DefaultFgColor;
		}

		Color blended;

		if (0 != (ButtonFlags & ButtonFlags.Depressed))
			blended = DepressedFgColor;
		else if (0 != (ButtonFlags & ButtonFlags.Armed))
			blended = ArmedFgColor;
		else if (0 != (ButtonFlags & ButtonFlags.Selected))
			blended = SelectedFgColor;
		else
			blended = DefaultFgColor;

		float fBlink = (MathF.Sin(System.GetTimeMillis() * 0.01f) + 1.0f) * 0.5f;

		if (0 != (ButtonFlags & ButtonFlags.Blink)) {
			blended[0] = (byte)Math.Clamp(blended[0] * fBlink + (float)BlinkFgColor[0] * (1.0f - fBlink), 0, 255);
			blended[1] = (byte)Math.Clamp(blended[1] * fBlink + (float)BlinkFgColor[1] * (1.0f - fBlink), 0, 255);
			blended[2] = (byte)Math.Clamp(blended[2] * fBlink + (float)BlinkFgColor[2] * (1.0f - fBlink), 0, 255);
			blended[3] = (byte)Math.Clamp(blended[3] * fBlink + (float)BlinkFgColor[3] * (1.0f - fBlink), 0, 255);
		}

		return blended;
	}

	public Color GetButtonBgColor() {
		if (0 != (ButtonFlags & ButtonFlags.Depressed))
			return DepressedBgColor;
		if (0 != (ButtonFlags & ButtonFlags.Armed))
			return ArmedBgColor;
		if (0 != (ButtonFlags & ButtonFlags.Selected))
			return SelectedBgColor;
		return DefaultBgColor;
	}

	public override void OnMousePressed(ButtonCode code) {
		if (!IsEnabled())
			return;

		if (!IsMouseClickEnabled(code))
			return;

		if (ActivationType == ActivationType.OnPressed) {
			if (IsKeyboardInputEnabled()) {
				RequestFocus();
			}
			DoClick();
			return;
		}

		if (DepressedSoundName != null)
			Surface.PlaySound(DepressedSoundName);

		if (IsUseCaptureMouseEnabled() && ActivationType == ActivationType.OnPressedAndReleased) {
			if (IsKeyboardInputEnabled())
				RequestFocus();

			SetSelected(true);
			Repaint();

			Input.SetMouseCapture(this);
		}
	}

	public override void OnMouseReleased(ButtonCode code) {
		if (IsUseCaptureMouseEnabled())
			Input.SetMouseCapture(null);

		if (ActivationType == ActivationType.OnPressed)
			return;

		if (!IsMouseClickEnabled(code))
			return;

		if (!IsSelected() && ActivationType == ActivationType.OnPressedAndReleased)
			return;

		if (IsEnabled() && (this == Input.GetMouseOver() || (ButtonFlags & ButtonFlags.ButtonKeyDown) != 0)) {
			DoClick();
		}
		else if (!StaySelectedOnClick) {
			SetSelected(false);
		}

		Repaint();
	}

	public void SetSelected(bool state) {
		if (((ButtonFlags & ButtonFlags.Selected) != 0) != state) {
			if (state)
				ButtonFlags |= ButtonFlags.Selected;
			else
				ButtonFlags &= ~ButtonFlags.Selected;

			RecalculateDepressedState();
			InvalidateLayout(false);
		}

		if (!StayArmedOnClick && state && (ButtonFlags & ButtonFlags.Armed) != 0) {
			ButtonFlags &= ~ButtonFlags.Armed;
			InvalidateLayout(false);
		}
	}

	public bool IsSelected() {
		return (ButtonFlags & ButtonFlags.Selected) != 0;
	}

	public bool IsMouseClickEnabled(ButtonCode code) {
		return (MouseClickMask & unchecked(1 << unchecked((int)(code + 1)))) != 0;
	}

	public bool IsUseCaptureMouseEnabled() => (ButtonFlags & ButtonFlags.UseCaptureMouse) != 0;
	public void SetUseCaptureMouse(bool state) {
		if (state) ButtonFlags |= ButtonFlags.UseCaptureMouse;
		else ButtonFlags &= ~ButtonFlags.UseCaptureMouse;
	}
	public bool IsArmed() => (ButtonFlags & ButtonFlags.Armed) != 0;

	public void SetArmed(bool state) {
		if (((ButtonFlags & ButtonFlags.Armed) != 0) != state) {
			ButtonFlags |= ButtonFlags.Armed;
			RecalculateDepressedState();
			InvalidateLayout(false);
			if (state && ArmedSoundName != null)
				Surface.PlaySound(ArmedSoundName);
		}
	}

	private void RecalculateDepressedState() {
		bool newState;
		if (!IsEnabled())
			newState = false;
		else {
			if (StaySelectedOnClick && (ButtonFlags & ButtonFlags.Selected) != 0) {
				newState = false;
			}
			else {
				newState = (ButtonFlags & ButtonFlags.Depressed) != 0
						 || (ButtonFlags&ButtonFlags.Armed) != 0 && (ButtonFlags &ButtonFlags.Selected) != 0;
			}
		}

		if (newState)
			ButtonFlags |= ButtonFlags.Depressed;
		else
			ButtonFlags &= ~ButtonFlags.Depressed;
	}

	public KeyValues? GetCommand() => ActionMessage;
	public void SetCommand(ReadOnlySpan<char> command) => SetCommand(new KeyValues("Command", "command", command));
	public void SetCommand(KeyValues command) {
		ActionMessage = null;
		ActionMessage = command;
	}

	Color DefaultFgColor, DefaultBgColor;
	Color ArmedFgColor, ArmedBgColor;
	Color SelectedFgColor, SelectedBgColor;
	Color DepressedFgColor, DepressedBgColor;
	Color BlinkFgColor;
	Color KeyboardFocusColor;

	IBorder? DefaultBorder, DepressedBorder, KeyFocusBorder;

	public override void ApplySchemeSettings(IScheme scheme) {
		base.ApplySchemeSettings(scheme);
		DefaultBorder = scheme.GetBorder("ButtonBorder");
		DepressedBorder = scheme.GetBorder("ButtonDepressedBorder");
		KeyFocusBorder = scheme.GetBorder("ButtonKeyFocusBorder");

		DefaultFgColor = GetSchemeColor("Button.TextColor", new(255, 255, 255, 255), scheme);
		DefaultBgColor = GetSchemeColor("Button.BgColor", new(0, 0, 0, 255), scheme);

		ArmedFgColor = GetSchemeColor("Button.ArmedTextColor", DefaultFgColor, scheme);
		ArmedBgColor = GetSchemeColor("Button.ArmedBgColor", DefaultBgColor, scheme);

		SelectedFgColor = GetSchemeColor("Button.SelectedTextColor", SelectedFgColor, scheme);
		SelectedBgColor = GetSchemeColor("Button.SelectedBgColor", SelectedBgColor, scheme);

		DepressedFgColor = GetSchemeColor("Button.DepressedTextColor", DefaultFgColor, scheme);
		DepressedBgColor = GetSchemeColor("Button.DepressedBgColor", DefaultBgColor, scheme);
		KeyboardFocusColor = GetSchemeColor("Button.FocusBorderColor", new(0, 0, 0, 255), scheme);

		BlinkFgColor = GetSchemeColor("Button.BlinkColor", new(255, 155, 0, 255), scheme);
		InvalidateLayout();
	}

	public void SetDefaultBorder(IBorder? border) {
		DefaultBorder = border;
		InvalidateLayout(false);
	}

	public void SetDepressedBorder(IBorder? border) {
		DepressedBorder = border;
		InvalidateLayout(false);
	}

	public void SetKeyFocusBorder(IBorder? border) {
		KeyFocusBorder = border;
		InvalidateLayout(false);
	}

	public void SetDefaultColor(Color fgColor, Color bgColor) {
		if (!(DefaultFgColor == fgColor && DefaultBgColor == bgColor)) {
			DefaultFgColor = fgColor;
			DefaultBgColor = bgColor;

			InvalidateLayout(false);
		}
	}
	public void SetSelectedColor(Color fgColor, Color bgColor) {
		if (!(SelectedFgColor == fgColor && SelectedBgColor == bgColor)) {
			SelectedFgColor = fgColor;
			SelectedBgColor = bgColor;

			InvalidateLayout(false);
		}
	}
	public void SetArmedColor(Color fgColor, Color bgColor) {
		if (!(ArmedFgColor == fgColor && ArmedBgColor == bgColor)) {
			ArmedFgColor = fgColor;
			ArmedBgColor = bgColor;

			InvalidateLayout(false);
		}
	}
	public void SetDepressedColor(Color fgColor, Color bgColor) {
		if (!(DepressedFgColor == fgColor && DepressedBgColor == bgColor)) {
			DepressedFgColor = fgColor;
			DepressedBgColor = bgColor;

			InvalidateLayout(false);
		}
	}

	public void SizeToContents() {
		GetContentSize(out int wide, out int tall);
		SetSize(wide + Label.Content, tall + Label.Content);
	}
	bool Paint;
	public void SetShouldPaint(bool paint) {
		Paint = paint;
	}
}
