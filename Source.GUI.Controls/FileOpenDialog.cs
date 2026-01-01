using Source.Common.Formats.Keyvalues;
using Source.Common.GUI;
using Source.Common.Input;

namespace Source.GUI.Controls;

enum FileOpenDialogType
{
	Save = 0,
	Open,
	SelectDirectory
}

class FileCompletionMenu : Menu
{
	public FileCompletionMenu(Panel? parent, ReadOnlySpan<char> panelName) : base(parent, panelName) {

	}

	// override it so it doesn't request focus
	public override void SetVisible(bool state) => Visible = state;
}

class FileCompletionEdit : TextEntry
{
	FileCompletionMenu DropDown;

	public FileCompletionEdit(Panel? parent, ReadOnlySpan<char> panelName) : base(parent, panelName) {

	}

	// int AddItem(ReadOnlySpan<char> itemText, KeyValues userData) { }

	void DeleteAllItems() { }

	// int GetItemCount() { }

	// int GetItemIDFromRow(int row) { }

	// int GetRowFromItemID(int itemID) { }

	public override void PerformLayout() { }

	public override void OnSetText(ReadOnlySpan<char> newtext) { }

	void OnKillFocus() { }

	void HideMenu() { }

	void ShowMenu() { }

	public override void OnKeyCodeTyped(ButtonCode code) { }

	void OnMenuItemHighlight(int itemID) { }
}

class FileOpenDialog : Frame
{
	ComboBox FullPathEdit;
	ListPanel FileList;
	FileCompletionEdit FileNameEdit;
	ComboBox FileTypeCombo;
	Button OpenButton;
	Button CancelButton;
	Button FolderUpButton;
	Button NewFolderButton;
	Button OpenInExplorerButton;
	ImagePanel FolderIcon;
	Label FileTypeLabel;
	KeyValues ContextKeyValues;
	string LastPath;
	uint StartDirContext;
	FileOpenDialogType DialogType;
	bool FileSelected;
	IPanel SaveModal;
	InputDialog InputDialog;

	public FileOpenDialog(Panel? parent, ReadOnlySpan<char> panelName) : base(parent, panelName) {

	}

	void Init(ReadOnlySpan<char> title, KeyValues contextKeyValues) { }

	public override void ApplySchemeSettings(IScheme scheme) { }

	public override void OnKeyCodeTyped(ButtonCode code) { }

	void PopulateDriveList() { }

	public override void OnClose() { }

	void OnFolderUp() { }

	void OnInputCompleted(KeyValues data) { }

	void OnInputCanceled() { }

	void OnNewFolder() { }

	void OnOpenInExplorer() { }

	public override void OnCommand(ReadOnlySpan<char> command) { }

	void SetStartDirectoryContext(ReadOnlySpan<char> startDirContext, ReadOnlySpan<char> defaultDir) { }

	void SetStartDirectory(ReadOnlySpan<char> dir) { }

	void AddFilter(ReadOnlySpan<char> filter, ReadOnlySpan<char> filterName, bool active, ReadOnlySpan<char> filterInfo) { }

	void DoModal(bool unused) { }

	void GetCurrentDirectory(ReadOnlySpan<char> buf) { }

	void GetSelectedFileName(ReadOnlySpan<char> buf) { }

	void NewFolder(ReadOnlySpan<char> folderName) { }

	void MoveUpFolder() { }

	void ValidatePath() { }

	void PopulateFileList() { }

	// bool ExtensionMatchesFilter(ReadOnlySpan<char> ext) { }

	void ChooseExtension(ReadOnlySpan<char> ext) { }

	void SaveFileToStartDirContext(ReadOnlySpan<char> fullPath) { }

	void PostFileSelectedMessage(ReadOnlySpan<char> fileName) { }

	void OnSelectFolder() { }

	void OnOpen() { }

	void PopulateFileNameCompletion() { }

	void OnItemSelected() { }

	void OnTextChanged(KeyValues kv) { }
}