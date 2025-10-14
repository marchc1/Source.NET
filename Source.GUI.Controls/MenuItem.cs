using Source.Common.Formats.Keyvalues;
using Source.Common.GUI;

namespace Source.GUI.Controls;

public class MenuItemCheckImage : TextImage
{
	private MenuItem MenuItem;

	public MenuItemCheckImage(MenuItem item) : base("g")
	{
		MenuItem = item;
		SetSize(20, 13);
	}

	public override void Paint()
	{
		DrawSetTextFont(GetFont());

		DrawSetTextColor(MenuItem.GetBgColor());
		DrawPrintChar(0, 0, 'g');

		if (MenuItem.IsChecked())
		{
			if (MenuItem.IsEnabled())
			{
				DrawSetTextColor(MenuItem.GetButtonFgColor());
				DrawPrintChar(1, 3, 'a');
			} else
			{
				DrawSetTextColor(MenuItem.GetDisabledFgColor1());
				DrawPrintChar(1, 3, 'a');

				DrawSetTextColor(MenuItem.GetDisabledFgColor2());
				DrawPrintChar(0, 2, 'a');
			}
		}
	}
}

public class MenuItem : Button
{
	static MenuItem() => ChainToAnimationMap<MenuItem>();
	Menu? CascadeMenu;
	bool Checkable;
	bool Checked;
	TextImage? CasecadeArrow;
	Image? Check;
	// TextImage? BlankCheck;
	TextImage? CurrentKeyBinding;
	KeyValues? UserData;

	private int KEYBINDING_INSET = 5;
	private int CHECK_INSET = 6;

	public MenuItem(Menu parent, string name, string text) : base(parent, name, text)
	{
		ContentAlignment = Alignment.West;
		SetParent(parent);
		Init();
	}

	public MenuItem(Menu parent, string panelName, string text, Menu? cascadeMenu, bool checkable) : base(parent, panelName, text)
	{
		CascadeMenu = cascadeMenu;
		Checkable = checkable;
		SetButtonActivationType(ActivationType.OnReleased);
		UserData = null;
		CurrentKeyBinding	= null;
		Assert(!(cascadeMenu != null && checkable));
		Init();
	}

	new void Init()
	{
		CasecadeArrow = null;
		Check = null;

		if (CascadeMenu != null)
		{
			CascadeMenu.SetParent(this);
			CasecadeArrow = new TextImage("4");
			CascadeMenu.AddActionSignalTarget(this);
		}
		else if (Checkable)
		{
			// SetTextImageIndex(1);
			Check = new MenuItemCheckImage(this);
			// SetImageAtIndex(0, Check, 6);
			SetChecked(false);
		}

		SetButtonBorderEnabled(false);
		SetUseCaptureMouse(false);
		ContentAlignment = Alignment.West;
	}

	public Menu? GetParentMenu() => GetParent() is Menu menu ? menu : null;

	public override void PerformLayout()
	{
		base.PerformLayout();

		if (CasecadeArrow != null)
			CasecadeArrow.SetColor(GetButtonFgColor());
	}

	public void CloseCascadeMenu()
	{
		if (CascadeMenu != null)
		{
			if (CascadeMenu.IsVisible())
				CascadeMenu.SetVisible(false);

			SetArmed(false);
		}
	}

	public override void OnCursorMoved(int x, int  y)
	{
		if (GetParentMenu()!.GetMenuMode() == MenuMode.KEYBOARD)
			OnCursorEntered();

		CallParentFunction(new KeyValues("OnCursorMoved", "x", x, "y", y));
	}

	public override void OnCursorEntered()
	{
		KeyValues msg = new KeyValues("CursorEnteredMenuItem");
		msg.SetPtr("Panel", this);
		VGui.PostMessage(GetParent(), msg, null);
	}

	public override void OnCursorExited()
	{
		KeyValues msg = new KeyValues("CursorExitedMenuItem");
		msg.SetPtr("Panel", this);
		VGui.PostMessage(GetParent(), msg, null);
	}

	public void ArmItem() {
		GetParentMenu()!.CloseOtherMenus(null);
		SetArmed(true);

		Menu? parent = GetParentMenu();
		if (parent != null)
			parent.ForceCalculateWidth();

		Repaint();
	}

	public void DisarmItem() {
		if (CascadeMenu == null)
			base.OnCursorExited();

		Menu? parent = GetParentMenu();

		if (parent != null)
			parent.ForceCalculateWidth();

		Repaint();
	}

	public bool IsItemArmed() {
		return IsArmed();
	}

	public override void OnKillFocus(Panel? newPanel)
	{
		GetParentMenu()?.OnKillFocus(newPanel);
	}

	public override void FireActionSignal() {
		if (CascadeMenu == null)
		{
			KeyValues kv = new KeyValues("MenuItemSelected");
			kv.SetPtr("panel", this);
			VGui.PostMessage(GetParent(), kv, this);
			base.FireActionSignal();

			if (Checkable)
				SetChecked(!Checked);
		}
		else if (GetParentMenu()?.GetMenuMode() == MenuMode.KEYBOARD)
			OpenCasecadeMenu();
	}

	public void OpenCasecadeMenu()
	{
		if (CascadeMenu != null) {
			CascadeMenu.PerformLayout();
			CascadeMenu.SetVisible(true);
			CascadeMenu.MoveToFront();
		}
	}

	public bool HasMenu()
	{
		return CascadeMenu != null;
	}

	public override void ApplySchemeSettings(IScheme scheme)
	{
		base.ApplySchemeSettings(scheme);

		SetDefaultColor(GetSchemeColor("Menu.TextColor", GetFgColor(), scheme), GetSchemeColor("Menu.BgColor", GetBgColor(), scheme));
		SetArmedColor(GetSchemeColor("Menu.ArmedTextColor", GetFgColor(), scheme), GetSchemeColor("Menu.ArmedBgColor", GetBgColor(), scheme));
		SetDepressedColor(GetSchemeColor("Menu.ArmedTextColor", GetFgColor(), scheme), GetSchemeColor("Menu.ArmedBgColor", GetBgColor(), scheme));

		SetTextInset(int.TryParse(scheme.GetResourceString("Menu.TextInset"), out int r) ? r : 0, 0);

		GetParentMenu()?.ForceCalculateWidth();
	}

	public void GetTextImageSize(out int wide, out int tall)
	{
		GetTextImage().GetSize(out wide, out tall);
	}

	public void SetTextImageSize(int wide, int tall)
	{
		GetTextImage().SetSize(wide, tall);
	}

	public void GetArrowImageSize(out int wide, out int tall)
	{
		wide = 0;
		tall = 0;

		if (CasecadeArrow != null)
			CasecadeArrow.GetSize(out wide, out tall);
	}

	public void GetCheckImageSize(out int wide, out int tall)
	{
		wide = 0;
		tall = 0;

		if (Check != null)
		{
			// Check.ResizeImageToContent(); todo
			Check.GetSize(out wide, out tall);
			wide += CHECK_INSET;
		}
	}

	public Menu? GetMenu()
	{
		return CascadeMenu;
	}

	public IBorder? GetBorder(bool depressed, bool armed, bool selected, bool keyfocus) {
		return null;
	}

	public void OnKeyModeSet() {
		VGui.PostMessage(GetParent(), new KeyValues("KeyModeSet"), this);
	}

	internal bool IsCheckable()
	{
		return Checkable;
	}

	public bool IsChecked()
	{
		return Checked;
	}

	public void SetChecked(bool state)
	{
		if (Checkable)
			Checked = state;
	}

	public bool CanBeDefaultButton()
	{
		return false;
	}

	public KeyValues? GetUserData() {
		if (HasMenu())
			return CascadeMenu!.GetItemUserData(CascadeMenu.GetActiveItem());
		else
				return UserData;
	}
	public void SetUserData(KeyValues? kv)
	{
		UserData = null;
		UserData = kv?.MakeCopy();
	}

	public void SetCurrentKeyBinding(ReadOnlySpan<char> keyName)
	{
		if (keyName.Length == 0)
			return;


		if (CurrentKeyBinding == null)
			CurrentKeyBinding = new TextImage(keyName.ToString());
		else
		{
			char[] curtext = new char[256];
			CurrentKeyBinding.GetText(curtext);

			if (String.Compare(new string(curtext).TrimEnd('\0'), keyName.ToString(), StringComparison.Ordinal) != 0)
				return;

			CurrentKeyBinding.SetText(keyName);
		}

		InvalidateLayout(false, true);
	}

	public override void Paint()
	{
		base.Paint();
		if (CurrentKeyBinding == null)
			return;

		GetSize(out int w, out int h);
		CurrentKeyBinding.GetSize(out int iw, out int ih);

		int x = w - iw - KEYBINDING_INSET;
		int y = (h - ih) / 2;

		if (IsEnabled()) {
			CurrentKeyBinding.SetPos(x, y);
			CurrentKeyBinding.SetColor(GetButtonFgColor());
			CurrentKeyBinding.Paint();
		} else {
			CurrentKeyBinding.SetPos(x + 1, y + 1);
			CurrentKeyBinding.SetColor(GetDisabledFgColor1());
			CurrentKeyBinding.Paint();

			Surface.DrawFlushText();

			CurrentKeyBinding.SetPos(x, y);
			CurrentKeyBinding.SetColor(GetDisabledFgColor2());
			CurrentKeyBinding.Paint();
		}
	}

	public override void GetContentSize(out int wide, out int tall) {
		base.GetContentSize(out wide, out tall);
		if (CurrentKeyBinding == null)
			return;

		CurrentKeyBinding.GetSize(out int iw, out int ih);
		wide += iw + KEYBINDING_INSET;
		tall = Math.Max(tall, ih);
	}
}
