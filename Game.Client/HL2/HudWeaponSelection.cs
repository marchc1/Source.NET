using Game.Client.HUD;

using Source;
using Source.Common.Commands;
using Source.Common.GUI;
using Source.GUI.Controls;

namespace Game.Client.HL2;

[DeclareHudElement(Name = "CHudWeaponSelection")]
class HudWeaponSelection : BaseHudWeaponSelection, IHudElement
{
	public static ConVar hud_showemptyweaponslots = new ConVar("hud_showemptyweaponslots", "1", FCvar.Archive, "Shows slots for missing weapons when recieving weapons out of order");

	const float SELECTION_TIMEOUT_THRESHOLD = 0.5f;  // Seconds
	const float SELECTION_FADEOUT_TIME = 0.75f;
	const float PLUS_DISPLAY_TIMEOUT = 0.5f; // Seconds
	const float PLUS_FADEOUT_TIME = 0.75f;
	const float FASTSWITCH_DISPLAY_TIMEOUT = 1.5f;
	const float FASTSWITCH_FADEOUT_TIME = 1.5f;
	const float CAROUSEL_SMALL_DISPLAY_ALPHA = 200.0f;
	const float FASTSWITCH_SMALL_DISPLAY_ALPHA = 160.0f;
	const float MAX_CAROUSEL_SLOTS = 5;

	[PanelAnimationVar("NumberFont", "HudSelectionNumbers")] protected IFont NumberFont;
	[PanelAnimationVar("TextFont", "HudSelectionText")] protected IFont TextFont;
	[PanelAnimationVar("Blur", "0")] protected float Blur;
	[PanelAnimationVarAliasType("SmallBoxSize", "32", "proportional_float")] protected float SmallBoxSize;
	[PanelAnimationVarAliasType("LargeBoxWide", "108", "proportional_float")] protected float LargeBoxWide;
	[PanelAnimationVarAliasType("LargeBoxTall", "72", "proportional_float")] protected float LargeBoxTall;
	[PanelAnimationVarAliasType("MediumBoxWide", "75", "proportional_float")] protected float MediumBoxWide;
	[PanelAnimationVarAliasType("MediumBoxTall", "50", "proportional_float")] protected float MediumBoxTall;
	[PanelAnimationVarAliasType("BoxGap", "12", "proportional_float")] protected float BoxGap;
	[PanelAnimationVarAliasType("SelectionNumberXPos", "4", "proportional_float")] protected float SelectionNumberXPos;
	[PanelAnimationVarAliasType("SelectionNumberYPos", "4", "proportional_float")] protected float SelectionNumberYPos;
	[PanelAnimationVarAliasType("TextYPos", "54", "proportional_float")] protected float TextYPos;
	[PanelAnimationVar("Alpha", "0")] protected float AlphaOverride;
	[PanelAnimationVar("SelectionAlpha", "0")] protected float SelectionAlphaOverride;
	[PanelAnimationVar("TextColor", "SelectionTextFg")] protected Color TextColor;
	[PanelAnimationVar("NumberColor", "SelectionNumberFg")] protected Color NumberColor;
	[PanelAnimationVar("EmptyBoxColor", "SelectionEmptyBoxBg")] protected Color EmptyBoxColor;
	[PanelAnimationVar("BoxColor", "SelectionBoxBg")] protected Color BoxColor;
	[PanelAnimationVar("SelectedBoxColor", "SelectionSelectedBoxBg")] protected Color SelectedBoxColor;
	[PanelAnimationVar("SelectedFgColor", "FgColor")] protected Color SelectedFgColor;
	[PanelAnimationVar("SelectedFgColor", "BgColor")] protected Color BrightBoxColor;
	[PanelAnimationVar("SelectionGrowTime", "0.1")] protected float WeaponPickupGrowTime;
	[PanelAnimationVar("TextScan", "1.0")] protected float TextScan;
	bool FadingOut;
	struct WeaponBox
	{
		public int Slot;
		public int SplotPos;
	}
	List<WeaponBox> WeaponBoxes = [];
	int SelectedWeaponBox;
	int SelectedSlideDir;
	int SelectedBoxPosition;
	int SelectedSlot;
	BaseCombatWeapon? LastWeapon;
	[PanelAnimationVar(nameof(WeaponBoxOffset), "WeaponBoxOffset", "0")] protected float WeaponBoxOffset;

	IHudElement HudElement => this;

	public HudWeaponSelection(string? elementName) : base("HudWeaponSelection") {
		Panel Parent = clientMode.GetViewport();
		SetParent(Parent);
		FadingOut = false;
	}

	bool IsWeaponSelectable() => IsInSelectionMode();
	void SetSelectedWeapon(BaseCombatWeapon? weapon) => SelectedWeapon = weapon;
	void SetSelectedSlot(int slot) => SelectedSlot = slot;
	void SetSelectedSlideDir(int dir) => SelectedSlideDir = dir;

	void OnWeaponPickup(BaseCombatWeapon weapon) {
		HudHistoryResource? hr = gHUD.FindElement("CHudHistoryResource") as HudHistoryResource;
		// hr?.AddToHistory(weapon);
	}

	public override void OnThink() {
		float selectionTimeout = SELECTION_TIMEOUT_THRESHOLD;
		float selectionFadeoutTime = SELECTION_FADEOUT_TIME;

		if (hud_fastswitch.GetBool()) {
			selectionTimeout = FASTSWITCH_DISPLAY_TIMEOUT;
			selectionFadeoutTime = FASTSWITCH_FADEOUT_TIME;
		}

		if (gpGlobals.CurTime - SelectionTime > selectionTimeout) {
			if (!FadingOut) {
				clientMode.GetViewportAnimationController()?.StartAnimationSequence("FadeOutWeaponSelectionMenu");
				FadingOut = true;
			}
			else if (gpGlobals.CurTime - SelectionTime > selectionTimeout + selectionFadeoutTime)
				HideSelection();
		}
		else if (FadingOut) {
			clientMode.GetViewportAnimationController()?.StartAnimationSequence("OpenWeaponSelectionMenu");
			FadingOut = false;
		}
	}

	bool ShouldDraw() {
		BasePlayer? player = BasePlayer.GetLocalPlayer();
		if (player == null) {
			if (IsInSelectionMode())
				HideSelection();
			return false;
		}

		bool bret = HudElement.ShouldDraw();
		if (!bret)
			return false;

		if (hud_fastswitch.GetBool() && gpGlobals.CurTime - SelectionTime < (FASTSWITCH_DISPLAY_TIMEOUT + FASTSWITCH_FADEOUT_TIME))
			return true;

		return SelectionVisible;
	}

	void LevelInit() {
		HudElement.LevelInit();
		SelectedWeaponBox = -1;
		SelectedSlideDir = 0;
		LastWeapon = null;
	}

	void ActivateFastswitchWeaponDisplay(BaseCombatWeapon selectedWeapon) { }

	void ActivateWeaponHighlight(BaseCombatWeapon selectedWeapon) { }

	float GetWeaponBoxAlpha(bool selected) {
		if (selected)
			return SelectionAlphaOverride;
		return SelectionAlphaOverride * (AlphaOverride / 255.0f);
	}

	public override void Paint() { }

	void DrawLargeWeaponBox(BaseCombatWeapon pWeapon, bool bSelected, int xpos, int ypos, int boxWide, int boxTall, Color selectedColor, float alpha, int number) { }

	void DrawBox(int x, int y, int wide, int tall, Color color, float normalizedAlpha, int number) { }

	public override void ApplySchemeSettings(IScheme scheme) { }

	void OpenSelection() { }

	void HideSelection() { }

	// BaseCombatWeapon FindNextWeaponInWeaponSelection(int currentSlot, int currentPosition) { }

	// BaseCombatWeapon FindPrevWeaponInWeaponSelection(int currentSlot, int currentPosition) { }

	void CycleToNextWeapon() { }

	void CycleToPrevWeapon() { }

	// int GetLastPosInSlot(int slot) { }

	// BaseCombatWeapon GetWeaponInSlot(int slot, int slotPos) { }

	void FastWeaponSwitch(int weaponSlot) { }

	void PlusTypeFastWeaponSwitch(int weaponSlot) { }

	void SelectWeaponSlot(int slot) { }
}