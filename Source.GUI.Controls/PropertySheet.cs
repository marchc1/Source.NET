﻿using Source.Common.Formats.Keyvalues;
using Source.Common.GUI;
using Source.Common.Input;

namespace Source.GUI.Controls;

class ContextLabel : Label
{
	private Button TabButton;
	public ContextLabel(Button parent, string panelName, string text) : base(parent, panelName, text) {
		TabButton = parent;
		// SetBlockDragChaining(true);
	}

	public override void OnMousePressed(ButtonCode code) {
		if (TabButton != null)
			TabButton.FireActionSignal();
	}

	public override void OnMouseReleased(ButtonCode code) {
		base.OnMouseReleased(code);
		if (GetParent() != null)
			GetParent()!.OnCommand("ShowContextMenu");
	}

	public override void ApplySchemeSettings(IScheme scheme) {
		base.ApplySchemeSettings(scheme);

		IFont marlett = scheme.GetFont("Marlett")!;
		SetFont(marlett);
		SetTextInset(0, 0);
		SetContentAlignment(Alignment.Northwest);

		if (GetParent() != null) {
			SetFgColor(scheme.GetColor("Button.TextColor", GetParent()!.GetFgColor()));
			SetBgColor(GetParent()!.GetBgColor());
		}
	}
}

class PageTab : Button
{
	bool Active;
	Color TextColor;
	Color DimTextColor;
	int MaxTabWidth;
	IBorder ActiveBorder;
	IBorder NormalBorder;
	PropertySheet Parent;
	Panel Page;
	ImagePanel? Image;
	char[] ImageName;
	bool ShowContextLabel;
	bool AtttemptingDrop;
	ContextLabel ContextLabel;
	long HoverActivePageTime;
	long DropHoverTime;

	public PageTab(PropertySheet parent, string panelName, string text, ReadOnlySpan<char> imageName, int maxTabWidth, Panel page, bool showContextButton, long hoverActivePageTime = -1)
		: base(parent, panelName, text) {
		Parent = parent;
		Page = page;
		Image = null;
		ShowContextLabel = showContextButton;
		AtttemptingDrop = false;
		HoverActivePageTime = hoverActivePageTime;
		DropHoverTime = -1;

		SetCommand(new KeyValues("TabPressed"));
		Active = false;
		MaxTabWidth = maxTabWidth;
		SetDropEnabled(true);
		// SetDragEnabled(parent.IsDraggableTab());

		if (!imageName.IsEmpty) {
			Image = new(this, text);
			ImageName = new char[imageName.Length];
			imageName.CopyTo(ImageName);
		}

		SetMouseClickEnabled(ButtonCode.MouseRight, true);

		ContextLabel = ShowContextLabel ? new(this, "Context", "9") : null!;
	}

	public override void OnCursorEntered() {
		DropHoverTime = System.GetTimeMillis();
	}

	public override void OnCursorExited() {
		DropHoverTime = -1;
	}

	public override void OnThink() {
		if (AtttemptingDrop && HoverActivePageTime >= 0 && DropHoverTime >= 0) {
			long hoverTime = System.GetTimeMillis() - DropHoverTime;
			if (hoverTime > HoverActivePageTime) {
				FireActionSignal();
				SetSelected(true);
				Repaint();
			}
		}
		AtttemptingDrop = false;

		base.OnThink();
	}

	public bool IsDroppable(List<KeyValues> msglist) {
		AtttemptingDrop = true;

		if (GetParent() == null)
			return false;

		// PropertySheet sheet = IsDroppingSheet(msglist);
		// if (sheet != null)
		// 	return GetParent().IsDroppable(msglist);

		// return base.IsDroppable();
		return false;
	}

	public void OnDroppablePanelPaint() {

	}

	public void OnPanelDropped() {

	}

	public void OnDragFailed() {

	}

	public void OnCreateDragData() {

	}

	public override void ApplySchemeSettings(IScheme scheme) {
		base.ApplySchemeSettings(scheme);

		TextColor = GetSchemeColor("PropertySheet.SelectedTextColor", GetFgColor(), scheme);
		DimTextColor = GetSchemeColor("PropertySheet.TextColor", new(128, 128, 128, 255), scheme);
		ActiveBorder = scheme.GetBorder("TabActiveBorder")!;
		NormalBorder = scheme.GetBorder("TabBorder")!;

		if (Image != null) {
			ClearImages();

			// Image.SetImage(SchemeManager.GetImage(ImageName, false)!);
			// AddImage(Image.GetImage(), 2);
			Image.GetSize(out int w, out int h);
			w += ContextLabel != null ? 10 : 0;
			if (ContextLabel != null)
				Image.SetPos(10, 0);
			SetSize(w + 4, h + 2);
		}
		else {
			GetSize(out _, out int tall);
			GetContentSize(out int contentWide, out _);

			int wide = Math.Max(MaxTabWidth, contentWide + 10);
			wide += ContextLabel != null ? 10 : 0;
			SetSize(wide, tall);
		}

		if (ContextLabel != null)
			SetTextInset(12, 0);
	}

	public override void ApplySettings(KeyValues resourceData) {
		ReadOnlySpan<char> Border = resourceData.GetString("activeborder_override", "");
		if (!Border.IsEmpty)
			ActiveBorder = GetScheme()!.GetBorder(Border)!;

		Border = resourceData.GetString("normalborder_override", "");
		if (!Border.IsEmpty)
			NormalBorder = GetScheme()!.GetBorder(Border)!;

		base.ApplySettings(resourceData);
	}

	public override void OnCommand(ReadOnlySpan<char> command) {
		if (command.Equals("ShowContextMenu", StringComparison.OrdinalIgnoreCase)) {
			KeyValues kv = new("OpenContextMenu");
			kv.SetPtr("page", Page);
			kv.SetPtr("contextlabel", ContextLabel);
			PostActionSignal(kv);
			return;
		}

		base.OnCommand(command);
	}

	public override IBorder? GetBorder(bool depressed, bool armed, bool seleced, bool keyfocus) {
		if (Active)
			return ActiveBorder;
		return NormalBorder;
	}

	public override Color GetButtonFgColor() {
		if (Active)
			return TextColor;
		else
			return DimTextColor;
	}

	public void SetActive(bool state) {
		Active = state;
		SetZPos(state ? 100 : 0);
		InvalidateLayout();
		Repaint();
	}

	public void SetTabWidth(int width) {
		MaxTabWidth = width;
		InvalidateLayout();
	}

	public override bool CanBeDefaultButton() {
		return false;
	}

	public override void OnMousePressed(ButtonCode code) {
		if (!IsEnabled())
			return;

		if (!IsMouseClickEnabled(code))
			return;

		if (IsUseCaptureMouseEnabled()) {
			RequestFocus();
			FireActionSignal();
			SetSelected(true);
			Repaint();
			Input.SetMouseCapture(this);
		}
	}

	public override void OnMouseReleased(ButtonCode code) {
		if (IsUseCaptureMouseEnabled())
			Input.SetMouseCapture(null);

		SetSelected(false);
		Repaint();

		if (code == ButtonCode.MouseRight) {
			KeyValues kv = new("OpenContextMenu");
			kv.SetPtr("page", Page);
			kv.SetPtr("contextlabel", ContextLabel);
			PostActionSignal(kv);
		}
	}

	public override void PerformLayout() {
		base.PerformLayout();

		if (ContextLabel != null) {
			GetSize(out _, out int h);
			ContextLabel.SetBounds(0, 0, 10, h);
		}
	}
}

public class PropertySheet : EditablePanel {
	struct Page {
		public Panel page;
		public bool ContextMenu;

		public Page(Panel pagePnl, bool contextMenu = false) {
			page = pagePnl;
			ContextMenu = contextMenu;
		}
	}

	List<Page> Pages;
	List<PageTab> PageTabs;
	Panel? ActivePage;
	PageTab? ActiveTab;
	int TabWidth;
	int ActiveTabIndex;
	ComboBox? Combo;
	bool ShowTabs;
	bool TabFocus;
	// PHandle PreviouslyActivePage
	float PageTransitionEffectTime;
	bool SmallTabs;
	IFont TabFont;
	bool DraggableTabs;
	bool ContextButton;
	bool KBNavigationEnabled;
	[PanelAnimationVar("tabxident", "0", "int")] protected int TabXIndent;
	[PanelAnimationVar("tabxdelta", "0", "int")] protected int TabXDelta;
	[PanelAnimationVar("tabxfittotext", "1", "int")] protected int TabFitText;
	[PanelAnimationVar("tabheight", "28", "int")] protected int SpecifiedTabHeight;
	[PanelAnimationVar("tabheight_small", "14", "int")] protected int SpecifiedTabHeightSmall;
	int TabHeight;
	int TabHeightSmall;
	KeyValues? TabKV;

	public PropertySheet(Panel? parent, string? panelName, bool draggableTabs = false) : base(parent, panelName) {
		Pages = new();
		PageTabs = new();
		ActivePage = null;
		ActiveTab = null;
		TabWidth = 64;
		ActiveTabIndex = -1;
		ShowTabs = true;
		Combo = null;
		TabFocus = false;
		PageTransitionEffectTime = 0.0f;
		SmallTabs = false;
		// TabFont = 0;
		DraggableTabs = draggableTabs;
		TabKV = null;
		TabHeight = 0;
		TabHeightSmall = 0;

		if (DraggableTabs)
			SetDropEnabled(true);

		KBNavigationEnabled = true;
	}

	public PropertySheet(Panel? parent, string? panelName, ComboBox combo) : base(parent, panelName) {
		Pages = new();
		PageTabs = new();
		ActivePage = null;
		ActiveTab = null;
		TabWidth = 64;
		ActiveTabIndex = -1;
		Combo = combo;
		Combo.AddActionSignalTarget(this);
		ShowTabs = false;
		TabFocus = false;
		PageTransitionEffectTime = 0.0f;
		SmallTabs = false;
		// TabFont = 0;
		DraggableTabs = false;
		TabKV = null;
		TabHeight = 0;
		TabHeightSmall = 0;
	}

	public bool IsDraggableTab() => DraggableTabs;

	public void SetDraggableTabs(bool state) => DraggableTabs = state;

	public void SetSmallTabs(bool state) {
		SmallTabs = state;
		TabFont = GetScheme()!.GetFont(SmallTabs ? "DefaultVerySmall" : "DefaultSmall")!;
		for (int i = 0; i < PageTabs.Count; i++)
			PageTabs[i].SetFont(TabFont);
	}

	public bool IsSmallTabs() => SmallTabs;

	public void ShowContextButtons(bool state) => ContextButton = state;

	public bool ShouldShowContextButtons() => ContextButton;

	public int FindPage(Panel page) {
		for (int i = 0; i < Pages.Count; i++)
			if (Pages[i].page == page)
				return i;

		return -1;
	}

	public void AddPage(Panel page, ReadOnlySpan<char> title, char[]? imageName = null, bool hasContextMenu = false) {
		if (page == null)
			return;

		if (FindPage(page) != -1)
			return;

		long hoverActivePageTime = 250;
		PageTab tab = new(this, "Tab", title.ToString(), imageName, TabWidth, page, ContextButton && hasContextMenu, hoverActivePageTime);
		// if (DraggableTabs)
		// tab.SetDragEnabled(true);

		tab.SetFont(TabFont);
		if (ShowTabs)
			tab.AddActionSignalTarget(this);
		else if (Combo != null)
			Combo.AddItem(title.ToString(), null);

		if (TabKV != null)
			tab.ApplySettings(TabKV);

		PageTabs.Add(tab);

		Page info;
		info.page = page;
		info.ContextMenu = hasContextMenu;

		Pages.Add(info);

		page.SetParent(this);
		page.AddActionSignalTarget(this);
		PostMessage(page, new KeyValues("ResetData")); // todo static kv

		page.SetVisible(false);
		InvalidateLayout();

		if (ActivePage == null) {
			ChangeActiveTab(0);
			if (ActivePage != null)
				ActivePage.RequestFocus();
		}
	}

	public void SetActivePage(Panel page) {
		int index = FindPage(page);
		if (index == -1)
			return;

		ChangeActiveTab(index);
	}

	public void SetTabWidth(int pixels) {
		if (pixels < 0) {
			if (ActiveTab == null)
				return;

			ActiveTab.GetContentSize(out pixels, out _);
		}

		if (TabWidth == pixels)
			return;

		TabWidth = pixels;
		InvalidateLayout();
	}

	public void ResetAllData() {
		for (int i = 0; i < Pages.Count; i++)
			Pages[i].page.SendMessage(new KeyValues("ResetData"), this); // todo static kv
	}

	public void ApplyChanges() {
		for (int i = 0; i < Pages.Count; i++)
			Pages[i].page.SendMessage(new KeyValues("ApplyChanges"), this); // todo static kv
	}

	public Panel? GetActivePage() => ActivePage;

	public Panel? GetActiveTab() => ActiveTab;

	public int GetNumPages() => Pages.Count;

	public void GetActiveTabTitle(Span<char> textOut) {
		if (ActiveTab != null) ActiveTab.GetText(textOut);
	}

	public bool GetTabTitle(int i, Span<char> textOut) {
		if (i < 0 || i >= Pages.Count) {
			textOut.Clear();
			return false;
		}

		PageTabs[i].GetText(textOut);
		return true;
	}

	public bool SetTabTitle(int i, string title) {
		if (i < 0 || i >= Pages.Count)
			return false;

		PageTabs[i].SetText(title);
		return true;
	}

	public int GetActivePageNum() {
		for (int i = 0; i < Pages.Count; i++) {
			if (Pages[i].page == ActivePage)
				return i;
		}

		return -1;
	}

	public override void RequestFocus(int direction) {
		if (direction == -1 || direction == 0) {
			if (ActivePage != null) {
				ActivePage.RequestFocus(direction);
				TabFocus = false;
			}
		} else {
			if (ShowTabs && ActiveTab != null) {
				ActiveTab.RequestFocus(direction);
				TabFocus = true;
			} else if (ActivePage != null) {
				ActivePage.RequestFocus(direction);
				TabFocus = false;
			}
		}
	}

	public override bool RequestFocusPrev(IPanel? panel) {
		if (TabFocus || !ShowTabs || ActiveTab == null) {
			TabFocus = false;
			return base.RequestFocusPrev(panel);
		} else {
			if (GetParent() != null)
				PostMessage(GetParent()!, new KeyValues("FindDefaultButton")); // todo: make static kv
			ActiveTab.RequestFocus(-1);
			TabFocus = true;
			return true;
		}
	}

	public override bool RequestFocusNext(IPanel? panel) {
		if (!TabFocus || ActivePage == null)
			return base.RequestFocusNext(panel);
		else {
			if (ActiveTab == null)
				return base.RequestFocusNext(panel);
			else {
				ActivePage.RequestFocus(1);
				TabFocus = true;
				return true;
			}
		}
	}

	public override void ApplySchemeSettings(IScheme scheme) {
		base.ApplySchemeSettings(scheme);

		IBorder Border = scheme.GetBorder("PropertySheetBorder")!;
		if (Border == scheme.GetBorder("Default"))
			Border = scheme.GetBorder("RaisedBorder")!;

		SetBorder(Border);
		PageTransitionEffectTime = float.Parse(scheme.GetResourceString("PropertySheet.TransitionEffectTime"));
		TabFont = scheme.GetFont(SmallTabs ? "DefaultVerySmall" : "DefaultSmall")!;

		if (TabKV != null)
			for (int i = 0; i < PageTabs.Count; i++)
				PageTabs[i].ApplySettings(TabKV);

		if (IsProportional()) {
			TabHeight = SchemeManager.GetProportionalScaledValueEx(GetScheme()!, SpecifiedTabHeight);
			TabHeightSmall = SchemeManager.GetProportionalScaledValueEx(GetScheme()!, SpecifiedTabHeightSmall);
		} else {
			TabHeight = SpecifiedTabHeight;
			TabHeightSmall = SpecifiedTabHeightSmall;
		}
	}

	public override void ApplySettings(KeyValues resourceData) {
		base.ApplySettings(resourceData);

		KeyValues pTabKV = resourceData.FindKey("tabskv")!;
		if (pTabKV != null) {
			if (TabKV != null)
				TabKV = null;

			TabKV = new KeyValues("tabkv");
			TabKV.CopySubkeys(pTabKV);
		}

		KeyValues TabWidthKV = resourceData.FindKey("tabwidth")!;
		if (TabWidthKV != null) {
			TabWidth = SchemeManager.GetProportionalScaledValueEx(GetScheme()!, TabWidthKV.GetInt());
			for (int i = 0; i < PageTabs.Count; i++)
				PageTabs[i].SetWide(TabWidth);
		}

		KeyValues TransitionKV = resourceData.FindKey("transition_time")!;
		if (TransitionKV != null)
			PageTransitionEffectTime = TransitionKV.GetFloat();
	}

	public void PaintBorder() {
		IBorder border = GetBorder()!;
		if (border == null)
			return;

		int px = 0, py = 0, pwide = 0, ptall = 0;
		if (ActiveTab != null) {
			ActiveTab.GetBounds(out px, out py, out pwide, out ptall);
			ptall -= 1;
		}

		GetSize(out int wide, out int tall);
		border.Paint(0, py + ptall, wide, tall, Sides.Top, px + 1, px + pwide - 1);
	}

	public override void PerformLayout() {
		base.PerformLayout();

		GetBounds(out _, out _, out int wide, out int tall);
		if (ActivePage != null) {
			int tabHeight = IsSmallTabs() ? TabHeightSmall : TabHeight;
			if (ShowTabs)
				ActivePage.SetBounds(0, tabHeight, wide, tall - tabHeight);
			else
				ActivePage.SetBounds(0, 0, wide, tall);

			ActivePage.InvalidateLayout();

			int limit = PageTabs.Count;
			int xtab = TabXIndent;

			if (ShowTabs) {
				for (int i = 0; i < limit; i++) {
					PageTab tab = PageTabs[i];

					tab.GetSize(out int tabWide, out _);

					if (TabFitText != 0) {
						tab.SizeToContents();
						tabWide = tab.GetWide();

						tab.GetTextInset(out int XInset, out _);
						tabWide += XInset * 2;
					}

					if (tab == ActiveTab)
						tab.SetBounds(xtab, 2, tabWide, tabHeight);
					else
						tab.SetBounds(xtab, 4, tabWide, tabHeight);
					tab.SetVisible(true);
					xtab += tabWide + 1 + TabXDelta;
				}
			}
			else {
				for (int i = 0; i < limit; i++)
					PageTabs[i].SetVisible(false);
			}

			if (ActivePage != null) {
				ActivePage.MoveToFront();
				ActivePage.Repaint();
			}

			if (ActiveTab != null) {
				ActiveTab.MoveToFront();
				ActiveTab.Repaint();
			}
		}
	}

	public void OnTabPressed(Panel panel) {
		for (int i = 0; i < PageTabs.Count; i++) {
			if (PageTabs[i] == panel) {
				ChangeActiveTab(i);
				return;
			}
		}
	}

	public Panel GetPage(int i) {
		if (i < 0 || i >= Pages.Count)
			return null!;

		return Pages[i].page;
	}

	public void DisablePage(string title) {
		SetPageEnabled(title, false);
	}

	public void EnablePage(string title) {
		SetPageEnabled(title, true);
	}

	public void SetPageEnabled(string title, bool state) {
		Span<char> tmp = stackalloc char[50];
		for (int i = 0; i < Pages.Count; i++) {
			if (ShowTabs) {
				tmp.Clear();
				PageTabs[i].GetText(tmp);
				if (new string(tmp).Equals(title, StringComparison.OrdinalIgnoreCase))
					PageTabs[i].SetEnabled(state);
			} else {
				Combo.SetItemEnabled(title, state);
			}
		}
	}

	public void RemoveAllPages() {
		for (int i = Pages.Count - 1; i >= 0; --i)
			RemovePage(Pages[i].page);
	}

	public void DeleteAllPages() {
		for (int i = Pages.Count - 1; i >= 0; --i)
			DeletePage(Pages[i].page);
	}

	public void RemovePage(Panel panel) {
		int location = FindPage(panel);
		if (location == -1)
			return;

		// PreviouslyActivePage = ActivePage;
		ActiveTab = null;

		if (ShowTabs)
			PageTabs[location].RemoveActionSignalTarget(this);

		PageTab tab = PageTabs[location];
		PageTabs.RemoveAt(location);
		tab.MarkForDeletion();

		Pages.RemoveAt(location);

		panel.SetParent(null);

		if (ActivePage == panel) {
			ActivePage = null;
			ChangeActiveTab(Math.Max(location - 1, 0));
		}

		PerformLayout();
	}

	public void DeletePage(Panel panel) {
		Assert(panel != null);
		RemovePage(panel);
		panel.MarkForDeletion();
	}

	public void ChangeActiveTab(int index) {
		if (index < 0 || index >= Pages.Count) {
			ActiveTab = null;
			if (Pages.Count > 0) {
				ActivePage = null;
				if (index < 0)
					ChangeActiveTab(Pages.Count - 1);
				else
					ChangeActiveTab(0);
			}

			return;
		}

		if (Pages[index].page == ActivePage) {
			if (ActiveTab != null)
				ActiveTab.RequestFocus();
			TabFocus = true;
			return;
		}

		for (int i = 0; i < PageTabs.Count; i++)
			PageTabs[i].SetVisible(false);

		// PreviouslyActivePage = ActivePage;
		if (ActivePage != null) {
			VGui.PostMessage(ActivePage, new KeyValues("PageHide"), this);
			KeyValues msg = new("PageTabActivated");
			msg.SetPtr("panel", null);
			VGui.PostMessage(ActivePage, msg, this);
		}

		if (ActiveTab != null) {
			ActiveTab.SetActive(false);
			TabFocus = ActiveTab.HasFocus();
		}
		else TabFocus = false;

		ActivePage = Pages[index].page;
		ActiveTab = PageTabs[index];
		ActiveTabIndex = index;

		ActivePage.SetVisible(true);
		ActivePage.MoveToFront();

		ActiveTab.SetVisible(true);
		ActiveTab.MoveToFront();
		ActiveTab.SetActive(true);

		if (TabFocus)
			ActiveTab.RequestFocus();
		else
			ActivePage.RequestFocus();

		if (!ShowTabs)
			Combo.ActivateItemByRow(index);

		ActivePage.MakeReadyForUse();

		if (PageTransitionEffectTime != 0.0f) {
			// todo
		}
		else {

		}

		VGui.PostMessage(ActivePage, new KeyValues("PageShow"), this);

		KeyValues msg2 = new("PageTabActivated");
		msg2.SetPtr("panel", ActivePage);
		VGui.PostMessage(ActivePage, msg2, this);

		PostActionSignal(new KeyValues("PageChanged"));

		InvalidateLayout();
		Repaint();
	}

	// todo hotkeys
	public Panel? HasHotKey(char key) {
		return null;
	}

	public void OnOpenContextMenu(KeyValues parameters) {
		KeyValues kv = parameters.MakeCopy();
		PostActionSignal(kv);
		Panel page = (Panel)kv.GetPtr("page")!;
		if (page != null)
			PostMessage(page, kv.MakeCopy());
	}

	public override void OnKeyCodePressed(ButtonCode code) {
		bool shift = Input.IsKeyDown(ButtonCode.KeyLShift) || Input.IsKeyDown(ButtonCode.KeyRShift);
		bool ctrl = Input.IsKeyDown(ButtonCode.KeyLControl) || Input.IsKeyDown(ButtonCode.KeyRControl);
		bool alt = Input.IsKeyDown(ButtonCode.KeyLAlt) || Input.IsKeyDown(ButtonCode.KeyRAlt);

		if (ctrl && shift && alt && code == ButtonCode.KeyB) {
			EditablePanel? panel = (EditablePanel)GetActivePage()!;
			if (panel != null)
				panel.ActivateBuildMode();
		}

		if (IsKBNavigationEnabled()) {
			switch (code) {
				case ButtonCode.KeyRight:
					ChangeActiveTab(ActiveTabIndex + 1);
					break;
				case ButtonCode.KeyLeft:
					ChangeActiveTab(ActiveTabIndex - 1);
					break;
				default:
					base.OnKeyCodePressed(code);
					break;
			}
		} else
			base.OnKeyCodePressed(code);
	}

	public void OnTextChanged(Panel panel, string text) {
		if (panel == Combo) {
			Span<char> tabText = stackalloc char[30];
			for (int i = 0; i < PageTabs.Count; i++) {
				tabText.Clear();
				PageTabs[i].GetText(tabText);
				if (MemoryExtensions.Equals(tabText, text, StringComparison.OrdinalIgnoreCase))
					ChangeActiveTab(i);
			}
		}
	}

	public override void OnCommand(ReadOnlySpan<char> command) {
		if (command.Equals("Close", StringComparison.OrdinalIgnoreCase) && GetParent() != null)
			CallParentFunction(new KeyValues("Command", "command", command));
	}

	public override void OnMessage(KeyValues message, IPanel? from) {
		switch (message.Name) {
			case "TabPressed":
				OnTabPressed((Panel)from!);
				return;
			case "TextChanged":
				OnTextChanged((Panel)from!, message.GetString("text", "").ToString());
				return;
			case "OpenContextMenu":
				OnOpenContextMenu(message);
				return;
			case "ApplyButtonEnable":
				OnApplyButtonEnable();
				return;
			case "DefaultButtonSet":
				OnDefaultButtonSet((Panel)message.GetPtr("button")!);
				return;
			case "CurrentDefaultButtonSet":
				OnCurrentDefaultButtonSet((Panel)message.GetPtr("button")!);
				return;
			case "FindDefaultButton":
				OnFindDefaultButton();
				return;
			default:
				base.OnMessage(message, from);
				return;
		}
	}

	public void OnApplyButtonEnable() {
		PostActionSignal(new KeyValues("ApplyButtonEnable"));// todo: make static kv
	}

	public void OnCurrentDefaultButtonSet(Panel defaultButton) {
		if (GetParent() != null) {
			// KeyValues msg = new("CurrentDefaultButtonSet");
			// msg.SetInt("button", VGui.PanelToHandle(defaultButton));
			// PostMessage(GetParent()!, msg);
		}
	}

	public void OnDefaultButtonSet(Panel defaultButton) {
		if (GetParent() != null) {
			// KeyValues msg = new("DefaultButtonSet");
			// msg.SetInt("button", VGui.PanelToHandle(defaultButton));
			// PostMessage(GetParent()!, msg);
		}
	}

	public void OnFindDefaultButton() {
		if (GetParent() != null)
			CallParentFunction(new KeyValues("FindDefaultButton")); // todo: make static kv
	}

	public bool PageHasContextMenu(Panel page) {
		int pageNum = FindPage(page);
		if (pageNum == -1)
			return false;

		return Pages[pageNum].ContextMenu;
	}

	public void OnPanelDropped(List<KeyValues> msglist) {
		if (msglist.Count != 1)
			return;

		PropertySheet sheet = IsDroppingSheet(msglist)!;
		if (sheet == null) {
			// if (ActivePage != null && ActivePage.IsDropEnabled())
			// return ActivePage.OnPanelDropped(msglist);
			return;
		}

		KeyValues data = msglist[0];
		Panel page = (Panel)data.GetPtr("propertypage")!;
		ReadOnlySpan<char> title = data.GetString("tabname", "");
		if (page == null || sheet == null)
			return;

		// ToolWindow tw = (ToolWindow)sheet.GetParent()!;

		// todo
	}

	public bool IsDroppable(List<KeyValues> msglist) {
		if (!DraggableTabs)
			return false;

		if (msglist.Count != 1)
			return false;

		Input.GetCursorPos(out int mx, out int my);
		ScreenToLocal(ref mx, ref my);

		int tabHeight = IsSmallTabs() ? TabHeightSmall : TabHeight;
		if (my > tabHeight)
			return false;

		PropertySheet sheet = IsDroppingSheet(msglist)!;
		if (sheet == null)
			return false;

		if (sheet == this)
			return false;

		return true;
	}

	public void OnDroppablePanelPaint(List<KeyValues> msglist, List<Panel> dragPanels) {
		GetSize(out int w, out int h);

		int tabHeight = IsSmallTabs() ? TabHeightSmall : TabHeight;
		h = tabHeight + 4;

		int x = 0, y = 0;
		LocalToScreen(ref x, ref y);

		// Surface.DrawSetColor(GetDropFrameColor()); todo
		Surface.DrawOutlinedRect(x, y, x + w, y + h);
		Surface.DrawOutlinedRect(x + 1, y + 1, x + w - 1, y + h - 1);

		if (!IsDroppable(msglist))
			return;

		if (!ShowTabs)
			return;

		x = 0;
		y = 2;
		w = 1;
		h = tabHeight;

		int last = PageTabs.Count;
		if (last != 0)
			PageTabs[last - 1].GetBounds(out x, out y, out w, out h);

		x += w + 1;

		KeyValues data = msglist[0];
		ReadOnlySpan<char> text = data.GetString("tabname", "");
		Assert(!text.IsEmpty);

		PageTab fakeTab = new(this, "FakeTab", text.ToString(), null, TabWidth, null, false);
		fakeTab.SetBounds(x, 4, w, tabHeight - 4);
		fakeTab.SetFont(TabFont);
		fakeTab.Repaint();
		Surface.SolveTraverse(fakeTab, true);
		Surface.SolveTraverse(fakeTab);
		fakeTab.MarkForDeletion();
	}

	public void SetKBNavigationEnabled(bool state) => KBNavigationEnabled = state;
	public bool IsKBNavigationEnabled() => KBNavigationEnabled;

	static PropertySheet? IsDroppingSheet(List<KeyValues> msglist) {
		if (msglist.Count == 1)
			return null;

		KeyValues data = msglist[0];
		PropertySheet sheet = (PropertySheet)data.GetPtr("propertysheet")!;
		if (sheet == null)
			return sheet;

		return null;
	}
}
