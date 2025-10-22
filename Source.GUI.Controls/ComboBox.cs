﻿using Source.Common.Formats.Keyvalues;
using Source.Common.GUI;
using Source.Common.Input;

namespace Source.GUI.Controls;

class ComboBoxButton : Button
{
	Color DisabledBgColor;

	public ComboBoxButton(Panel parent, ReadOnlySpan<char> name, ReadOnlySpan<char> text) : base(parent, name, text) {
		SetButtonActivationType(ActivationType.OnPressed);
	}

	public override void ApplySchemeSettings(IScheme scheme) {
		base.ApplySchemeSettings(scheme);

		SetFont(scheme.GetFont("Marlett", IsProportional()));
		SetContentAlignment(Alignment.West);

#if OSX
		SetTextInset(-3, 0);
#else
		SetTextInset(3, 0);
#endif
		SetDefaultBorder(scheme.GetBorder("ScrollBarButtonBorder"));

		SetDefaultColor(GetSchemeColor("ComboBoxButton.ArrowColor", scheme), GetSchemeColor("ComboBoxButton.BgColor", scheme));
		SetArmedColor(GetSchemeColor("ComboBoxButton.ArmedArrowColor", scheme), GetSchemeColor("ComboBoxButton.BgColor", scheme));
		SetDepressedColor(GetSchemeColor("ComboBoxButton.ArmedArrowColor", scheme), GetSchemeColor("ComboBoxButton.BgColor", scheme));
		DisabledBgColor = GetSchemeColor("ComboBoxButton.DisabledBgColor", scheme);
	}

	public override IBorder GetBorder(/*bool depressed, bool armed, bool selected, bool keyfocus*/) {
		return null!;
	}

	public override void OnCursorExited() {
		CallParentFunction(new KeyValues("CursorExited")); // todo: static kv
	}

	public override Color GetButtonFgColor() {
		if (IsEnabled())
			return base.GetButtonFgColor();
		return DisabledBgColor;
	}
}

public class ComboBox : TextEntry
{
	public static Panel Create_ComboBox() => new ComboBox(null, null, 5, true);

	Menu DropDown;
	ComboBoxButton Button;
	bool PreventTextChangeMessage;
	bool Highlight;
	MenuDirection Direction;
	int OpenOffsetY;
	char[] BorderOverride = new char[64];

	public ComboBox(Panel parent, string name, int numLines, bool allowEdit) : base(parent, name) {
		SetEditable(allowEdit);
		// SetHorizontalScrolling(false);

		DropDown = new Menu(this, null);
		DropDown.AddActionSignalTarget(this);
		DropDown.SetTypeAheadMode(MenuTypeAheadMode.TYPE_AHEAD_MODE);

		Button = new ComboBoxButton(this, "Button", "u");
		Button.SetCommand("ButtonClicked");
		Button.AddActionSignalTarget(this);

		// SetNumberOfEditLines(numLines);

		Highlight = false;
		Direction = MenuDirection.DOWN;
		OpenOffsetY = 0;
		PreventTextChangeMessage = false;
		BorderOverride[0] = '\0';
	}

	~ComboBox() {
		DropDown?.DeletePanel();
		Button?.DeletePanel();
	}

	// public void SetNumberOfEditLines(int numLines) => DropDown.SetNumberOfVisibleItems(numLines);

	public void AddItem(ReadOnlySpan<char> itemText, KeyValues? userData) {

	}

	public void DeleteItem(int itemID) {

	}

	public bool UpdateItem(int itemID, ReadOnlySpan<char> itemText, KeyValues? userData) {
		return false;//todo
	}

	// public bool IsItemIDValid(int itemID) => DropDown.IsValidMenuID(itemID);

	public void SetItemEnabled(ReadOnlySpan<char> itemText, bool state) => DropDown.SetItemEnabled(itemText, state);

	public void SetItemEnabled(int itemID, bool state) => DropDown.SetItemEnabled(itemID, state);

	public void RemoveAll() => DropDown.DeleteAllItems();

	public int GetItemCount() => DropDown.GetItemCount();

	public int GetItemIDFromRow(int row) => DropDown.GetMenuID(row);

	public void ActivateItem(int itemID) {

	}

	public void ActivateItemByRow(int row) {

	}

	public void SilentActivateItemByRow(int row) {

	}

	public void SilentActivateItem(int itemID) {

	}

	public void SetMenu(Menu menu) {

	}

	public override void PerformLayout() {
		GetPaintSize(out int wide, out int tall);
		base.PerformLayout();

		IFont buttonFont = Button.GetFont()!;

		int fontTall = Surface.GetFontTall(buttonFont);
		int buttonSize = Math.Min(tall, fontTall);
		int buttonY = (tall - 1 - buttonSize) / 2;

		Button.GetContentSize(out int buttonWide, out int buttonTall);
		buttonWide = Math.Max(buttonSize, buttonWide);

		Button.SetBounds(wide - buttonWide, buttonY, buttonWide, buttonSize);

		if (IsEditable())
			SetCursor(CursorCode.IBeam);
		else
			SetCursor(CursorCode.Arrow);

		Button.SetEnabled(IsEnabled());

		DoMenuLayout();
	}

	public void DoMenuLayout() {

	}

	public void SortItems() { }

	public int GetActiveItem() => DropDown.GetActiveItem();

	public KeyValues GetActiveItemUserData() => DropDown.GetItemUserData(GetActiveItem());

	public KeyValues GetItemUserData(int itemID) => DropDown.GetItemUserData(itemID);

	public void GetItemText(int itemID, Span<char> text) => DropDown.GetItemText(itemID, text);

	public bool IsDropdownVisible() => DropDown.IsVisible();

	public override void ApplySchemeSettings(IScheme scheme) {
		base.ApplySchemeSettings(scheme);
		SetBorder(scheme.GetBorder(BorderOverride.Length > 0 ? BorderOverride : "ComboBoxBorder"));
	}

	public override void ApplySettings(KeyValues resourceData) {
		base.ApplySettings(resourceData);

		ReadOnlySpan<char> border = resourceData.GetString("border_override", "");
		if (border.Length > 0)
			border.CopyTo(BorderOverride);

		KeyValues KVButton = resourceData.FindKey("Button")!;
		if (KVButton != null && Button != null)
			Button.ApplySettings(KVButton);
	}

	public void SetDropdownButtonVisible(bool state) =>
		Button.SetVisible(state);

	public override void OnMousePressed(ButtonCode code) {

	}

	public override void OnMouseDoublePressed(ButtonCode code) {

	}

	public override void OnCommand(ReadOnlySpan<char> command) {
		if (String.Equals(new(command), "ButtonClicked", StringComparison.OrdinalIgnoreCase))
			DoClick();

		base.OnCommand(command);
	}

	public void OnSetText() {

	}

	public void HideMenu() {
		if (DropDown == null)
			return;

		DropDown.SetVisible(false);
		Repaint();
		// OnHideMenu();
	}

	public void ShowMnenu() {
		if (DropDown == null)
			return;

		DropDown.SetVisible(true);
		DoClick();
	}

	public override void OnKillFocus(Panel? newPanel) {
		SelectNoText();
	}

	public void OnMenuClose() {

	}

	public void DoClick() {

	}

	public override void OnCursorEntered() {
		Button.OnCursorEntered();
		base.OnCursorEntered();
	}

	public override void OnCursorExited() {
		if (DropDown.IsVisible()) {
			Button.SetArmed(false);
			base.OnCursorExited();
		}
	}

	public void OnMenuItemSelected() {
		Highlight = true;

		int idx = GetActiveItem();
		if (idx >= 0) {
			Span<char> name = stackalloc char[256];
			GetItemText(idx, name);
			OnSetText(name);
		}

		Repaint();
	}

	public override void OnSizeChanged(int newWide, int newTall) {
		base.OnSizeChanged(newWide, newTall);
		//PerformLayout();
		//Button.GetSize(out int bwide, out _);
		//SetDrawWidth(newWide - bwide);
	}

	public override void OnSetFocus() {
		base.OnSetFocus();
		GotoTextEnd();
		SelectAllText(false);
	}

	public override void OnKeyCodePressed(ButtonCode code) {

	}

	public override void OnKeyCodeTyped(ButtonCode code) {

	}

	public override void OnKeyTyped(char ch) {

	}

	public void SelectMenuItem(int itemToSelect) {
		if (itemToSelect >= 0 && itemToSelect < DropDown.GetItemCount()) {
			Span<char> menuItemName = stackalloc char[255];

			int menuID = DropDown.GetMenuID(itemToSelect);
			DropDown.GetMenuItem(menuID)!.GetText(menuItemName);
			OnSetText(menuItemName);
			SelectAllText(false);
		}
	}

	public void MoveAlongMenuItemList(int direction) {

	}

	public void MoveToFirstMenuItem() =>
		SelectMenuItem(0);

	public void MoveToLastMenuItem() {
		SelectMenuItem(DropDown.GetItemCount() - 1);
	}

	public void SetOpenDirection(MenuDirection direction) => Direction = direction;

	public override void SetFont(IFont? font) {
		base.SetFont(font);
		DropDown.SetFont(font);
	}

	public override void SetUseFallbackFont(bool state, IFont Fallback) {
		base.SetUseFallbackFont(state, Fallback);
		DropDown.SetUseFallbackFont(state, Fallback);
	}

	public override void OnMessage(KeyValues message, IPanel? from) {
		switch (message.Name) {
			case "MenuItemSelected":
				OnMenuItemSelected();
				break;
			case "SetText":
				OnSetText(message.GetString("text", ""));
				break;
			case "MenuClosed":
				OnMenuClose();
				break;
			case "ActiveItem":
				ActivateItem(message.GetInt("itemID", -1));
				break;
		}

		base.OnMessage(message, from);
	}
}
