using Source.Common.Commands;
using Source.Common.Formats.Keyvalues;
using Source.GUI.Controls;

namespace Game.UI;

public class OptionsSubMouse : PropertyPage
{
	CvarNegateCheckButton ReverseMouseCheckBox;
	CvarToggleCheckButton MouseFilterCheckBox;
	CvarToggleCheckButton MouseRawCheckBox;
	CheckButton MouseAccelerationCheckBox;
	CvarToggleCheckButton JoystickCheckBox;
	CvarToggleCheckButton JoystickSouthpawCheckBox;
	CvarToggleCheckButton QuickInfoCheckBox;
	CvarToggleCheckButton ReverseJoystickCheckBox;
	CvarSlider MouseSensitivitySlider;
	TextEntry MouseSensitivityLabel;
	CvarSlider MouseAccelExponentSlider;
	TextEntry MouseAccelExponentLabel;
	CvarSlider JoyYawSensitivitySlider;
	Label JoyYawSensitivityPreLabel;
	CvarSlider JoyPitchSensitivitySlider;
	Label JoyPitchSensitivityPreLabel;

	public OptionsSubMouse(Panel? parent, string? name) : base(parent, name) {
		ReverseMouseCheckBox = new(this, "ReverseMouse", "#GameUI_ReverseMouse", "m_pitch");
		MouseFilterCheckBox = new(this, "MouseFilter", "#GameUI_MouseFilter", "m_filter");
		MouseRawCheckBox = new(this, "MouseRaw", "#GameUI_MouseRaw", "m_rawinput");
		MouseAccelerationCheckBox = new(this, "MouseAccelerationCheckbox", "#GameUI_MouseCustomAccel");
		JoystickCheckBox = new(this, "Joystick", "#GameUI_Joystick", "joystick");
		JoystickSouthpawCheckBox = new(this, "JoystickSouthpaw", "#GameUI_JoystickSouthpaw", "joy_movement_stick");
		ReverseJoystickCheckBox = new(this, "ReverseJoystick", "#GameUI_ReverseJoystick", "joy_inverty");
		QuickInfoCheckBox = new(this, "HudQuickInfo", "#GameUI_HudQuickInfo", "hud_quickinfo");
		MouseSensitivitySlider = new(this, "Slider", "#GameUI_MouseSensitivity", 0.1f, 6.0f, "sensitivity", true);
		MouseSensitivityLabel = new(this, "SensitivityLabel");
		MouseSensitivityLabel.AddActionSignalTarget(this);
		MouseAccelExponentSlider = new(this, "MouseAccelerationSlider", "#GameUI_MouseAcceleration", 1.0f, 1.4f, "m_customaccel_exponent", true);
		MouseAccelExponentLabel = new(this, "MouseAccelerationLabel");
		MouseAccelExponentLabel.AddActionSignalTarget(this);
		JoyYawSensitivitySlider = new(this, "JoystickYawSlider", "#GameUI_JoystickYawSensitivity", -0.5f, -7.0f, "joy_yawsensitivity", true);
		JoyYawSensitivityPreLabel = new(this, "JoystickYawSensitivityPreLabel", "#GameUI_JoystickLookSpeedYaw");
		JoyPitchSensitivitySlider = new(this, "JoystickPitchSlider", "#GameUI_JoystickPitchSensitivity", 0.5f, 7.0f, "joy_pitchsensitivity", true);
		JoyPitchSensitivityPreLabel = new(this, "JoystickPitchSensitivityPreLabel", "#GameUI_JoystickLookSpeedPitch");

		LoadControlSettings("resource/OptionsSubMouse.res");
	}

	public override void OnResetData() {
		ReverseMouseCheckBox.Reset();
		MouseFilterCheckBox.Reset();
		MouseRawCheckBox.Reset();
		JoystickCheckBox.Reset();
		JoystickSouthpawCheckBox.Reset();
		ReverseJoystickCheckBox.Reset();
		QuickInfoCheckBox.Reset();
		MouseSensitivitySlider.Reset();
		MouseAccelExponentSlider.Reset();
		JoyYawSensitivitySlider.Reset();
		JoyPitchSensitivitySlider.Reset();

		ConVarRef var = new("m_customaccel");
		if (var.IsValid())
			MouseAccelerationCheckBox.SetSelected(var.GetBool());
	}

	public override void OnApplyChanges() {
		ReverseMouseCheckBox.ApplyChanges();
		MouseFilterCheckBox.ApplyChanges();
		MouseRawCheckBox.ApplyChanges();
		JoystickCheckBox.ApplyChanges();
		JoystickSouthpawCheckBox.ApplyChanges();
		ReverseJoystickCheckBox.ApplyChanges();
		QuickInfoCheckBox.ApplyChanges();
		MouseSensitivitySlider.ApplyChanges();
		MouseAccelExponentSlider.ApplyChanges();
		JoyYawSensitivitySlider.ApplyChanges();
		JoyPitchSensitivitySlider.ApplyChanges();

		// engine.ClientCmd_Unrestricted("jpyadvancedupdate");

		ConVarRef var = new("m_customaccel");
		if (var.IsValid())
			var.SetValue(MouseAccelerationCheckBox.IsSelected() ? 3 : 0);
	}

	public void OnControlModified(Panel panel) {
		PostActionSignal(new KeyValues("ApplyButtonEnable"));

		if (panel == MouseSensitivitySlider && MouseAccelExponentSlider.HasBeenModified())
			UpdateSensitivityLabel();
		else if (panel == MouseAccelExponentSlider && MouseAccelExponentSlider.HasBeenModified())
			UpdateAccelerationLabel();
		// else if (panel == JoystickCheckBox)
			// UpdateJoystickPanels();
		else if (panel == MouseAccelerationCheckBox) {
			MouseAccelExponentSlider.SetEnabled(MouseAccelerationCheckBox.IsSelected());
			MouseAccelExponentLabel.SetEnabled(MouseAccelerationCheckBox.IsSelected());
		}
	}

	private void UpdateSensitivityLabel() {
		Span<char> buf = stackalloc char[64];
		string formatted = $" {MouseSensitivitySlider.GetSliderValue():F2}";
		formatted.AsSpan().CopyTo(buf);
		MouseSensitivityLabel.SetText(buf);
	}

	public void UpdateAccelerationLabel() {
		Span<char> buf = stackalloc char[64];
		string formatted = $" {MouseAccelExponentSlider.GetSliderValue():F2}";
		formatted.AsSpan().CopyTo(buf);
		MouseAccelExponentLabel.SetText(buf);
	}

	//todo
}