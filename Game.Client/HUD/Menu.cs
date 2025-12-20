using Source;
using Source.Common.Bitbuffers;
using Source.Common.Formats.Keyvalues;
using Source.Common.GUI;
using Source.GUI.Controls;

namespace Game.Client.HUD;

[DeclareHudElement(Name = "CHudMenu")]
class HudMenu : EditableHudElement
{
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
	float ShutoffTime;
	int WaitingForMore;
	int SelectedItem;
	bool MenuTakesInput;
	float SelectionTime;

	[PanelAnimationVar("OpenCloseTime", "1", "float")] protected float OpenCloseTime;
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

	public HudMenu(string elementName) : base(null, elementName) {

	}

	public override void Init() { }

	void Reset() { }

	// bool IsMenuOpen() { }

	void VidInit() { }

	public override void OnThink() { }

	// bool ShouldDraw() { }

	void PaintString(ReadOnlySpan<char> text, IFont font, int x, int y) { }

	public override void Paint() { }

	void SelectMenuItem(int menu_item) { }

	void ProcessText() { }

	void HideMenu() { }

	void ShowMenu(ReadOnlySpan<char> menuName, int validSlots) { }

	void ShowMenu_KeyValueItems(KeyValues kv) { }

	void MsgFunc_ShowMenu(bf_read msg) { }

	public override void ApplySchemeSettings(IScheme scheme) { }
}