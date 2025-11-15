using Source.Common.Input;
using Source.GUI.Controls;

namespace Game.UI;

public class OptionsSubKeyboard : PropertyPage
{
	struct KeyBinding {
		string Binding;
	}

	// ControlsListPanel KeyBindList;
	Button SetBindingButton;
	Button ClearBindingButton;

	KeyBinding[] KeyBindings = new KeyBinding[(int)ButtonCode.Last];

	List<string> KeysToUnbind = new();

	public OptionsSubKeyboard(Panel? parent, string? name) : base(parent, name) {
		for (int i = 0; i < KeyBindings.Length; i++)
			KeyBindings[i] = new();

		// CreateKeyBindingList();
		// SaveCurrentBindings();
		// ParseActionDescriptions();

		SetBindingButton = new(this, "ChangeKeyButton", "");
		ClearBindingButton = new(this, "ClearKeyButton", "");

		LoadControlSettings("resource/OptionsSubKeyboard.res");

		SetBindingButton.SetEnabled(false);
		ClearBindingButton.SetEnabled(false);
	}

	~OptionsSubKeyboard() {
		// DeleteSavedBindings();
	}
}