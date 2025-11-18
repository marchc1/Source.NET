using Source.Common.Formats.Keyvalues;
using Source.Common.GUI;
using Source.Common.Input;

namespace Source.GUI.Controls;

public struct PanelItem
{
	public Panel? EditLabel;
	public TextEntry? EditPanel;
	public ComboBox? Combo;
	public Button? EditButton;
	public string Name;
	public int Type;

	public PanelItem() {
		EditLabel = null;
		EditPanel = null;
		Combo = null;
		EditButton = null;
		Name = string.Empty;
		Type = 0;
	}
}

class SmallTextEntry : TextEntry
{
	public SmallTextEntry(Panel parent, ReadOnlySpan<char> name) : base(parent, name) { }

	public override void ApplySchemeSettings(IScheme scheme) {
		base.ApplySchemeSettings(scheme);
		SetFont(scheme.GetFont("DefaultVerySmall"));
	}
}

public class BuildModeDialog : Frame
{
	public static Panel Create_BuildModeDialog() => new BuildModeDialog(null);

	class PanelList
	{
		List<PanelItem> panelList = [];
		// PanelListPanel Controls;
		KeyValues? ResourceData;

		public void AddItem(Panel label, TextEntry edit, ComboBox combo, Button button, ReadOnlySpan<char> name, Type type) {
			PanelItem item = new();
			item.EditLabel = label;
			item.EditPanel = edit;
			item.Name = name.ToString();
			item.Type = (int)type;
			item.Combo = combo;
			item.EditButton = button;
			panelList.Add(item);
		}

		public void RemoveAll() {
			for (int i = 0; i < panelList.Count; i++) {
				PanelItem item = panelList[i];
				if (item.EditLabel != null)
					item.EditLabel.DeletePanel();
				if (item.EditPanel != null)
					item.EditPanel.DeletePanel();
				if (item.Combo != null)
					item.Combo.DeletePanel();
				if (item.EditButton != null)
					item.EditButton.DeletePanel();
			}

			panelList.Clear();
			// Controls.Clear();
		}
	}

	static Dictionary<string, KeyValues> CachedResFiles = [];

	Panel CurrentPanel;
	BuildGroup BuildGroup;
	Label StatusLabel;
	ComboBox FileSelectionCombo;
	// Divider Divider;

	PanelList panelList;

	Button SaveButton;
	Button ApplyButton;
	Button ExitButton;
	Button DeleteButton;
	Button ReloadLocalization;
	MenuButton VarsButton;

	bool AutoUpdate;

	ComboBox AddNewControlCombo;
	KeyValues UndoSettings;
	KeyValues CopySettings;
	char[] CopyClassName;
	int Click1;
	int Click2;

	enum Type
	{
		String,
		Integer,
		Color,
		Alignment,
		AutoResize,
		Corner,
		LocalizedString
	}

	Menu ContextMenu;

	ComboBox EditableParents;
	ComboBox EditableChildren;

	Button NextChild;
	Button PrevChild;

	public BuildModeDialog(BuildGroup buildGroup) : base(buildGroup?.GetContextPanel(), "BuildModeDialog") {

	}

	~BuildModeDialog() {

	}

	public override void OnClose() {

	}

	public void CreateControls() {

	}

	public override void ApplySchemeSettings(IScheme scheme) {
		base.ApplySchemeSettings(scheme);

	}

	public override void PerformLayout() {
		base.PerformLayout();

	}

	public void RemoveAllControls() {

	}

	public void OnTextKillFocus() {

	}

	public void SetActiveControl(Panel controlToEdit) {

	}

	public void UpdateControlData(Panel control) {

	}

	public void UpdateEditControl(PanelItem panelItem, ReadOnlySpan<char> datString) {

	}

	public override void OnCommand(ReadOnlySpan<char> command) {

		base.OnCommand(command);
	}

	public void OnDeletePanel() {

	}

	public void ApplyDataToControls() {

	}

	public void StoreUndoSettings() {

	}

	public void DoUndo() {

	}

	public void DoCopy() {

	}

	public void DoPaste() {

	}

	public KeyValues StoreSettings() {
		return new();
	}

	public override void OnKeyCodeTyped(ButtonCode code) {

	}

	public void ExitBuildMode() {

	}

	public Panel OnNewControl(ReadOnlySpan<char> name, int x, int y) {
		return null!;
	}

	public void EnableSaveButton() => SaveButton.SetEnabled(true);

	public void RevertToSaved() {

	}

	public void ShowHelp() {

	}

	public void ShutdownBuildMode() {

	}

	public void OnPanelMoved() {

	}

	public void OnSetClipboardText(ReadOnlySpan<char> text) {

	}

	public void OnCreateNewControl(ReadOnlySpan<char> text) {

	}

	public void OnShowNewControlMenu() {

	}

	public void OnReloadLocalizaton() {

	}

	public override bool IsBuildGroupEnabled() => false; // Don't ever edit the actual build dialog!!!

	public void OnChangeChild() {

	}
}
