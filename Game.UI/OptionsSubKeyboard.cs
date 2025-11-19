using Source.Common.Formats.Keyvalues;
using Source.Common.GUI;
using Source.Common.Input;
using Source.GUI.Controls;

namespace Game.UI;

public class OptionsSubKeyboard : PropertyPage
{
	struct KeyBinding
	{
		char[] Binding;
	}

	OptionsSubKeyboardAdvancedDlg? OptionsSubKeyboardAdvancedDlg;
	ControlsListPanel KeyBindList;
	Button SetBindingButton;
	Button ClearBindingButton;

	KeyBinding[] KeyBindings = new KeyBinding[(int)ButtonCode.Last];

	List<string> KeysToUnbind = new();

	public OptionsSubKeyboard(Panel? parent, string? name) : base(parent, name) {
		for (int i = 0; i < KeyBindings.Length; i++)
			KeyBindings[i] = new();

		CreateKeyBindingList();
		SaveCurrentBindings();
		ParseActionDescriptions();

		SetBindingButton = new(this, "ChangeKeyButton", "");
		ClearBindingButton = new(this, "ClearKeyButton", "");

		LoadControlSettings("resource/OptionsSubKeyboard.res");

		SetBindingButton.SetEnabled(false);
		ClearBindingButton.SetEnabled(false);
	}

	~OptionsSubKeyboard() => DeleteSavedBindings();

	public override void OnResetData() {
		FillInCurrentBindings();
		if (IsVisible())
			KeyBindList.SetSelectedItem(0);
	}

	public override void OnApplyChanges() => ApplyAllBindings();

	public void CreateKeyBindingList() {
		KeyBindList = new(this, "listpanel_keybindlist");
	}

	public override void OnKeyCodeTyped(ButtonCode code) {
		if (code == ButtonCode.KeyEnter)
			OnCommand("ChangeKey");
		else
			base.OnKeyCodeTyped(code);
	}

	public override void OnCommand(ReadOnlySpan<char> command) {
		if (command.Equals("Defaults", StringComparison.OrdinalIgnoreCase)) {
			var box = new QueryBox("#GameUI_KeyboardSettings", "#GameUI_KeyboardSettingsText");
			box.AddActionSignalTarget(this);
			box.SetOKCommand(new KeyValues("Command", "command", "DefaultsOK"));
			box.DoModal();
		}
		else if (command.Equals("DefaultsOK", StringComparison.OrdinalIgnoreCase)) {
			FillInDefaultBindings();
			KeyBindList.RequestFocus();
		}
		else if (!KeyBindList.IsCapturing() && command.Equals("ChangeKey", StringComparison.OrdinalIgnoreCase))
			KeyBindList.StartCaptureMode(CursorCode.Blank);
		else if (!KeyBindList.IsCapturing() && command.Equals("ClearKey", StringComparison.OrdinalIgnoreCase)) {
			OnKeyCodePressed(ButtonCode.KeyDelete);
			KeyBindList.RequestFocus();
		}
		else if (command.Equals("Advanced", StringComparison.OrdinalIgnoreCase))
			OpenKeyboardAdvancedDialog();
		else
			base.OnCommand(command);
	}

	public void ParseActionDescriptions() {

	}

	public void GetItemForBinding() {

	}

	public void AddBinding() {

	}

	public void ClearBindItems() {

	}

	public void RemoveKeyFromBindItems() {

	}

	public void FillInCurrentBindings() {

	}

	public void DeleteSavedBindings() {

	}

	public void SaveCurrentBindings() {

	}

	public void BindKey() {

	}

	public void UnbindKey() {

	}

	public void ApplyAllBindings() {

	}

	public void FillInDefaultBindings() {

	}

	public void ItemSelected(int itemID) {

	}

	public void Finish() {

	}

	public override void OnThink() {
		base.OnThink();

	}

	public override void OnKeyCodePressed(ButtonCode code) {
		base.OnKeyCodePressed(code);

	}

	public void OpenKeyboardAdvancedDialog() {
		if (OptionsSubKeyboardAdvancedDlg == null)
			OptionsSubKeyboardAdvancedDlg = new(GetParent());

		OptionsSubKeyboardAdvancedDlg.Activate();
	}

	public override void OnMessage(KeyValues message, IPanel? from) {
		if (message.Name == "ItemSelected") {
			ItemSelected(message.GetInt("itemID", -1));
			return;
		}

		base.OnMessage(message, from);
	}

	char[] Util_CopyString(ReadOnlySpan<char> input) {
		var arr = new char[input.Length];
		input.CopyTo(arr);
		return arr;
	}

	readonly string[] VaBuf = new string[4];
	int VaIndex;

	ReadOnlySpan<char> Util_va(string fmt, params object[] args) {
		VaIndex = (VaIndex + 1) & 3;
		VaBuf[VaIndex] = string.Format(fmt, args);
		return VaBuf[VaIndex];
	}
}

class OptionsSubKeyboardAdvancedDlg : Frame
{
	public OptionsSubKeyboardAdvancedDlg(Panel? parent) : base(null, null) {
		SetTitle("#GameUI_KeyboardAdvanced_Title", true);
		SetSize(280, 140);
		LoadControlSettings("resource/OptionsSubKeyboardAdvancedDlg.res");
		MoveToCenterOfScreen();
		SetSizeable(false);
		SetDeleteSelfOnClose(true);
	}
}