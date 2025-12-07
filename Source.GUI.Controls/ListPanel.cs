using Source;
using Source.Common.Formats.Keyvalues;
using Source.Common.GUI;
using Source.Common.Input;
using Source.GUI.Controls;

class ColumnButton : Button
{
	public ColumnButton(Panel parent, ReadOnlySpan<char> name, ReadOnlySpan<char> text) : base(parent, name, text) {

	}
}

class Dragger : Panel
{
	public Dragger(int column) {

	}
}


class FastSortListPanelItem : ListPanelItem
{
	public List<int> SortedTreeIndexes = [];
	public bool visible;
	int primarySortIndexValue;
	int secondarySortIndexValue;
}

public class ListPanelItem
{
	public KeyValues? kv;
	public uint UserData;
	public KeyValues? DragData;
	public bool Image;
	public int ImageIndex;
	public int ImageIndexSelected;
	public IImage? Icon;

	public ListPanelItem() {
		kv = null;
		UserData = 0;
		DragData = null;
		Image = false;
		ImageIndex = -1;
		ImageIndexSelected = -1;
		Icon = null;
	}
}

public class ListPanel : Panel
{
	[Flags]
	public enum ColumnFlags
	{
		FixedSize = 0x01,
		ResizeWithWindow = 0x02,
		Image = 0x04,
		Hidden = 0x08,
		Unhidable = 0x10
	}
	struct IndexItem_t
	{
		public ListPanelItem DataItem;
		public int DuplicateIndex;
	}

	public delegate int SortFunc(ListPanel panel, ListPanelItem item1, ListPanelItem item2);

	class Column
	{
		public Button Header;
		public int MinWidth;
		public int MaxWidth;
		public bool ResizesWithWindow;
		public Panel Resizer;
		public SortFunc? SortFunc;
		public bool TypeIsText;
		public bool Hidden;
		public bool Unhidable;
		public SortedDictionary<int, IndexItem_t> SortedTree;
		public int ContentAlignment;
	}
	List<Column> ColumnsData = [];
	List<int> ColumnsHistory = [];
	List<int> CurrentColumns = [];
	int ColumnDraggerMoved;
	int LastBarWidth;
	Dictionary<int, FastSortListPanelItem> DataItems = [];
	private int NextItemID = 0;
	List<int> VisibleItems = [];
	int SortColumn;
	int SortColumnSecondary;
	TextImage TextImage;
	ImagePanel ImagePanel;
	Label Label;
	ScrollBar Hbar;
	ScrollBar Vbar;
	int SelectedColumn;
	bool NeedsSort;
	bool SortAscending;
	bool SortAscendingSecondary;
	bool CanSelectIndividualCells;
	bool ShiftHeldDown;
	bool MultiselectEnabled;
	bool AllowUserAddDeleteColumns;
	bool DeleteImageListWhenDone;
	bool IgnoreDoubleClick;
	int HeaderHeight;
	int RowHeight;
	List<int> SelectedItems = [];
	int LastSelectedItem;
	int TableStartX;
	int TableStartY;
	Color LabelFgColor;
	Color DisabledColor;
	Color SelectionFgColor;
	Color DisabledSelectionFgColor;
	ImageList? ImageList;
	TextImage EmptyListText;
	Panel? EditModePanel;
	int EditModeItemID;
	int EditModeColumn;
	int UserConfigFileVersion;

	public ListPanel(Panel parent, ReadOnlySpan<char> name) : base(parent, name) {
		IgnoreDoubleClick = false;
		MultiselectEnabled = true;
		EditModeItemID = 0;
		EditModeColumn = 0;

		HeaderHeight = 20;
		RowHeight = 20;
		CanSelectIndividualCells = false;
		SelectedColumn = -1;
		AllowUserAddDeleteColumns = false;

		Hbar = new(this, "HorizScrollBar", false);
		Hbar.AddActionSignalTarget(this);
		Hbar.SetVisible(false);
		Vbar = new(this, "VertScrollBar", true);
		Vbar.AddActionSignalTarget(this);
		Vbar.SetVisible(false);

		Label = new(this, null, "");
		Label.SetVisible(false);
		Label.SetPaintBackgroundEnabled(false);
		Label.SetContentAlignment(Alignment.West);

		TextImage = new("");
		ImagePanel = new(this, "ListImage");
		ImagePanel.SetAutoDelete(true);

		SortColumn = -1;
		SortColumnSecondary = -1;
		SortAscending = true;
		SortAscendingSecondary = true;

		LastBarWidth = 0;
		ColumnDraggerMoved = -1;
		NeedsSort = false;
		LastSelectedItem = -1;

		ImageList = null;
		DeleteImageListWhenDone = false;
		EmptyListText = new("");

		UserConfigFileVersion = 1;
	}


	// bool RBTreeLessFunc(IndexItem_t item1, IndexItem_t item2) {

	// }

	void SetImageList(ImageList imageList, bool deleteImageListWhenDone) {

	}

	void SetColumnHeaderHeight(int height) {

	}

	public void AddColumnHeader(int index, ReadOnlySpan<char> columnName, ReadOnlySpan<char> columnText, int width, int columnFlags) {
		if ((columnFlags & (int)ColumnFlags.FixedSize) != 0 && (columnFlags & (int)ColumnFlags.ResizeWithWindow) == 0)
			AddColumnHeader(index, columnName, columnText, width, width, width, (ColumnFlags)columnFlags);
		else
			AddColumnHeader(index, columnName, columnText, width, 20, 10000, (ColumnFlags)columnFlags);
	}

	public void AddColumnHeader(int index, ReadOnlySpan<char> columnName, ReadOnlySpan<char> columnText, int width, int minWidth, int maxWidth, ColumnFlags columnFlags) {
		Assert(minWidth <= width);
		Assert(maxWidth >= width);

		Column column = new();
		ColumnsData.Add(column);
		int columnIndex = ColumnsData.Count - 1;

		ColumnsHistory.Add(columnIndex);

		if (index >= 0 && index < CurrentColumns.Count)
			CurrentColumns.Insert(index, columnIndex);
		else
			CurrentColumns.Add(columnIndex);

		ColumnButton button = new(this, columnName, columnText);
		button.MakeReadyForUse();
		button.SetSize(width, 24);
		button.AddActionSignalTarget(this);
		button.SetContentAlignment(Alignment.West);
		button.SetTextInset(5, 0);

		column.Header = button;
		column.MinWidth = minWidth;
		column.MaxWidth = maxWidth;
		column.ResizesWithWindow = (columnFlags & ColumnFlags.ResizeWithWindow) != 0;
		column.Hidden = false;
		column.Unhidable = (columnFlags & ColumnFlags.Unhidable) != 0;
		column.ContentAlignment = (int)Alignment.West;

		Dragger dragger = new(index);
		dragger.SetParent(this);
		dragger.AddActionSignalTarget(this);
		dragger.MoveToFront();
		// if (minWidth == maxWidth || (columnFlags & ColumnFlags.FixedSize) != 0)
		// dragger.SetMovable(false);
		column.Resizer = dragger;

		column.SortFunc = null;
		// column.SortedTree.SetLessFunc(RBTreeLessFunc);

		ResetColumnHeaderCommands();
		ResortColumnRBTree(index);
		Vbar.MoveToFront();
		SetColumnVisible(index, (columnFlags & ColumnFlags.Hidden) == 0);

		InvalidateLayout();
	}

	void ResortColumnRBTree(int col) {

	}

	void ResetColumnHeaderCommands() {

	}

	void SetColumnHeaderText(int col, ReadOnlySpan<char> text) {

	}


	void SetColumnTextAlignment(int col, int align) {

	}

	void SetColumnHeaderImage(int column, int imageListIndex) {

	}

	void SetColumnHeaderTooltip(int column, ReadOnlySpan<char> tooltipText) {

	}

	// int GetNumColumnHeaders() {

	// }

	// bool GetColumnHeaderText(int index, char pOut, int maxLen) {

	// }

	void SetColumnSortable(int col, bool sortable) {

	}

	void SetColumnVisible(int col, bool visible) {

	}

	void RemoveColumn(int col) {

	}

	// int FindColumn(ReadOnlySpan<char> columnName) {

	// }

	public int AddItem(KeyValues item, uint userData, bool scrollToItem, bool sortOnAdd) {
		FastSortListPanelItem newItem = new();
		newItem.kv = item.MakeCopy();
		newItem.UserData = userData;
		newItem.DragData = null;
		newItem.Image = newItem.kv.GetInt("image") != 0;
		newItem.ImageIndex = newItem.kv.GetInt("image");
		newItem.ImageIndexSelected = newItem.kv.GetInt("imageSelected");
		newItem.Icon = (IImage?)newItem.kv.GetPtr("iconImage");

		int itemID = NextItemID++;
		DataItems.Add(itemID, newItem);
		VisibleItems.Add(itemID);
		int displayedRow = VisibleItems.Count - 1;
		newItem.visible = true;

		IndexItem(itemID);

		if (sortOnAdd)
			NeedsSort = true;

		InvalidateLayout();

		if (scrollToItem)
			Vbar.SetValue(displayedRow);

		return itemID;
	}

	void SetUserData(int itemID, uint userData) {

	}

	// int GetItemIDFromUserData(uint userData) {

	// }

	public int GetItemCount() => VisibleItems.Count;

	public int GetItem(ReadOnlySpan<char> itemName) {
		foreach (var kvp in DataItems) {
			if (kvp.Value.kv != null && kvp.Value.kv.GetString("name").SequenceEqual(itemName))
				return kvp.Key;
		}
		return -1;
	}

	public KeyValues? GetItem(int itemID) {
		if (!DataItems.TryGetValue(itemID, out var item))
			return null;
		return item.kv;
	}

	// int GetItemCurrentRow(int itemID) {

	// }

	void SetItemDragData(int itemID, KeyValues data) {

	}

	void OnCreateDragData(KeyValues msg) {

	}

	public int GetItemIDFromRow(int currentRow) {
		if (currentRow < 0 || currentRow >= VisibleItems.Count)
			return -1;
		return VisibleItems[currentRow];
	}

	// int FirstItem() {

	// }

	// int NextItem(int iItem) {

	// }

	// int InvalidItemID() {

	// }

	public bool IsValidItemID(int itemID) {
		return DataItems.ContainsKey(itemID);
	}

	// ListPanelItem GetItemData(int itemID) {

	// }

	// uint GetItemUserData(int itemID) {

	// }

	void ApplyItemChanges(int itemID) {

	}

	void IndexItem(int itemID) {

	}

	void RereadAllItems() {

	}

	void CleanupItem(FastSortListPanelItem? data) {
		if (data == null)
			return;

		data.kv = null;
		data.DragData = null;
	}

	public void RemoveItem(int itemID) {
		if (!DataItems.TryGetValue(itemID, out var data))
			return;

		for (int i = 0; i < ColumnsHistory.Count; i++) {
			int colIndex = ColumnsHistory.ElementAt(i);
			if (colIndex == -1 || colIndex >= ColumnsData.Count)
				continue;

			Column column = ColumnsData.ElementAt(colIndex);
			if (data.SortedTreeIndexes == null || i >= data.SortedTreeIndexes.Count)
				continue;

			int key = data.SortedTreeIndexes[i];
			column.SortedTree.Remove(key);
		}

		SelectedItems.Remove(itemID);
		PostActionSignal(new KeyValues("ItemDeselected"));
		VisibleItems.Remove(itemID);

		DataItems.Remove(itemID);
		CleanupItem(data);
		InvalidateLayout();
	}

	public void RemoveAll() {
		for (int i = 0; i < ColumnsHistory.Count; i++) {
			int colIndex = ColumnsHistory[i];
			if (colIndex < 0 || colIndex >= ColumnsData.Count)
				continue;

			Column column = ColumnsData[colIndex];
			if (column?.SortedTree != null)
				column.SortedTree.Clear();
		}

		foreach (var item in DataItems.Values)
			CleanupItem(item);

		DataItems.Clear();
		NextItemID = 0;
		VisibleItems.Clear();

		InvalidateLayout();
	}


	public void DeleteAllItems() => RemoveAll();

	void ResetScrollBar() {

	}

	// int GetSelectedItemsCount() {

	// }

	// int GetSelectedItem(int selectionIndex) {

	// }

	// int GetSelectedColumn() {

	// }

	// void ClearSelectedItems() {

	// }

	// bool IsItemSelected(int itemID) {

	// }

	void AddSelectedItem(int itemID) {

	}

	void SetSingleSelectedItem(int itemID) {

	}

	void SetSelectedCell(int itemID, int col) {

	}

	void GetCellText(int itemID, int col, Span<char> buffer, int bufferSizeInBytes) {

	}

	Label GetCellRenderer(int itemID, int col) {
		Assert(TextImage != null);
		Assert(ImagePanel != null);

		Column column = ColumnsData.ElementAt(col);
		IScheme scheme = GetScheme()!;

		Label.SetContentAlignment((Alignment)column.ContentAlignment);

		if (column.TypeIsText) {
			Span<char> tempText = stackalloc char[256];

			GetCellText(itemID, col, tempText, 256);
			KeyValues item = GetItem(itemID)!;
			TextImage.SetText(tempText);
			TextImage.GetContentSize(out int cw, out int tall);

			Panel header = column.Header;
			int wide = header.GetWide();
			TextImage.SetSize(Math.Min(cw, wide - 5), tall);

			Label.SetTextImageIndex(0);
			Label.SetImageAtIndex(0, TextImage, 3);

			bool selected = false;
			if (SelectedItems.Contains(itemID) && (!CanSelectIndividualCells || col == SelectedColumn)) {
				selected = true;
				IPanel? focus = Input.GetFocus();
				if (HasFocus() || (focus != null && focus.HasParent(this)))
					Label.SetBgColor(GetSchemeColor("ListPanel.SelectedBgColor", scheme));
				else
					Label.SetBgColor(GetSchemeColor("ListPanel.SelectedOutOfFocusBgColor", scheme));

				if (item.IsEmpty("cellcolor") == false)
					TextImage.SetColor(item.GetColor("cellcolor"));
				else if (item.GetInt("disabled", 0) == 0)
					TextImage.SetColor(SelectionFgColor);
				else
					TextImage.SetColor(DisabledSelectionFgColor);

				Label.SetPaintBackgroundEnabled(true);
			}
			else {
				if (item.IsEmpty("cellcolor") == false)
					TextImage.SetColor(item.GetColor("cellcolor"));
				else if (item.GetInt("disabled", 0) == 0)
					TextImage.SetColor(LabelFgColor);
				else
					TextImage.SetColor(DisabledColor);
				Label.SetPaintBackgroundEnabled(false);
			}

			FastSortListPanelItem listItem = DataItems[itemID];
			if (col == 0 && listItem.Image && ImageList != null) {
				IImage? Image = null;
				if (listItem.Icon != null)
					Image = listItem.Icon;
				else {
					int imageIndex = selected ? listItem.ImageIndexSelected : listItem.ImageIndex;
					if (ImageList.IsValidIndex(imageIndex))
						Image = ImageList.GetImage(imageIndex);
				}

				if (Image != null) {
					Label.SetTextImageIndex(1);
					Label.SetImageAtIndex(0, Image, 0);
					Label.SetImageAtIndex(1, TextImage, 3);
				}
			}

			return Label;
		}
		else {
			if (SelectedItems.Contains(itemID) && (!CanSelectIndividualCells || col == SelectedColumn)) {
				IPanel? focus = Input.GetFocus();
				if (HasFocus() || (focus != null && focus.HasParent(this)))
					Label.SetBgColor(GetSchemeColor("ListPanel.SelectedBgColor", scheme));
				else
					Label.SetBgColor(GetSchemeColor("ListPanel.SelectedOutOfFocusBgColor", scheme));
				Label.SetPaintBackgroundEnabled(true);
			}
			else
				Label.SetPaintBackgroundEnabled(false);

			// IImage pIImage = GetCellImage(itemID, col);
			// Label.SetImageAtIndex(0, pIImage, 0);

			return Label;
		}
	}

	const int WINDOW_BORDER_WIDTH = 2;
	public override void PerformLayout() {
		if (CurrentColumns.Count == 0)
			return;

		if (NeedsSort)
			SortList();

		int rowsperpage = (int)GetRowsPerPage();
		int visibleItemCount = VisibleItems.Count;

		Vbar.SetVisible(true);
		Vbar.SetEnabled(false);
		Vbar.SetRangeWindow(rowsperpage);
		Vbar.SetRange(0, visibleItemCount);
		Vbar.SetButtonPressedScrollValue(1);

		GetSize(out int wide, out int tall);
		Vbar.SetPos(wide - (Vbar.GetWide() + WINDOW_BORDER_WIDTH), 0);
		Vbar.SetSize(Vbar.GetWide(), tall - 2);
		Vbar.InvalidateLayout();

		int buttonMaxXPos = wide - (Vbar.GetWide() + WINDOW_BORDER_WIDTH);

		int columnCount = CurrentColumns.Count;
		int numToResize = 0;
		if (ColumnDraggerMoved != -1)
			numToResize = 1;
		else
			for (int i = 0; i < columnCount; i++) {
				Column column = ColumnsData.ElementAt(CurrentColumns.ElementAt(i));
				if (column.ResizesWithWindow && !column.Hidden) {
					numToResize++;
				}
			}

		int dxPerBar;
		int oldSizeX = 0;
		int lastColumnIndex = columnCount - 1;
		for (int i = columnCount - 1; i >= 0; --i) {
			Column column = ColumnsData.ElementAt(CurrentColumns.ElementAt(i));
			if (!column.Hidden) {
				column.Header.GetPos(out oldSizeX, out _);
				lastColumnIndex = i;
				break;
			}
		}

		bool bForceShrink = false;
		if (numToResize == 0) {
			int minWidth = 0;
			for (int i = 0; i < columnCount; i++) {
				Column column = ColumnsData.ElementAt(CurrentColumns.ElementAt(i));
				if (!column.Hidden)
					minWidth += column.MinWidth;
			}

			if (minWidth > buttonMaxXPos) {
				int dx = buttonMaxXPos - minWidth;
				dxPerBar = dx / columnCount;
				bForceShrink = true;
			}
			else
				dxPerBar = 0;
			LastBarWidth = buttonMaxXPos;
		}
		else if (oldSizeX != 0) {
			int dx = buttonMaxXPos - LastBarWidth;
			dxPerBar = dx / numToResize;
			LastBarWidth = buttonMaxXPos;
		}
		else {
			int startingBarWidth = 0;
			for (int i = 0; i < columnCount; i++) {
				Column column = ColumnsData.ElementAt(CurrentColumns.ElementAt(i));
				if (!column.Hidden)
					startingBarWidth += column.Header.GetWide();
			}
			int dx = buttonMaxXPos - startingBarWidth;
			dxPerBar = dx / numToResize;
			LastBarWidth = buttonMaxXPos;
		}

		for (int i = 0; i < columnCount; i++) {
			Column column = ColumnsData.ElementAt(CurrentColumns.ElementAt(i));
			Panel header = column.Header;
			if (header.GetWide() < column.MinWidth)
				header.SetWide(column.MinWidth);
		}

		for (int iLoopSanityCheck = 0; iLoopSanityCheck < 1000; iLoopSanityCheck++) {
			int x = -1;
			int i;
			for (i = 0; i < columnCount; i++) {
				Column column = ColumnsData.ElementAt(CurrentColumns.ElementAt(i));
				Panel header = column.Header;
				if (column.Hidden) {
					header.SetVisible(false);
					continue;
				}

				header.SetPos(x, 0);
				header.SetVisible(true);

				if (x + column.MinWidth >= buttonMaxXPos && !bForceShrink)
					break;

				int hWide = header.GetWide();

				if (i == lastColumnIndex)
					hWide = buttonMaxXPos - x;
				else if (i == ColumnDraggerMoved)
					hWide += dxPerBar;
				else if (ColumnDraggerMoved == -1) {
					if (column.ResizesWithWindow || bForceShrink) {
						Assert(column.MinWidth <= column.MaxWidth);
						hWide += dxPerBar;
					}
				}

				if (hWide < column.MinWidth && !bForceShrink)
					hWide = column.MinWidth;
				else if (hWide > column.MaxWidth)
					hWide = column.MaxWidth;

				header.SetSize(hWide, Vbar.GetWide());
				x += hWide;

				Panel sizer = column.Resizer;
				if (i == lastColumnIndex)
					sizer.SetVisible(false);
				else
					sizer.SetVisible(true);
				sizer.MoveToFront();
				sizer.SetPos(x - 4, 0);
				sizer.SetSize(8, Vbar.GetWide());
			}

			if (i == columnCount)
				break;

			int totalDesiredWidth = 0;
			for (i = 0; i < columnCount; i++) {
				Column column = ColumnsData.ElementAt(CurrentColumns.ElementAt(i));
				if (!column.Hidden) {
					Panel header = column.Header;
					totalDesiredWidth += header.GetWide();
				}
			}

			Assert(totalDesiredWidth > buttonMaxXPos);
			for (i = columnCount - 1; i >= 0; i--) {
				Column column = ColumnsData.ElementAt(CurrentColumns.ElementAt(i));
				if (!column.Hidden) {
					Panel header = column.Header;

					totalDesiredWidth -= header.GetWide();
					if (totalDesiredWidth + column.MinWidth <= buttonMaxXPos) {
						int newWidth = buttonMaxXPos - totalDesiredWidth;
						header.SetSize(newWidth, Vbar.GetWide());
						break;
					}

					totalDesiredWidth += column.MinWidth;
					header.SetSize(column.MinWidth, Vbar.GetWide());
				}
			}

			dxPerBar -= 5;
			if (dxPerBar < 0)
				dxPerBar = 0;

			if (i == -1) {
				break;
			}
		}

		if (EditModePanel != null) {
			TableStartX = 0;
			TableStartY = HeaderHeight + 1;

			int nTotalRows = VisibleItems.Count();
			int nRowsPerPage = (int)GetRowsPerPage();

			int nStartItem = 0;
			if (nRowsPerPage <= nTotalRows)
				nStartItem = Vbar.GetValue();

			bool done = false;
			int drawcount = 0;
			for (int i = nStartItem; i < nTotalRows && !done; i++) {
				int x = 0;
				if (!VisibleItems.IsValidIndex(i))
					continue;

				int itemID = VisibleItems[i];

				for (int j = 0; j < CurrentColumns.Count; j++) {
					Panel header = ColumnsData.ElementAt(CurrentColumns.ElementAt(j)).Header;

					if (!header.IsVisible())
						continue;

					wide = header.GetWide();

					if (itemID == EditModeItemID && j == EditModeColumn) {

						EditModePanel.SetPos(x + TableStartX + 2, (drawcount * RowHeight) + TableStartY);
						EditModePanel.SetSize(wide, RowHeight - 1);

						done = true;
					}

					x += wide;
				}

				drawcount++;
			}
		}

		Repaint();
		ColumnDraggerMoved = -1;

		Column column0 = ColumnsData.ElementAt(0);
		if (column0.Header != null)
			HeaderHeight = column0.Header.GetTall();
	}

	public override void OnSizeChanged(int wide, int tall) {
		base.OnSizeChanged(wide, tall);
		InvalidateLayout();
		Repaint();
	}

	public override void Paint() {
		if (NeedsSort)
			SortList();

		GetSize(out int wide, out _);

		TableStartX = 0;
		TableStartY = HeaderHeight + 1;

		int totalRows = VisibleItems.Count;
		int rowsPerPage = (int)GetRowsPerPage();

		int startItem = 0;
		if (rowsPerPage < totalRows)
			startItem = Vbar.GetValue();

		int VbarInset = Vbar.IsVisible() ? Vbar.GetWide() : 0;
		int maxw = wide - VbarInset - 8;

		bool done = false;
		int drawCount = 0;
		for (int i = startItem; i < totalRows && !done; i++) {
			int x = 0;
			if (i < 0 || i >= VisibleItems.Count)
				continue;

			int itemID = VisibleItems[i];

			for (int j = 0; j < ColumnsData.Count; j++) {
				Column col = ColumnsData.ElementAt(j);
				Panel header = col.Header;
				Panel render = GetCellRenderer(itemID, j);

				if (!header.IsVisible())
					continue;

				int hWide = header.GetWide();

				if (render != null) {
					if (render.GetParent() != this)
						render.SetParent(this);

					if (!render.IsVisible())
						render.SetVisible(true);

					int xpos = x + TableStartX + 2;
					render.SetPos(xpos, (drawCount * RowHeight) + TableStartY);

					int right = Math.Min(x + hWide, maxw);
					int usew = right - xpos;
					render.SetSize(usew, RowHeight - 1);
					render.Repaint();

					Surface.SolveTraverse(render);

					render.GetClipRect(out _, out int y0, out _, out int y1);
					if ((y1 - y0) < (RowHeight - 3)) {
						done = true;
						break;
					}

					Surface.PaintTraverse(render);
				}
				x += hWide;
			}
			drawCount++;
		}

		Label.SetVisible(false);

		if (VisibleItems.Count < 1 && EmptyListText != null) {
			EmptyListText.SetPos(TableStartX + 8, TableStartY + 4);
			EmptyListText.SetSize(wide - 8, RowHeight);
			EmptyListText.Paint();
		}
	}

	void HandleMultselection(int itemID, int row, int column) {

	}

	void HandleAddSelection(int itemID, int row, int column) {

	}

	void UpdateSelection(ButtonCode code, int x, int y, int row, int column) {

	}

	public override void OnMousePressed(ButtonCode code) {

	}

	public override void OnMouseWheeled(int delta) {

	}

	public override void OnMouseDoublePressed(ButtonCode code) {

	}

	void OnButtonCodePressed(ButtonCode code) {

	}

	// bool GetCellBounds(int row, int col, int x, int y, int wide, int tall) {

	// }

	// bool GetCellAtPos(int x, int y, int row, int col) {

	// }

	public override void ApplySchemeSettings(IScheme scheme) {
		Label.InvalidateLayout(true);

		base.ApplySchemeSettings(scheme);

		SetBgColor(GetSchemeColor("ListPanel.BgColor", scheme));
		SetBorder(scheme.GetBorder("ButtonDepressedBorder"));

		Label.SetBgColor(GetSchemeColor("ListPanel.BgColor", scheme));

		LabelFgColor = GetSchemeColor("ListPanel.TextColor", scheme);
		DisabledColor = GetSchemeColor("ListPanel.DisabledTextColor", scheme);
		SelectionFgColor = GetSchemeColor("ListPanel.SelectedTextColor", scheme);
		DisabledSelectionFgColor = GetSchemeColor("ListPanel.DisabledSelectedTextColor", scheme);

		EmptyListText.SetColor(GetSchemeColor("ListPanel.EmptyListInfoTextColor", scheme));

		SetFont(scheme.GetFont("Default", IsProportional())!);
		EmptyListText.SetFont(scheme.GetFont("Default", IsProportional()));
	}

	public void SetSortFunc(int col, SortFunc func) {

	}

	void SetSortColumn(int column) {

	}

	// int GetSortColumn() {

	// }

	public void SetSortColumnEx(int primarySortColumn, int secondarySortColumn, bool sortAscending) {

	}

	void GetSortColumnEx(int primarySortColumn, int secondarySortColumn, bool sortAscending) {

	}

	void SortList() {

	}

	void SetFont(IFont font) {

	}

	void OnSliderMoved() {

	}

	void OnColumnResized(int col, int delta) {

	}

	void OnSetSortColumn(int column) {

	}

	void SetItemVisible(int itemID, bool state) {

	}

	// bool IsItemVisible(int itemID) {

	// }

	void SetItemDisabled(int itemID, bool state) {

	}

	float GetRowsPerPage() => (GetTall() - HeaderHeight) / RowHeight;

	// int GetStartItem() {

	// }

	void SetSelectIndividualCells(bool state) {

	}

	void SetMultiselectEnabled(bool state) {

	}

	// bool IsMultiselectEnabled() {

	// }

	void SetEmptyListText(ReadOnlySpan<char> text) {

	}


	void OpenColumnChoiceMenu() {

	}

	void ResizeColumnToContents(int column) {

	}

	void OnToggleColumnVisible(int col) {

	}

	void ApplyUserConfigSettings(KeyValues userConfig) {

	}

	void GetUserConfigSettings(KeyValues userConfig) {

	}

	// bool HasUserConfigSettings() {

	// }

	void SetAllowUserModificationOfColumns(bool allowed) {

	}

	void SetIgnoreDoubleClick(bool state) {

	}

	void EnterEditMode(int itemID, int column, Panel editPanel) {

	}

	void LeaveEditMode() {

	}

	// bool IsInEditMode() {

	// }
}