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
	KeyValues? userData;
	Menu? CascadeMenu;
	bool Checkable;
	bool Checked;

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
		Assert(!(cascadeMenu != null && checkable));
		Init();
	}

	public MenuItem(Menu parent, string text, KeyValues message, Panel target) : base(parent, text, text)
	{
		SetCommand(message);
		if (target != null)
		{
			AddActionSignalTarget(target);
		}
		Init();
	}

	new void Init()
	{
		if (CascadeMenu != null)
		{
			CascadeMenu.SetParent(this);
			CascadeMenu.AddActionSignalTarget(this);
		}
		else if (Checkable)
		{
			// todo
		}

		SetButtonBorderEnabled(false);
		SetUseCaptureMouse(false);
		ContentAlignment = Alignment.West;
	}
	public KeyValues? GetUserData() => userData;
	public void SetUserData(KeyValues? kv)
	{
		userData = null;
		userData = kv?.MakeCopy();
	}
	public override void PaintBackground() {

	}

	public Menu? GetParentMenu() => GetParent() is Menu menu ? menu : null;

	public override void ApplySchemeSettings(IScheme scheme)
	{
		base.ApplySchemeSettings(scheme);

		SetDefaultColor(GetSchemeColor("Menu.TextColor", GetFgColor(), scheme), GetSchemeColor("Menu.BgColor", GetBgColor(), scheme));
		SetArmedColor(GetSchemeColor("Menu.ArmedTextColor", GetFgColor(), scheme), GetSchemeColor("Menu.ArmedBgColor", GetBgColor(), scheme));
		SetDepressedColor(GetSchemeColor("Menu.ArmedTextColor", GetFgColor(), scheme), GetSchemeColor("Menu.ArmedBgColor", GetBgColor(), scheme));

		SetTextInset(int.TryParse(scheme.GetResourceString("Menu.TextInset"), out int r) ? r : 0, 0);

		GetParentMenu()?.ForceCalculateWidth();
	}

	internal bool IsCheckable()
	{
		return Checkable;
	}

	public bool IsChecked()
	{
		return Checked;
	}

	public bool HasMenu()
	{
		return CascadeMenu != null;
	}

	public Menu? GetMenu()
	{
		return CascadeMenu;
	}
}
