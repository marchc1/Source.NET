using Source.Common.Formats.Keyvalues;
using Source.Common.GUI;
using Source.Common.Input;

namespace Source.GUI.Controls;

class SectionListPanelHeader : Label
{
	int SectionID;
	Color SectionDividerColor;
	SectionedListPanel ListPanel;
	bool DrawDividerBar;

	public SectionListPanelHeader(SectionedListPanel parent, ReadOnlySpan<char> name, int sectionID) : base(parent, name, "") {
		ListPanel = parent;
		SectionID = sectionID;
		SetTextImageIndex(-1);
		ClearImages();
		SetPaintBackgroundEnabled(false);
		DrawDividerBar = true;
	}

	public override void ApplySchemeSettings(IScheme scheme) {
		base.ApplySchemeSettings(scheme);

	}

	public override void Paint() {
		base.Paint();

	}

	public void SetColor() {

	}

	public void SetDividerColor() {

	}

	public override void PerformLayout() {
		base.PerformLayout();

	}
}

class ItemButton : Label
{
	SectionedListPanel ListPanel;
	int ID;
	int SectionID;
	KeyValues Data;
	Color FgColor2;
	Color FgColor;
	Color ArmedFgColor1;
	Color ArmedFgColor2;
	Color OutOfFocusSelectedTextColor;
	Color ArmedBgColor;
	Color SelectionBG2Color;
	List<TextImage> TextImages;
	bool Selected;
	bool OverrideColors;
	bool ShowColumns;
	int HorizFillInset;

	public ItemButton(SectionedListPanel parent, int itemID) : base(parent, null, "< item >") {
		ListPanel = parent;
		ID = itemID;
		Data = null;
		// Clear();
		HorizFillInset = 0;
	}

	private void Clear() {

	}

	public int GetID() => ID;
	public void SetID(int id) => ID = id;
	public int GetSectionID() => SectionID;

	public void SetSectionID() {

	}

	public void SetData() {

	}

	public override void PerformLayout() {

		base.PerformLayout();
	}

	public override void ApplySchemeSettings(IScheme scheme) {
		base.ApplySchemeSettings(scheme);

	}

	public override void PaintBackground() {

	}

	public override void Paint() {
		base.Paint();

	}

	public override void OnMousePressed(ButtonCode code) {

	}

	public void SetSelected() {

	}

	public bool IsSelected() => Selected;

	public override void OnSetFocus() {
		InvalidateLayout();
		base.OnSetFocus();
	}

	public override void OnKillFocus(Panel? newPanel) {
		InvalidateLayout();
		base.OnKillFocus(newPanel);
	}

	public override void OnMouseDoublePressed(ButtonCode code) {

	}

	public void GetCellBounds() {

	}

	public void GetMaxCellBounds() {

	}

	public void SetOverrideColors(bool state) => OverrideColors = state;
	public void SetShowColumns(bool state) => ShowColumns = state;
	public void SetItemBgHorizFillInset(int inset) => HorizFillInset = inset;
}

public class SectionedListPanel : Panel
{
	public static Panel Create_SectionedListPanel() => new SectionedListPanel(null, null);

	[Flags]
	enum ColumnFlags
	{
		HeaderImage = 0x01,
		ColumnImage = 0x02,
		ColumnBright = 0x04,
		ColumnCenter = 0x08,
		ColumnRight = 0x10
	}

	public const int BUTTON_HEIGHT_DEFAULT = 20;
	public const int BUTTON_HEIGHT_SPACER = 7;
	public const int DEFAULT_LINE_SPACING = 20;
	public const int DEFAULT_SECTION_GAP = 8;
	public const int COLUMN_DATA_INDENT = 6;
	public const int COLUMN_DATA_GAP = 2;

	struct ColorOverride
	{
		int SectionID;
		int ItemID;
		int ColumnID;
		Color ClrOverride;
	}

	struct Column
	{
		char[] ColumnName;
		char[] ColumnText;
		int ColumnFlags;
		int Width;
		IFont? FallbackFont;
	}

	struct Section
	{
		int ID;
		bool AlwaysVisible;
		SectionListPanelHeader? Header;
		List<Column> Columns;
		// SectionSortFunc SortFunc;
		int MinimumHeight;
	}

	List<Section> Sections;
	List<ItemButton> Items;
	List<ItemButton> FreeItems;
	List<ItemButton> SortedItems;

	List<ColorOverride> ColorOverrides;

	Panel EditModePanel;
	int EditModeItemID;
	int EditModeColumn;
	int ContentHeight;
	int LineSpacing;
	int LineGap;
	int SectionGap;

	ScrollBar ScrollBar;
	ImageList? ImageList;
	bool DeleteImageListWhenDone;
	bool SortNeeded;
	bool VerticalScrollbarEnabled;

	IFont HeaderFont;
	IFont RowFont;

	bool Clickable;
	bool DrawSectionHeaders;

	[PanelAnimationVar("show_columns", "false")] bool ShowColumns;

	public SectionedListPanel(Panel parent, ReadOnlySpan<char> name) : base(parent) {
		ScrollBar = new(this, "SectionedScrollBar", true);
		ScrollBar.SetVisible(false);
		ScrollBar.AddActionSignalTarget(this);

		EditModeItemID = 0;
		EditModeColumn = 0;
		SortNeeded = false;
		VerticalScrollbarEnabled = false;
		LineSpacing = DEFAULT_LINE_SPACING;
		LineGap = 0;
		SectionGap = DEFAULT_SECTION_GAP;

		ImageList = null;
		DeleteImageListWhenDone = false;

		// HeaderFont = INVALID_FONT;
		// RowFont = INVALID_FONT;

		Clickable = true;

		DrawSectionHeaders = true;
	}

	private void ReSortList() {

	}

	public override void PerformLayout() {
		if (SortNeeded) {
			ReSortList();
			SortNeeded = false;
		}

		base.PerformLayout();

		LayoutPanels(ContentHeight);

		GetBounds(out int cx, out int cy, out int cwide, out int ctall);

		if (ContentHeight > ctall && VerticalScrollbarEnabled) {
			ScrollBar.SetVisible(true);
			ScrollBar.MoveToFront();
			ScrollBar.SetPos(cwide - ScrollBar.GetWide() - 2, 0);
			ScrollBar.SetSize(ScrollBar.GetWide(), ctall - 2);
			ScrollBar.SetRangeWindow(ctall);
			ScrollBar.SetRange(0, ContentHeight);
			ScrollBar.InvalidateLayout();
			ScrollBar.Repaint();

			LayoutPanels(ContentHeight);
		}
		else {
			ScrollBar.SetValue(0);

			bool wasVisible = ScrollBar.IsVisible();
			ScrollBar.SetVisible(false);

			if (wasVisible)
				LayoutPanels(ContentHeight);
		}
	}

	private void LayoutPanels(int contentTall) {

	}

	private void ScrollToItem(int itemID) {

	}

	public override void ApplySchemeSettings(IScheme scheme) {
		base.ApplySchemeSettings(scheme);

		SetFgColor(GetSchemeColor("SectionedListPanel.BgColor", scheme));
		SetBorder(scheme.GetBorder("ButtonDepressedBorder"));

		//foreach (var item in Items) {
			// item.SetShowColumns(ShowColumns)
		//}
	}

	public void SetHeaderFont(IFont font) => HeaderFont = font;
	public IFont GetHeaderFont() => HeaderFont;

	public void SetRowFont(IFont font) => RowFont = font;
	public IFont GetRowFont() => RowFont;

	public override void ApplySettings(KeyValues resourceData) {
		base.ApplySettings(resourceData);

	}

	public override void SetProportional(bool state) {
		base.SetProportional(state);

	}

	public void SetVerticalScrollbar(bool state) => VerticalScrollbarEnabled = state;

	public void AddSection() {

	}

	// public void AddSection() {}
	// public void AddSection() {}

	public void RemoveAllSections() {

	}

	public void AddColumnToSection() {

	}

	// public void AddColumnToSection() {}

	public void ModifyColumn() {

	}

	public void AddItem() {

	}

	public void ModifyItem() {

	}

	public void SetItemFgColor() {

	}

	public void SetItemBgColor() {

	}

	public void SetItemBgHorizFillInset() {

	}

	public void SetItemFont() {

	}

	public void SetItemEnabled() {

	}

	public void SetSectionFgColor() {

	}

	public void SetSectionDividerColor() {

	}

	public void SetSectionDrawDividerBar() {

	}

	public void SetSectionAlwaysVisible() {

	}

	public void SetFontSection() {

	}

	public void SetSectionMinimumHeight() {

	}

	public void RemoveItem() {

	}

	public void GetColumnCountBySection() {

	}

	public void GetColumnNameBySection() {

	}

	public void GetColumnTextBySection() {

	}

	public void GetColumnFlagsBySection() {

	}

	public void GetColumnWWidthBySection() {

	}

	public void SetColumnWidthBySection() {

	}

	public void GetColumnIndexByName() {

	}

	public void FindSectionIndexByID() {

	}

	public void OnSliderMoved() {

	}

	public void OnSizeChanged() {

	}

	public void OnMousePressed() {

	}

	public void ClearSelection() {

	}

	public void MoveSelectionDown() {

	}

	public void MoveSelectionUp() {

	}

	public void NavigateTo() {

	}

	public void OnKeyCodePressed() {

	}

	public void DeleteAllItems() {

	}

	public void SetSelectedItem(int itemID) {

	}

	public void GetSelectedItem() {

	}

	// public void SetSelectedItem() {

	// }

	public void GetItemData() {

	}

	public void GetItemSection() {

	}

	public void IsItemIDValid() {

	}

	public void GetHighestItemID() {

	}

	public void GetItemCount() {

	}

	public void GetItemIDFromRow() {

	}

	public void GetRowFromItemID() {

	}

	public void GetCellBounds() {

	}

	public void GetSectionHeaderBounds() {

	}

	public void GetMaxCellBounds() {

	}

	public void GetItemBounds() {

	}

	public void InvalidateItem() {

	}

	public void EnterEditMode() {

	}

	public void LeaveEditMode() {

	}

	public void IsInEditMode() {

	}

	public void SetImageList() {

	}

	public override void OnSetFocus() {

	}

	public void GetSectionTall() {

	}

	public void GetContentSize() {

	}

	public void GetNewItemButton() {

	}

	public void GetColumnFallbackFontBySection() {

	}

	public void GetColorOverrideForCell() {

	}

	public void SetColorOverrideForCell() {

	}
}
