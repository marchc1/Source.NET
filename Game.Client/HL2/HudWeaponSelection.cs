using Game.Client.HUD;

using Source;
using Source.Common.GUI;
using Source.GUI.Controls;

[DeclareHudElement(Name = "CHudWeaponSelection")]
class HudWeaponSelection : BaseHudWeaponSelection, IHudElement
{
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

	public HudWeaponSelection(string? elementName) : base("HudWeaponSelection") {

	}

	void OnWeaponPickup(BaseCombatWeapon weapon) { }

	public override void OnThink() { }

	// bool ShouldDraw() { }

	void LevelInit() { }

	void ActivateFastswitchWeaponDisplay(BaseCombatWeapon selectedWeapon) { }

	void ActivateWeaponHighlight(BaseCombatWeapon selectedWeapon) { }

	// float GetWeaponBoxAlpha(bool selected) { }

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