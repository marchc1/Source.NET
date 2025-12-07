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
	public Dragger(Panel parent, ReadOnlySpan<char> name) : base(parent, name) {

	}
}


class FastSortListPanelItem : ListPanelItem
{
	List<int> SortedTreeIndexes = [];
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

	struct Column
	{
		public Button Header;
		public int MinWidth;
		public int MaxWidth;
		public bool ResizesWithWindow;
		public Panel Resizer;
		public SortFunc SortFunc;
		public bool TypeIsText;
		public bool Hidden;
		public bool Unhidable;
		public SortedDictionary<int, IndexItem_t> SortedTree;
		public int ContentAlignment;
	}
	LinkedList<Column> ColumnsData = [];
	List<byte> ColumnsHistory = [];
	List<byte> CurrentColumns = [];
	int ColumnDraggerMoved;
	int LastBarWidth;
	LinkedList<FastSortListPanelItem> DataItems = [];
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

	}

	public void AddColumnHeader(int index, ReadOnlySpan<char> columnName, ReadOnlySpan<char> columnText, int width, int minWidth, int maxWidth, int columnFlags) {

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

		DataItems.AddLast(newItem);
		int itemID = DataItems.Count - 1;
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

	// int GetItemCount() {

	// }

	public int GetItem(ReadOnlySpan<char> itemName) {
		int itemID = 0;
		foreach (var item in DataItems) {
			if (item.kv != null && item.kv.GetString("name").SequenceEqual(itemName))
				return itemID;
			itemID++;
		}
		return -1;
	}

	public KeyValues? GetItem(int itemID) {
		if (itemID < 0 || itemID >= DataItems.Count)
			return null;
		return DataItems.ElementAt(itemID).kv;
	}

	// int GetItemCurrentRow(int itemID) {

	// }

	void SetItemDragData(int itemID, KeyValues data) {

	}

	void OnCreateDragData(KeyValues msg) {

	}

	// int GetItemIDFromRow(int currentRow) {

	// }

	// int FirstItem() {

	// }

	// int NextItem(int iItem) {

	// }

	// int InvalidItemID() {

	// }

	// bool IsValidItemID(int itemID) {

	// }

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

	void CleanupItem(FastSortListPanelItem data) {

	}

	void RemoveItem(int itemID) {

	}

	public void RemoveAll() {

	}

	public void DeleteAllItems() {

	}

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

	// Panel GetCellRenderer(int itemID, int col) {

	// }

	public override void PerformLayout() {

	}

	public override void OnSizeChanged(int wide, int tall) {
		base.OnSizeChanged(wide, tall);
		InvalidateLayout();
		Repaint();
	}

	public override void Paint() {

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

	// float GetRowsPerPage() {

	// }

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