using Source.Common.GUI;
using Source.GUI.Controls;

class DirectoryTreeView : TreeView
{
	DirectorySelectDialog Parent;
	public DirectoryTreeView(Panel? parent, ReadOnlySpan<char> panelName) : base(parent, panelName) {

	}

	public override void GenerateChildrenOfNode(int itemIndex) { }
}

class CreateDirectoryDialog : Frame
{
	Button OKButton;
	Button CancelButton;
	TextEntry NameEntry;
	IPanel PrevAppFocusPanel;

	public CreateDirectoryDialog(Panel? parent, ReadOnlySpan<char> panelName) : base(parent, panelName) {

	}

	public override void PerformLayout() { }

	public override void OnCommand(ReadOnlySpan<char> command) { }

	public override void OnClose() { }
}

// messages: TextChanged, TreeViewItemSelected, CreateDirectory
class DirectorySelectDialog : Frame
{
	string CurrentDir;
	string DefaultCreateDirName;
	string CurrentDrive;
	TreeView DirTree;
	ComboBox DriveCombo;
	Button CancelButton;
	Button SelectButton;
	Button CreateButton;

	public DirectorySelectDialog(Panel? parent, ReadOnlySpan<char> panelName) : base(parent, panelName) {

	}

	public override void PerformLayout() { }

	public override void ApplySchemeSettings(IScheme scheme) { }

	void ExpandTreeToPath(ReadOnlySpan<char> path, bool selectFinalDirectory = true) { }

	void SetStartDirectory(ReadOnlySpan<char> path) { }

	void SetDefaultCreateDirectoryName(ReadOnlySpan<char> defaultCreateDirName) { }

	public override void DoModal() { }

	void BuildDriveChoices() { }

	void BuildDirTree() { }

	void ExpandTreeNode(ReadOnlySpan<char> path, int parentNodeIndex) { }

	// bool DoesDirectoryHaveSubdirectories(ReadOnlySpan<char> path, ReadOnlySpan<char> dir) { }

	void GenerateChildrenOfDirectoryNode(int nodeIndex) { }

	void GenerateFullPathForNode(int nodeIndex, char path, int pathBufferSize) { }

	void OnTextChanged() { }

	void OnCreateDirectory(ReadOnlySpan<char> dir) { }

	public override void OnClose() { }

	public override void OnCommand(ReadOnlySpan<char> command) { }

	void OnTreeViewItemSelected() { }
}