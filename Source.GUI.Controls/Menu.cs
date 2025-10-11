using Source.Common.Formats.Keyvalues;
using Source.Common.GUI;
using Source.Common.Input;

namespace Source.GUI.Controls;

public class MenuSeparator : Panel
{

}

public class Menu : Panel
{
	Color BorderDark;

	bool UseFallbackFont;
	IFont? FallbackItemFont;

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
	int CheckImageWidth;
	int CurrentlySelectedItemID;
	int ActivatedItem;

	public const int DEFAULT_MENU_ITEM_HEIGHT = 22;
	public const int MENU_UP = -1;
	public const int MENU_DOWN = 1;

	public override void Paint()
	{
		if (Scroller!.IsVisible())
		{
			GetSize(out int wide, out int tall);
			Surface.DrawSetColor(BorderDark);
			Surface.DrawFilledRect(wide - Scroller!.GetWide(), -1, wide - Scroller!.GetWide(), tall);
		}
	}

	public override void ApplySchemeSettings(IScheme scheme) {
		base.ApplySchemeSettings(scheme);
		SetFgColor(GetSchemeColor("Menu.TextColor", scheme));
		SetBgColor(GetSchemeColor("Menu.BgColor", scheme));

		BorderDark = scheme.GetColor("BorderDark", new(255, 255, 255, 0));

		foreach (MenuItem? menuItem in MenuItems) {
			if (menuItem.IsCheckable()) {
				// todo;
			}
		}

		RecalculateWidth = true;
		CalculateWidth();

		InvalidateLayout();
	}

	public Menu(Panel parent, string panelName) : base(parent, panelName)
	{
		Alignment = Alignment.West;
		FixedWidth = 0;
		MinimumWidth = 0;
		NumVisibleLines = 0;
		CurrentlySelectedItemID = -1;
		Scroller = new ScrollBar(this, "MenuScrollBar", true);
		Scroller.SetVisible(false);
		Scroller.AddActionSignalTarget(this);
		SizedForScrollBar = false;
		SetZPos(1);
		SetVisible(false);
		MakePopup(false);
		SetParent(parent);
		RecalculateWidth = true;
		// UseMenuManager = true;
		CheckImageWidth = 0;
		ActivatedItem = 0;

		if (IsProportional())
			MenuItemHeight = SchemeManager.GetProportionalScaledValueEx(GetScheme(), DEFAULT_MENU_ITEM_HEIGHT);
		else
			MenuItemHeight = DEFAULT_MENU_ITEM_HEIGHT;
	}


	public void DeleteAllItems()
	{
		for (int i = 0; i < MenuItems.Count; i++)
			MenuItems[i].MarkForDeletion();

		MenuItems.Clear();
		SortedItems.Clear();
		VisibleSortedItems.Clear();
		Separators.Clear();

		int count = SeparatorPanels.Count;
		for (int i = 0; i < count; i++)
			SeparatorPanels[i].MarkForDeletion();
		SeparatorPanels.Clear();
		InvalidateLayout();
	}

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
			ti?.SetUseFallbackFont(UseFallbackFont, FallbackItemFont);
		}

		// hotkeys?

		return itemID;
	}

	public void DeleteItem(int itemID) {
		Assert(SeparatorPanels.Count == 0); // From Source - FIXME: This doesn't work with seperator panels yet

		MenuItems[itemID].MarkForDeletion();
		MenuItems.Remove(MenuItems[itemID]);

		SortedItems.Remove(itemID);
		VisibleSortedItems.Remove(itemID);

		InvalidateLayout(false);
		RecalculateWidth = true;
	}

	public int AddMenuItemCharCommand(MenuItem item, ReadOnlySpan<char> command, Panel target, KeyValues userData)
	{
		item.SetCommand(command);
		item.AddActionSignalTarget(target);
		item.SetUserData(userData);
		return AddMenuItem(item);
	}

	public int AddMenuItemKeyValuesCommand(MenuItem item, KeyValues message, Panel target, KeyValues userData)
	{
		item.SetCommand(message);
		item.AddActionSignalTarget(target);
		item.SetUserData(userData);
		return AddMenuItem(item);
	}

	public virtual int AddMenuItem(string itemName, string itemText, ReadOnlySpan<char> command, Panel target, KeyValues userData)
	{
		MenuItem item = new(this, itemName, itemText);
		return AddMenuItemCharCommand(item, command, target, userData);
	}

	public virtual int AddMenuItem(string itemText, ReadOnlySpan<char> command, Panel target, KeyValues userData)
	{
		return AddMenuItem(itemText, itemText, command, target, userData);
	}

	public virtual int AddMenuItem(string itemName, string itemText, KeyValues message, Panel target, KeyValues userData)
	{
		MenuItem item = new(this, itemName, itemText);
		return AddMenuItemKeyValuesCommand(item, message, target, userData);
	}

	public virtual int AddMenuItem(string itemText, KeyValues message, Panel target, KeyValues userData)
	{
		return AddMenuItem(itemText, itemText, message, target, userData);
	}

	public virtual int AddMenuItem(string itemText, Panel target, KeyValues userData)
	{
		return AddMenuItem(itemText, itemText, target, userData);
	}

	public virtual int AddMenuItem(string itemText, KeyValues userData, Panel target)
	{
		return AddMenuItem(itemText, itemText, target, userData);
	}

	public virtual int AddCheckableMenuItem(string itemName, string itemtext, ReadOnlySpan<char> command, Panel target, KeyValues userData)
	{
		MenuItem item = new(this, itemName, itemtext, null, true);
		return AddMenuItemCharCommand(item, command, target, userData);
	}

	public virtual int AddCheckableMenuItem(string itemText, ReadOnlySpan<char> command, Panel target, KeyValues userData)
	{
		return AddCheckableMenuItem(itemText, itemText, command, target, userData);
	}

	public virtual int AddCheckableMenuItem(string itemName, string itemText, KeyValues message, Panel target, KeyValues userData)
	{
		MenuItem item = new(this, itemName, itemText, null, true);
		return AddMenuItemKeyValuesCommand(item, message, target, userData);
	}

	public virtual int AddCheckableMenuItem(string itemText, KeyValues message, Panel target, KeyValues userData)
	{
		return AddCheckableMenuItem(itemText, itemText, message, target, userData);
	}

	public virtual int AddCheckableMenuItem(string itemText, Panel target, KeyValues userData)
	{
		return AddCheckableMenuItem(itemText, itemText, target, userData);
	}

	public virtual int AddCascadingMenuItem(string itemName, string itemText, ReadOnlySpan<char> command, Panel target, Menu cascadeMenu, KeyValues userData)
	{
		MenuItem item = new(this, itemName, itemText, cascadeMenu, false);
		return AddMenuItemCharCommand(item, command, target, userData);
	}

	public virtual int AddCascadingMenuItem(string itemText, ReadOnlySpan<char> command, Panel target, Menu cascadeMenu, KeyValues userData)
	{
		return AddCascadingMenuItem(itemText, itemText, command, target, cascadeMenu, userData);
	}

	public virtual int AddCascadingMenuItem(string itemName, string itemText, KeyValues message, Panel target, Menu cascadeMenu, KeyValues userData)
	{
		MenuItem item = new(this, itemName, itemText, cascadeMenu, false);
		return AddMenuItemKeyValuesCommand(item, message, target, userData);
	}

	public virtual int AddCascadingMenuItem(string itemText, KeyValues message, Panel target, Menu cascadeMenu, KeyValues userData)
	{
		return AddCascadingMenuItem(itemText, itemText, message, target, cascadeMenu, userData);
	}

	public virtual int AddCascadingMenuItem(string itemText, Panel target, Menu cascadeMenu, KeyValues userData)
	{
		return AddCascadingMenuItem(itemText, itemText, target, cascadeMenu, userData);
	}

	MenuItem? GetParentMenuItem() => GetParent() is MenuItem mi ? mi : null;


	public int GetMenuItemHeight() {
		return MenuItemHeight;
	}

	public void SetContentAlignment(Alignment alignment)
	{
		if (Alignment != alignment)
		{
			Alignment = alignment;

			foreach (var menuItem in MenuItems)
				menuItem.SetContentAlignment(alignment);
		}
	}

	public void SetFixedWidth(int width)
	{
		FixedWidth = width;
		InvalidateLayout(false);
	}

	public void SetMenuItemHeight(int itemHeight) {
		MenuItemHeight = itemHeight;
	}

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
		bool cascading = parent != null;

		GetInset(out _, out _, out int itop, out int ibottom);
		ComputeWorkspaceSize(out int workWide, out int workTall);

		int fullHeightWouldRequire = ComputeFullMenuHeightWithInsets();
		bool needScrollbar = fullHeightWouldRequire >= workTall;
		int maxVisibleItems = CountVisibleItems();

		if (NumVisibleLines > 0 &&
			maxVisibleItems > NumVisibleLines) {
			needScrollbar = true;
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

			if (child.IsCheckable() && CheckImageWidth > 0)
				child.SetTextInset(CheckImageWidth, 0);
			else
				child.SetTextInset(0, 0);

			for (int j = 0; j < Separators.Count; j++)
			{
				if (Separators[j] == itemId)
				{
					MenuSeparator? sep = SeparatorPanels[j];
					Assert(sep != null);
					sep.SetVisible(true);
					sep.SetBounds(0, menuTall, trueW, separatorHeight);
					menuTall += separatorHeight;
					totalTall += separatorHeight;
					break;
				}
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

	private void PositionCascadingMenu()
	{
		IPanel? parent = GetParent();
		Assert(parent != null);
		parent.GetSize(out int parentWide, out _);
		parent.GetPos(out int parentX, out _);

		parentX += parentWide;
		int parentY = 0;

		ParentLocalToScreen(ref parentX, ref parentY);

		SetPos(parentX, parentY);

		GetBounds(out int x, out int y, out int wide, out int tall);
		Surface.GetWorkspaceBounds(out int workX, out int workY, out int workWide, out int workTall);

		if (x + wide > workX + workWide)
		{
			x -= parentWide + wide;
			x -= 2;
		}
		else x += 1;

		if (y + tall > workY + workTall)
		{
			int lastWorkY = workY + workTall;
			int pixelsOffBottom = (y + tall) - lastWorkY;
			y -= pixelsOffBottom;
			y -= 2;
		}
		else y -= 1;

		SetPos(x, y);
		MoveToFront();
	}

	private void LayoutScrollBar() {
		Scroller!.SetEnabled(false);
		Scroller.SetRangeWindow(VisibleSortedItems.Count);
		Scroller.SetRange(0, CountVisibleItems());
		Scroller.SetButtonPressedScrollValue(1);

		GetSize(out int wide, out int tall);
		GetInset(out int ileft, out int iright, out int itop, out int ibottom);

		wide -= iright;

		Scroller.SetPos(wide - Scroller.GetWide(), 1);
		Scroller.SetSize(Scroller.GetWide(), tall - ibottom - itop);
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
				menuItem.GetContentSize(out int wide, out _);
				if (wide > MenuWide - Label.Content) {
					MenuWide = wide + Label.Content;
				}
			}
		}

		if (MenuWide < MinimumWidth)
			MenuWide = MinimumWidth;

		RecalculateWidth = false;
	}

	public override void SetVisible(bool state)
	{
		if (state == IsVisible())
			return;

		if (state == false)
		{
			PostActionSignal(new KeyValues("MenuClose"));
			//CloseOtherMenus(null);

			SetCurrentlySelectedItem(-1);
		} else
		{
			MoveToFront();
			RequestFocus();

			// MenuMgr
		}

		base.SetVisible(state);
		SizedForScrollBar = false;
	}

	protected virtual void LayoutMenuBorder() {
		IScheme? scheme = GetScheme();
		IBorder? menuBorder = scheme?.GetBorder("MenuBorder");
		if (menuBorder != null)
			SetBorder(menuBorder);
	}

	public override void OnKeyCodeTyped(ButtonCode code)
	{
		if (!IsEnabled())
			return;

		bool alt = Input.IsKeyDown(ButtonCode.KeyLAlt) || Input.IsKeyDown(ButtonCode.KeyRAlt);
		if (alt)
		{
			base.OnKeyCodeTyped(code);

			//if (TypeAheadMode != TYPE_AHEAD_MODE)
			//PostActionSignal(new KeyValues("MenuClose"))
		}

		switch (code)
		{
			case ButtonCode.KeyEscape:
				SetVisible(false);
				break;
			case ButtonCode.KeyUp:
				MoveAlongMenuItemList(MENU_UP, 0);
				if (MenuItems[CurrentlySelectedItemID] != null)
					MenuItems[CurrentlySelectedItemID].SetArmed(true);
				else
					base.OnKeyCodeTyped(code);
				break;
			case ButtonCode.KeyDown:
				MoveAlongMenuItemList(MENU_DOWN, 0);
				if (MenuItems[CurrentlySelectedItemID] != null)
					MenuItems[CurrentlySelectedItemID].SetArmed(true);
				else
					base.OnKeyCodeTyped(code);
				break;
			case ButtonCode.KeyRight:
				if (MenuItems[CurrentlySelectedItemID] != null)
					ActivateItem(CurrentlySelectedItemID);
				else
					base.OnKeyCodeTyped(code);
				break;
			case ButtonCode.KeyLeft:
				if (GetParentMenuItem() != null)
					SetVisible(false);
				else
					base.OnKeyCodeTyped(code);
				break;
			case ButtonCode.KeyEnter:
				if (MenuItems[CurrentlySelectedItemID] != null)
					ActivateItem(CurrentlySelectedItemID);
				else
					base.OnKeyCodeTyped(code);
				break;
			case ButtonCode.KeyPageUp:
				if (NumVisibleLines > 1)
				{
					if (CurrentlySelectedItemID < NumVisibleLines)
						MoveAlongMenuItemList(MENU_UP * CurrentlySelectedItemID, 0);
					else
						MoveAlongMenuItemList(MENU_UP * (NumVisibleLines - 1), 0);
				}
				else
					MoveAlongMenuItemList(MENU_UP, 0);

				if (MenuItems[CurrentlySelectedItemID] != null)
					MenuItems[CurrentlySelectedItemID].SetArmed(true);
				break;
			case ButtonCode.KeyPageDown:
				if (NumVisibleLines > 1)
				{
					if (CurrentlySelectedItemID + NumVisibleLines >= MenuItems.Count)
						MoveAlongMenuItemList(MENU_DOWN * (MenuItems.Count - CurrentlySelectedItemID - 1), 0);
					else
						MoveAlongMenuItemList(MENU_DOWN * (NumVisibleLines - 1), 0);
				}
				else
					MoveAlongMenuItemList(MENU_DOWN, 0);

				if (MenuItems[CurrentlySelectedItemID] != null)
					MenuItems[CurrentlySelectedItemID].SetArmed(true);
				break;
			case ButtonCode.KeyHome:
				MoveAlongMenuItemList(MENU_UP * CurrentlySelectedItemID, 0);
				if (MenuItems[CurrentlySelectedItemID] != null)
					MenuItems[CurrentlySelectedItemID].SetArmed(true);
				break;
			case ButtonCode.KeyEnd:
				MoveAlongMenuItemList(MENU_DOWN * (MenuItems.Count - CurrentlySelectedItemID - 1), 0);
				if (MenuItems[CurrentlySelectedItemID] != null)
					MenuItems[CurrentlySelectedItemID].SetArmed(true);
				break;
		}
	}

	public void ActivateItem(int itemID)
	{
		if (MenuItems[itemID] != null)
		{
			MenuItem menuItem = MenuItems[itemID];

			if (menuItem != null && menuItem.IsEnabled())
			{
				menuItem.FireActionSignal();
				ActivatedItem = itemID;
			}
		}
	}

	public int GetActiveItem()
	{
		return ActivatedItem;
	}

	public void SetCurrentlySelectedItem(int itemID)
	{
		if (itemID == CurrentlySelectedItemID)
			return;

		if (MenuItems[CurrentlySelectedItemID] != null)
			MenuItems[CurrentlySelectedItemID].SetArmed(false);

		PostActionSignal(new KeyValues("MenuItemHighlight", "itemID", itemID));
		CurrentlySelectedItemID = itemID;
	}

	private void MoveAlongMenuItemList(int direction, int loopCount)
	{
		if (MenuItems.Count == 0)
			return;

		int itemID = CurrentlySelectedItemID;
		int row = SortedItems.FindIndex(x => x == itemID);
		row += direction;

		if (row > SortedItems.Count - 1)
		{
			if (Scroller!.IsVisible())
				row = SortedItems.Count - 1;
			else
				row = 0;
		}
		else if (row < 0)
		{
			if (Scroller!.IsVisible())
				row = Scroller.GetValue();
			else
				row = SortedItems.Count - 1;
		}

		if (Scroller!.IsVisible())
		{
			if (row > Scroller.GetValue() + NumVisibleLines - 1)
			{
				int val = Scroller.GetValue();
				val -= -direction;

				Scroller.SetValue(val);

				InvalidateLayout();
			}
			else if (row < Scroller.GetValue())
			{
				int val = Scroller.GetValue();
				val -= -direction;

				Scroller.SetValue(val);

				InvalidateLayout();
			}

			if ((row > Scroller.GetValue() + NumVisibleLines - 1) || (row < Scroller.GetValue()))
				Scroller.SetValue(row);

			if (SortedItems.FindIndex(x => x == row) != -1)
				SetCurrentlySelectedItem(SortedItems[row]);

			if(loopCount < MenuItems.Count) {
				char[] text = new char[256];
				MenuItems[CurrentlySelectedItemID].GetText(text);
				if (text[0] == 0 || !MenuItems[CurrentlySelectedItemID].IsVisible())
					MoveAlongMenuItemList(direction, loopCount + 1);
			}
		}
	}

	private void MakeItemsVisibleInScrollRange(int maxVisibleItems, int numPixelsAvailable) {
		foreach (var item in MenuItems)
			item.SetBounds(0, 0, 0, 0);

		for (int i = 0; i < SeparatorPanels.Count; ++i)
			SeparatorPanels[i].SetVisible(false);

		VisibleSortedItems.Clear();

		int tall = 0;

		int startItem = Scroller!.GetValue();
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
			int sepIndex = Separators.FindIndex(x => x == itemId);
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
		Scroller!.SetVisible(false);
		SizedForScrollBar = false;
	}

	private void AddScrollBar() {
		Scroller!.SetVisible(true);
		SizedForScrollBar = true;
	}

	private void ComputeWorkspaceSize(out int workWide, out int workTall) {
		GetInset(out _, out _, out int top, out int bottom);

		Surface.GetWorkspaceBounds(out _, out _, out workWide, out workTall);
		workTall -= 20;
		workTall -= top;
		workTall -= bottom;
	}

	private int ComputeFullMenuHeightWithInsets() {
		GetInset(out _, out _, out int top, out int bottom);

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
			for (int j = 0; j < Separators.Count; j++) {
				if (Separators[j] == itemId) {
					totalTall += separatorHeight;
					break;
				}
			}
		}

		return totalTall;
	}

	public void ForceCalculateWidth() {
		RecalculateWidth = true;
		CalculateWidth();
		PerformLayout();
	}
}
