using Game.Shared;

using Source;
using Source.Common.Bitbuffers;
using Source.Common.Formats.Keyvalues;
using Source.Common.GUI;
using Source.GUI.Controls;

namespace Game.Client.HUD;

[DeclareHudElement(Name = "CHudMenu")]
class HudMenu : EditableHudElement
{
	const float MENU_SELECTION_TIMEOUT = 5.0f;
	const int MAX_MENU_STRING = 512;
	static char[] MenuString = new char[MAX_MENU_STRING];
	static char[] PrelocalisedMenuString = new char[MAX_MENU_STRING];

	struct ProcessedLine
	{
		public int MenuItem;
		public int StartChar;
		public int Length;
		public int Pixels;
		public int Height;
	}
	List<ProcessedLine> Processed = [];
	int MaxPixels;
	int Height;
	bool MenuDisplayed;
	int BitsValidSlots;
	double ShutoffTime;
	int WaitingForMore;
	int SelectedItem;
	bool MenuTakesInput;
	double SelectionTime;

	[PanelAnimationVar("OpenCloseTime", "1", "double")] protected double OpenCloseTime;
	[PanelAnimationVar("Blur", "0", "float")] protected float Blur;
	[PanelAnimationVar("TextScan", "1", "float")] protected float TextScan;
	[PanelAnimationVar("Alpha", "255.0", "float")] protected float AlphaOverride;
	[PanelAnimationVar("SelectionAlpha", "255.0", "float")] protected float SelectionAlphaOverride;
	[PanelAnimationVar("TextFont", "MenuTextFont", "font")] protected IFont TextFont;
	[PanelAnimationVar("ItemFont", "MenuItemFont", "font")] protected IFont ItemFont;
	[PanelAnimationVar("ItemFontPulsing", "MenuItemFontPulsing", "font")] protected IFont ItemFontPulsing;
	[PanelAnimationVar("MenuColor", "MenuColor", "color")] protected Color MenuColor;
	[PanelAnimationVar("MenuItemColor", "ItemColor", "color")] protected Color ItemColor;
	[PanelAnimationVar("MenuBoxColor", "MenuBoxBg", "color")] protected Color BoxColor;

	public HudMenu(string elementName) : base(null, "CHudMenu") {
		SelectedItem = -1;
		Panel parent = clientMode.GetViewport();
		SetParent(parent);
		((IHudElement)this).SetHiddenBits(HideHudBits.MiscStatus);
	}

	public override void Init() {
		IHudElement.HookMessage("ShowMenu", MsgFunc_ShowMenu);
		MenuTakesInput = false;
		MenuDisplayed = false;
		BitsValidSlots = 0;
		Processed.Clear();
		MaxPixels = 0;
		Height = 0;
		Reset();
	}

	void Reset() {
		PrelocalisedMenuString[0] = '\0';
		WaitingForMore = 0;
	}

	public bool IsMenuOpen() => MenuDisplayed && MenuTakesInput;

	void VidInit() { }

	public override void OnThink() {
		float selectionTimeout = MENU_SELECTION_TIMEOUT;
		if (MenuDisplayed && (gpGlobals.CurTime - SelectionTime) > selectionTimeout)
			MenuDisplayed = false;
	}

	bool ShouldDraw() {
		bool draw = IHudElement.DefaultShouldDraw(this) && MenuDisplayed;
		if (!draw)
			return false;

		if (ShutoffTime > 0.0f && ShutoffTime <= gpGlobals.RealTime) {
			MenuDisplayed = false;
			return false;
		}

		return draw;
	}

	void PaintString(ReadOnlySpan<char> text, int textLen, IFont font, int x, int y) {
		surface.DrawSetTextFont(font);
		surface.DrawSetTextPos(x, y);

		for (int i = 0; i < textLen; i++)
			surface.DrawChar(text[i]);
	}

	public override void Paint() {
		if (!MenuDisplayed)
			return;

		int x = 20;

		Color menuColor = MenuColor;
		Color itemColor = ItemColor;

		int c = Processed.Count;
		int border = 20;

		int wide = MaxPixels + border;
		int tall = Height + border;

		int y = (int)((ScreenHeight() - tall) * 0.5f);

		DrawBox(x - border / 2, y - border / 2, wide, tall, BoxColor, SelectionAlphaOverride / 255.0f);

		menuColor[3] = (byte)(menuColor[3] * (SelectionAlphaOverride / 255.0f));
		itemColor[3] = (byte)(itemColor[3] * (SelectionAlphaOverride / 255.0f));

		for (int i = 0; i < c; i++) {
			ProcessedLine line = Processed[i];
			Assert(line);

			Color clr = line.MenuItem != 0 ? itemColor : menuColor;

			bool canblur = false;
			if (line.MenuItem != 0 && SelectedItem >= 0 && (line.MenuItem == SelectedItem))
				canblur = true;

			surface.DrawSetTextColor(clr);

			int drawLen = line.Length;
			if (line.MenuItem != 0)
				drawLen *= (int)TextScan;

			surface.DrawSetTextFont(line.MenuItem != 0 ? ItemFont : TextFont);

			PaintString(MenuString[line.StartChar].ToString(), drawLen, line.MenuItem != 0 ? ItemFont : TextFont, x, y);

			if (canblur) {
				for (float fl = Blur; fl > 0.0f; fl -= 1.0f) {
					if (fl >= 1.0f)
						PaintString(MenuString[line.StartChar].ToString(), drawLen, ItemFontPulsing, x, y);
					else {
						Color col = clr;
						col[3] *= (byte)fl;
						surface.DrawSetTextColor(col);
						PaintString(MenuString[line.StartChar].ToString(), drawLen, ItemFontPulsing, x, y);
					}
				}
			}

			y += line.Height;
		}
	}

	public void SelectMenuItem(int menu_item) {
		if (menu_item > 0 && (BitsValidSlots & (1 << (menu_item - 1))) != 0) {
			Span<char> buf = stackalloc char[32];
			sprintf(buf, "menuselect %d\n").D(menu_item);
			engine.ClientCmd_Unrestricted(buf); // TODO: This should not be unrestricted

			SelectedItem = menu_item;
			clientMode.GetViewportAnimationController()?.StartAnimationSequence("MenuPulse");

			MenuTakesInput = false;
			ShutoffTime = gpGlobals.RealTime + OpenCloseTime;
			clientMode.GetViewportAnimationController()?.StartAnimationSequence("MenuClose");
		}
	}

	void ProcessText() {
		Processed.Clear();
		MaxPixels = 0;
		Height = 0;

		int i = 0;
		int startpos = i;
		int menuitem = 0;
		while (i < MAX_MENU_STRING) {
			char ch = MenuString[i];
			if (ch == 0)
				break;

			if (i == startpos && ch == '-' && MenuString[i + 1] == '>') {
				menuitem = MenuString[i + 2] - '0';
				i += 2;
				startpos += 2;

				continue;
			}

			while (i < MAX_MENU_STRING && MenuString[i] != 0 && MenuString[i] != '\n')
				i++;

			if ((i - startpos) >= 1) {
				ProcessedLine line;
				line.MenuItem = menuitem;
				line.StartChar = startpos;
				line.Length = i - startpos;
				line.Pixels = 0;
				line.Height = 0;

				Processed.Add(line);
			}

			menuitem = 0;

			if (MenuString[i] == '\n') {
				i++;
			}
			startpos = i;
		}

		if (i - startpos >= 1) {
			ProcessedLine line;
			line.MenuItem = menuitem;
			line.StartChar = startpos;
			line.Length = i - startpos;
			line.Pixels = 0;
			line.Height = 0;
			Processed.Add(line);
		}

		int c = Processed.Count;
		for (i = 0; i < c; i++) {
			ProcessedLine l = Processed[i];
			Assert(l);

			int pixels = 0;
			IFont font = l.MenuItem != 0 ? ItemFont : TextFont;

			for (int ch = 0; ch < l.Length; ch++)
				pixels += surface.GetCharacterWidth(font, MenuString[ch + l.StartChar]);

			l.Pixels = pixels;
			l.Height = surface.GetFontTall(font);
			if (pixels > MaxPixels)
				MaxPixels = pixels;

			Height += l.Height;
		}
	}

	void HideMenu() {
		MenuTakesInput = false;
		ShutoffTime = gpGlobals.RealTime + OpenCloseTime;
		clientMode.GetViewportAnimationController()?.StartAnimationSequence("MenuClose");
	}

	void ShowMenu(ReadOnlySpan<char> menuName, int validSlots) {
		ShutoffTime = -1;
		BitsValidSlots = validSlots;
		WaitingForMore = 0;
		strcpy(PrelocalisedMenuString, menuName);

		clientMode.GetViewportAnimationController()?.StartAnimationSequence("MenuOpen");
		SelectedItem = -1;

		Span<char> menuString = stackalloc char[MAX_MENU_STRING];
		strcpy(menuString, ConvertCRtoNL(PrelocalisedMenuString));//hudtextmessage.BufferedLocaliseTextString

		ProcessText();

		MenuDisplayed = true;
		MenuTakesInput = true;
		SelectionTime = gpGlobals.CurTime;
	}

	void ShowMenu_KeyValueItems(KeyValues kv) {
		ShutoffTime = -1;
		WaitingForMore = 0;
		BitsValidSlots = 0;

		clientMode.GetViewportAnimationController()?.StartAnimationSequence("MenuOpen");
		SelectedItem = -1;

		MenuString[0] = '\0';
		Span<char> writePos = MenuString;
		int remaining = MenuString.Length;
		int count;

		int i = 0;
		for (KeyValues? item = kv.GetFirstSubKey(); item != null; item = item.GetNextKey()) {
			BitsValidSlots |= 1 << i;

			ReadOnlySpan<char> pszItem = item.GetString();
			ReadOnlySpan<char> localized = localize.Find(pszItem);

			count = sprintf(writePos, "%d. %s\n").D(i + 1).S(localized);
			remaining -= count;
			writePos = writePos[count..];

			i++;
		}

		BitsValidSlots |= 1 << 9;

		// count = sprintf(writePos, "0. %s\n").S(localize.Find("#Cancel"));
		// remaining -= count;
		// writePos = writePos[count..];

		ProcessText();

		MenuDisplayed = true;
		MenuTakesInput = true;

		SelectionTime = gpGlobals.CurTime;
	}

	void MsgFunc_ShowMenu(bf_read msg) {
		BitsValidSlots = (short)msg.ReadWord();
		int DisplayTime = msg.ReadChar();
		int NeedMore = msg.ReadByte();

		if (DisplayTime > 0)
			ShutoffTime = OpenCloseTime + DisplayTime + gpGlobals.RealTime;
		else
			ShutoffTime = -1;

		if (BitsValidSlots != 0) {
			Span<char> szString = stackalloc char[2048];
			msg.ReadString(szString);

			if (WaitingForMore == 0)
				strcpy(PrelocalisedMenuString, szString);
			else {
				int dstLen = PrelocalisedMenuString.IndexOf('\0');
				int srcLen = szString.IndexOf('\0');
				int copyLen = Math.Min(srcLen, PrelocalisedMenuString.Length - dstLen - 1);
				szString[..copyLen].CopyTo(PrelocalisedMenuString.AsSpan(dstLen));
				PrelocalisedMenuString[dstLen + copyLen] = '\0';
			}

			if (NeedMore == 0) {
				clientMode.GetViewportAnimationController()?.StartAnimationSequence("MenuOpen");
				SelectedItem = -1;

				Span<char> menuString = stackalloc char[MAX_MENU_STRING];
				strcpy(menuString, ConvertCRtoNL(PrelocalisedMenuString));//hudtextmessage.BufferedLocaliseTextString
				strcpy(MenuString, menuString);

				ProcessText();
			}

			MenuDisplayed = true;
			MenuTakesInput = true;

			SelectionTime = gpGlobals.CurTime;
		}
		else
			HideMenu();

		WaitingForMore = NeedMore;
	}

	private static ReadOnlySpan<char> ConvertCRtoNL(ReadOnlySpan<char> inString) {
		Span<char> outString = new char[inString.Length + 1];
		int outIndex = 0;

		for (int i = 0; i < inString.Length; i++) {
			if (inString[i] == '\r')
				outString[outIndex++] = '\n';
			else
				outString[outIndex++] = inString[i];
		}

		outString[outIndex] = '\0';
		return outString[..outIndex];
	}

	public override void ApplySchemeSettings(IScheme scheme) {
		base.ApplySchemeSettings(scheme);

		SetPaintBackgroundEnabled(false);

		GetPos(out int x, out int y);
		GetHudSize(out int screenWide, out int screenTall);
		SetBounds(0, y, screenWide, screenTall - y);

		ProcessText();
	}
}
