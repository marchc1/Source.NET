using Source.Common.GUI;

namespace Source.GUI.Controls;

public class MenuSeparator : Panel
{

}

public class Menu : Panel
{
	Color BorderDark;

	public const int MENU_SEPARATOR_HEIGHT = 3;
	public Alignment Alignment;

	protected ScrollBar? Scroller;

	protected List<MenuItem> MenuItems = [];
	protected List<int> SortedItems = [];
	protected List<int> Separators = [];
	protected List<int> VisibleSortedItems = [];
	protected List<MenuSeparator> SeparatorPanels = [];

	protected bool RecalculateWidth = true;
	int MenuItemHeight;
	bool SizedForScrollBar;
	int FixedWidth;
	int MinimumWidth;
	int MenuWide;
	int NumVisibleLines;

	public override void PaintBackground() {

	}
	public override void Paint() {

	}

	public override void ApplySchemeSettings(IScheme scheme) {
		base.ApplySchemeSettings(scheme);
		SetFgColor(GetSchemeColor("Menu.TextColor", scheme));
		SetBgColor(GetSchemeColor("Menu.BgColor", scheme));

		BorderDark = scheme.GetColor("BorderDark", new(255, 255, 255, 0));

		foreach (var menuItem in MenuItems) {
			if (menuItem.IsCheckable()) {
				// todo;
			}
		}

		RecalculateWidth = true;
		CalculateWidth();

		InvalidateLayout();
	}

	public Menu(Panel parent, string panelName) : base(parent, panelName) {
		Scroller = new ScrollBar(this, "MenuScrollBar", true);
		Scroller.SetVisible(false);
		Scroller.AddActionSignalTarget(this);
		SizedForScrollBar = false;
		SetZPos(1);
		SetVisible(false);
		MakePopup(false);
		SetParent(parent);
		RecalculateWidth = true;

		if (IsProportional()) {
			// todo
		}
		else
			MenuItemHeight = DEFAULT_MENU_ITEM_HEIGHT;
	}

	bool UseFallbackFont;
	IFont? FallbackItemFont;

	public virtual int AddMenuItem(MenuItem panel) {
		panel.SetParent(this);
		int itemID = MenuItems.Count;
		MenuItems.Add(panel);
		SortedItems.Add(itemID);
		InvalidateLayout(false);
		RecalculateWidth = true;
		panel.SetContentAlignment(Alignment);

		if (ItemFont != null)
			panel.SetFont(ItemFont);

		if (UseFallbackFont && FallbackItemFont != null) {
			Label l = panel;
			TextImage? ti = l.GetTextImage();
			if (ti != null)
				ti.SetUseFallbackFont(UseFallbackFont, FallbackItemFont);
		}

		// hotkeys?

		return itemID;
	}

	MenuItem? GetParentMenuItem() => GetParent() is MenuItem mi ? mi : null;


	public int GetMenuItemHeight() {
		return MenuItemHeight;
	}
	public void SetMenuItemHeight(int itemHeight) {
		MenuItemHeight = itemHeight;
	}
	public const int DEFAULT_MENU_ITEM_HEIGHT = 22;
	public int CountVisibleItems() {
		int count = 0;
		int len = SortedItems.Count;
		for (int i = 0; i < len; i++)
			if (MenuItems[SortedItems[i]].IsVisible())
				++count;

		return count;
	}

	IFont? ItemFont;
	public void SetFont(IFont? font) {
		ItemFont = font;
		if (font != null)
			MenuItemHeight = Surface.GetFontTall(font) + 2;
		InvalidateLayout();
	}

	public override void PerformLayout() {
		MenuItem? parent = GetParentMenuItem();
		bool cascading = parent != null ? true : false;

		GetInset(out int ileft, out int iright, out int itop, out int ibottom);
		ComputeWorkspaceSize(out int workWide, out int workTall);

		int fullHeightWouldRequire = ComputeFullMenuHeightWithInsets();
		bool needScrollbar = fullHeightWouldRequire >= workTall;
		int maxVisibleItems = CountVisibleItems();

		if (NumVisibleLines > 0 &&
			maxVisibleItems > NumVisibleLines) {
			needScrollbar = true;
			maxVisibleItems = NumVisibleLines;
		}

		if (needScrollbar) {
			AddScrollBar();
			MakeItemsVisibleInScrollRange(NumVisibleLines, Math.Min(fullHeightWouldRequire, workTall));
		}
		else {
			RemoveScrollBar();
			VisibleSortedItems.Clear();
			int ip;
			int c = SortedItems.Count;
			for (ip = 0; ip < c; ++ip) {
				int itemID = SortedItems[ip];
				MenuItem child = MenuItems[itemID];
				if (child == null || !child.IsVisible())
					continue;

				VisibleSortedItems.Add(itemID);
			}

			c = SeparatorPanels.Count;
			for (ip = 0; ip < c; ++ip)
				SeparatorPanels[ip]?.SetVisible(false);
		}

		// get the appropriate menu border
		LayoutMenuBorder();

		int trueW = GetWide();
		if (needScrollbar)
			trueW -= Scroller!.GetWide();

		int separatorHeight = MENU_SEPARATOR_HEIGHT;

		int menuTall = 0;
		int totalTall = itop + ibottom;
		int i;
		for (i = 0; i < VisibleSortedItems.Count; i++) {
			int itemId = VisibleSortedItems[i];

			MenuItem? child = MenuItems[itemId];
			Assert(child != null);
			if (child == null)
				continue;

			if (!child.IsVisible())
				continue;

			if (totalTall >= workTall)
				break;

			//  if (INVALID_FONT != m_hItemFont) {
			//  	child->SetFont(m_hItemFont);
			//  }

			child.SetPos(0, menuTall);
			child.SetTall(MenuItemHeight);
			menuTall += MenuItemHeight;
			totalTall += MenuItemHeight;

			// TODO: checkable

			int sepIndex = Separators.FindIndex(0, (x) => x == itemId);
			if (sepIndex != -1) {
				MenuSeparator? sep = SeparatorPanels[sepIndex];
				Assert(sep != null);
				sep.SetVisible(true);
				sep.SetBounds(0, menuTall, trueW, separatorHeight);
				menuTall += separatorHeight;
				totalTall += separatorHeight;
			}
		}

		if (FixedWidth == 0) {
			RecalculateWidth = true;
			CalculateWidth();
		}
		else if (FixedWidth > 0) {
			MenuWide = FixedWidth;
			if (SizedForScrollBar)
				MenuWide -= Scroller!.GetWide();
		}

		SizeMenuItems();

		int extraWidth = 0;
		if (SizedForScrollBar)
			extraWidth = Scroller!.GetWide();

		int mwide = MenuWide + extraWidth;
		if (mwide > workWide)
			mwide = workWide;

		int mtall = menuTall + itop + ibottom;
		if (mtall > workTall)
			mtall = workTall;

		SetSize(mwide, mtall);

		if (cascading)
			PositionCascadingMenu();

		if (Scroller!.IsVisible())
			LayoutScrollBar();

		foreach (var menuItem in MenuItems)
			menuItem.InvalidateLayout();
		Repaint();
	}

	private void PositionCascadingMenu() {

	}

	private void LayoutScrollBar() {

	}

	private void SizeMenuItems() {
		GetInset(out int left, out int right, out _, out _);

		foreach (var child in MenuItems)
			child.SetWide(MenuWide - left - right);
	}

	private void CalculateWidth() {
		if (!RecalculateWidth)
			return;

		MenuWide = 0;
		if (FixedWidth == 0) {
			foreach (var menuItem in MenuItems) {
				menuItem.GetContentSize(out int wide, out int tall);
				if (wide > MenuWide - Label.Content) {
					MenuWide = wide + Label.Content;
				}
			}
		}

		if (MenuWide < MinimumWidth)
			MenuWide = MinimumWidth;

		RecalculateWidth = false;
	}

	protected virtual void LayoutMenuBorder() {
		IScheme? scheme = GetScheme();
		IBorder? menuBorder = scheme?.GetBorder("MenuBorder");
		if (menuBorder != null)
			SetBorder(menuBorder);
	}

	private void MakeItemsVisibleInScrollRange(int maxVisibleItems, int numPixelsAvailable) {
		int i;
		foreach (var item in MenuItems) {
			item.SetBounds(0, 0, 0, 0);
		}
		for (i = 0; i < SeparatorPanels.Count; ++i)
			SeparatorPanels[i].SetVisible(false);


		VisibleSortedItems.Clear();

		int tall = 0;

		int startItem = 0; //Scroller?.GetValue();
		Assert(startItem >= 0);
		do {
			if (startItem >= SortedItems.Count)
				break;

			int itemId = SortedItems[startItem];

			if (!MenuItems[itemId].IsVisible()) {
				++startItem;
				continue;
			}

			int itemHeight = MenuItemHeight;
			int sepIndex = -1; // Separators.Find(itemId);
			if (sepIndex != -1) {
				itemHeight += MENU_SEPARATOR_HEIGHT;
			}

			if (tall + itemHeight > numPixelsAvailable)
				break;

			// Too many items
			if (maxVisibleItems > 0) {
				if (VisibleSortedItems.Count >= maxVisibleItems)
					break;
			}

			tall += itemHeight;
			VisibleSortedItems.Add(itemId);
			++startItem;
		}
		while (true);
	}

	private void RemoveScrollBar() {
		Scroller.SetVisible(false);
		SizedForScrollBar = false;
	}

	private void AddScrollBar() {
		Scroller.SetVisible(true);
		SizedForScrollBar = true;
	}

	private void ComputeWorkspaceSize(out int workWide, out int workTall) {
		GetInset(out _, out _, out int top, out int bottom);

		Surface.GetWorkspaceBounds(out int workX, out int workY, out workWide, out workTall);
		workTall -= 20;
		workTall -= top;
		workTall -= bottom;
	}

	private int ComputeFullMenuHeightWithInsets() {
		GetInset(out int left, out int right, out int top, out int bottom);

		int separatorHeight = 3;

		int totalTall = top + bottom;
		int i;
		for (i = 0; i < SortedItems.Count; i++) {
			int itemId = SortedItems[i];

			MenuItem child = MenuItems[itemId];
			Assert(child != null);
			if (child == null)
				continue;

			if (!child.IsVisible())
				continue;

			totalTall += MenuItemHeight;

			int sepIndex = Separators.FindIndex(0, (i) => i == itemId);
			if (sepIndex != -1)
				totalTall += separatorHeight;
		}

		return totalTall;
	}

	public void ForceCalculateWidth() {
		RecalculateWidth = true;
		CalculateWidth();
		PerformLayout();
	}
}
