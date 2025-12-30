using Source.Common.GUI;
using Source.Common.Utilities;

namespace Source.GUI.Controls;

class TreeViewListControl : Panel
{
	const int CT_HEADER_LEFTALIGN = 0x0001;
	TreeView Tree;

	class ColumnInfo
	{
		public ColumnInfo() => Width = Left = Right = Flags = 0;
		UtlSymbol Title;
		int Width;
		int Left;
		int Right;
		int Flags;
	}
	List<ColumnInfo> Columns;
	IFont TitleBarFont;
	int TitleBarHeight;
	List<int> Rows;
	Color BorderColor;

	public TreeViewListControl(Panel? parent, ReadOnlySpan<char> panelName) : base(parent, panelName) {

	}

	void SetTreeView(TreeView tree) { }

	// TreeView GetTree() { }

	// int GetTitleBarHeight() { }

	void SetTitleBarInfo(IFont font, int titleBarHeight) { }

	void SetBorderColor(Color color) { }

	void SetNumColumns(int nColumns) { }

	// int GetNumColumns() { }

	void SetColumnInfo(int column, ReadOnlySpan<char> title, int width, int flags) { }

	// int GetNumRows() { }

	// int GetTreeItemAtRow(int row) { }

	void GetGridElementBounds(int column, int row, int left, int top, int right, int bottom) { }

	public override void PerformLayout() { }

	void RecalculateRows() { }

	void RecalculateRows_R(int index) { }

	// int GetScrollBarSize() { }

	void RecalculateColumns() { }

	public override void PostChildPaint() { }

	public override void Paint() { }

	void DrawTitleBars() { }
}