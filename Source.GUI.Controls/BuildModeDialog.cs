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
		public PanelListPanel Controls;
		public KeyValues? ResourceData;

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
				item.EditLabel?.DeletePanel();
				item.EditPanel?.DeletePanel();
				item.Combo?.DeletePanel();
				item.EditButton?.DeletePanel();
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
	Divider Divider;

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
		SetMinimumSize(300, 256);
		SetSize(300, 420);
		CurrentPanel = null;
		EditableParents = null;
		EditableChildren = null;
		NextChild = null;
		PrevChild = null;
		BuildGroup = buildGroup;
		UndoSettings = null;
		CopySettings = null;
		AutoUpdate = false;
		MakePopup(true);
		SetTitle("VGUI Build Mode Editor", true);

		CreateControls();
		// LoadUserConfig("BuildModeDialog"); // TODO: System.GetUserConfigFileData

		// buildmodedialogmgr
	}

	~BuildModeDialog() {

	}

	public override void OnClose() {

	}

	public void CreateControls() {
		if (BuildGroup == null)
			return;

		int i;
		panelList = new();
		panelList.ResourceData = new KeyValues("BuildDialog");
		panelList.Controls = new(this, "BuildModeControls");

		FileSelectionCombo = new(this, "FileSelectionCombo", 10, false);
		for (i = 0; i < BuildGroup.GetRegisteredControlSettingsFileCount(); i++)
			FileSelectionCombo.AddItem(BuildGroup.GetRegisteredControlSettingsFileByIndex(i), null);

		if (FileSelectionCombo.GetItemCount() < 2)
			FileSelectionCombo.SetEnabled(false);

		int buttonH = 18;

		StatusLabel = new(this, "StatusLabel", "[nothing currently selected]");
		StatusLabel.SetTextColorState(ColorState.Dull);
		StatusLabel.SetTall(buttonH);

		Divider = new(this, "Divider");

		AddNewControlCombo = new(this, null, 30, false);
		AddNewControlCombo.SetSize(116, buttonH);
		AddNewControlCombo.SetOpenDirection(MenuDirection.DOWN);

		EditableParents = new(this, null, 15, false); //, true, buildGroup.GetContextPanel() // CBuildMoveNavCombo
		EditableParents.SetSize(116, buttonH);
		EditableParents.SetOpenDirection(MenuDirection.DOWN);

		EditableChildren = new(this, null, 15, false); //, true, buildGroup.GetContextPanel() // CBuildMoveNavCombo
		EditableChildren.SetSize(116, buttonH);
		EditableChildren.SetOpenDirection(MenuDirection.DOWN);

		NextChild = new(this, "NextChild", "Next");
		NextChild.SetCommand(new KeyValues("OnChildChanged", "direction", 1));

		PrevChild = new(this, "PrevChild", "Prev");
		PrevChild.SetCommand(new KeyValues("OnChildChanged", "direction", -1));

		int defaultItem = AddNewControlCombo.AddItem("None", null);

		List<ReadOnlyMemory<char>> names = [];
		// GetFactoryNames

		var sorted = new SortedSet<string>(StringComparer.Ordinal);
		foreach (var name in names) sorted.Add(name.ToString());
		foreach (var name in sorted) AddNewControlCombo.AddItem(name, null);

		AddNewControlCombo.ActivateItem(defaultItem);

		ExitButton = new(this, "ExitButton", "&Exit");
		ExitButton.SetSize(64, buttonH);

		SaveButton = new(this, "SaveButton", "&Save");
		SaveButton.SetSize(64, buttonH);

		ApplyButton = new(this, "ApplyButton", "&Apply");
		ApplyButton.SetSize(64, buttonH);

		ReloadLocalization = new(this, "Localization", "&Reload Localization");
		ReloadLocalization.SetSize(100, buttonH);

		ExitButton.SetCommand("Exit");
		SaveButton.SetCommand("Save");
		ApplyButton.SetCommand("Apply");
		ReloadLocalization.SetCommand(new KeyValues("ReloadLocalization"));//static

		DeleteButton = new(this, "DeletePanelButton", "Delete");
		DeleteButton.SetSize(64, buttonH);
		DeleteButton.SetCommand("DeletePanel");

		VarsButton = new(this, "VarsButton", "Variables");
		VarsButton.SetSize(72, buttonH);
		VarsButton.SetOpenDirection(MenuDirection.DOWN);

		// KeyValues vars = BuildGroup.GetDialogVariables();
		// if(vars)...
		VarsButton.SetEnabled(false);

		ApplyButton.SetTabPosition(1);
		panelList.Controls.SetTabPosition(2);
		VarsButton.SetTabPosition(3);
		DeleteButton.SetTabPosition(4);
		AddNewControlCombo.SetTabPosition(5);
		SaveButton.SetTabPosition(6);
		ExitButton.SetTabPosition(7);

		EditableParents.SetTabPosition(8);
		EditableChildren.SetTabPosition(9);

		PrevChild.SetTabPosition(10);
		NextChild.SetTabPosition(11);

		ReloadLocalization.SetTabPosition(12);
	}

	public override void ApplySchemeSettings(IScheme scheme) {
		base.ApplySchemeSettings(scheme);

	}

	public override void PerformLayout() {
		base.PerformLayout();

		int BORDER_GAP = 16, YGAP_SMALL = 4, YGAP_LARGE = 8, TITLE_HEIGHT = 24, BOTTOM_CONTROLS_HEIGHT = 145, XGAP = 6;

		GetSize(out int wide, out int tall);

		int xpos = BORDER_GAP;
		int ypos = BORDER_GAP + TITLE_HEIGHT;

		FileSelectionCombo.SetBounds(xpos, ypos, wide - (BORDER_GAP * 2), StatusLabel.GetTall());
		ypos += StatusLabel.GetTall() + YGAP_SMALL;

		StatusLabel.SetBounds(xpos, ypos, wide - (BORDER_GAP * 2), StatusLabel.GetTall());
		ypos += StatusLabel.GetTall() + YGAP_SMALL;

		panelList.Controls.SetPos(xpos, ypos);
		panelList.Controls.SetSize(wide - (BORDER_GAP * 2), tall - (ypos + BOTTOM_CONTROLS_HEIGHT));

		ypos = tall - BORDER_GAP;
		xpos = BORDER_GAP + VarsButton.GetWide() + DeleteButton.GetWide() + AddNewControlCombo.GetWide() + (XGAP * 2);

		ypos -= ApplyButton.GetTall();
		xpos -= ApplyButton.GetWide();
		ApplyButton.SetPos(xpos, ypos);

		xpos -= ExitButton.GetWide();
		xpos -= XGAP;
		ExitButton.SetPos(xpos, ypos);

		xpos -= SaveButton.GetWide();
		xpos -= XGAP;
		SaveButton.SetPos(xpos, ypos);

		xpos = BORDER_GAP;
		ypos -= YGAP_LARGE + Divider.GetTall();
		Divider.SetBounds(xpos, ypos, wide - (xpos + BORDER_GAP), 2);

		ypos -= YGAP_LARGE + VarsButton.GetTall();
		xpos = BORDER_GAP;

		EditableParents.SetPos(xpos, ypos);
		EditableChildren.SetPos(xpos + 150, ypos);

		ypos -= YGAP_LARGE + 18;
		xpos = BORDER_GAP;

		ReloadLocalization.SetPos(xpos, ypos);

		xpos += XGAP + ReloadLocalization.GetWide();

		PrevChild.SetPos(xpos, ypos);
		PrevChild.SetSize(64, ReloadLocalization.GetTall());
		xpos += XGAP + PrevChild.GetWide();

		NextChild.SetPos(xpos, ypos);
		NextChild.SetSize(64, ReloadLocalization.GetTall());

		ypos -= YGAP_LARGE + VarsButton.GetTall();
		xpos = BORDER_GAP;

		VarsButton.SetPos(xpos, ypos);
		xpos += XGAP + VarsButton.GetWide();
		DeleteButton.SetPos(xpos, ypos);
		xpos += XGAP + DeleteButton.GetWide();
		AddNewControlCombo.SetPos(xpos, ypos);
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
