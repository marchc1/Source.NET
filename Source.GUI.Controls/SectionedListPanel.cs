using Source.Common.GUI;

namespace Source.GUI.Controls;

class SectionedListPanel : Panel
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

	struct ColorOverride {
		int SectionID;
		int ItemID;
		int ColumnID;
		Color ClrOverride;
	}

	struct Column {
		char[] ColumnName;
		char[] ColumnText;
		int ColumnFlags;
		int Width;
		IFont? FallbackFont;
	}

	struct Section {
		int ID;
		bool AlwaysVisible;
		// SectionListPanelHeader? Header;
		List<Column> Columns;
		// SectionSortFunc SortFunc;
		int MinimumHeight;
	}

	List<Section> Sections;
	// List<ItemButton> Items;
	// List<ItemButton> FreeItems;
	// List<ItemButton> SortedItems;

	List<ColorOverride> ColorOverrides;

	Panel EditModePanel;
	int EditModeItemID;
	int EditModeColumn;
	int ContentHeight;
	int LineSpacing;
	int LineGap;
	int SectionGap;

	ScrollBar ScrollBar;
	// ImageList ImageList;
	bool DeleteImageListWhenDone;
	bool SortNeeded;
	bool VerticalScrollbarEnabled;

	IFont HeaderFont;
	IFont RowFont;

	bool Clickable;
	bool DrawSectionHeaders;

	public SectionedListPanel(Panel parent, ReadOnlySpan<char> name) : base(parent) {

	}
}
