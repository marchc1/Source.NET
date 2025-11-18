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

	}

	public override void OnApplyChanges() {

	}

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

	public void ItemSelected() {

	}

	public void Finish() {

	}

	public override void OnThink() {
		base.OnThink();

	}

	public override void OnKeyCodePressed(ButtonCode code) {
		base.OnKeyCodePressed(code);

	}

	public void OpenKeyboardAdvancedDlg() {
		if (OptionsSubKeyboardAdvancedDlg == null)
			OptionsSubKeyboardAdvancedDlg = new(GetParent());

		OptionsSubKeyboardAdvancedDlg.Activate();
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