using Game.Client.HUD;
using Game.Shared;

using Source;
using Source.Common.Commands;
using Source.Common.GUI;
using Source.Common.MaterialSystem;
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
	[PanelAnimationVar("WeaponBoxOffset", "0", "float")] protected float HorizWeaponSelectOffsetPoint;
	bool FadingOut;
	struct WeaponBox
	{
		public int Slot;
		public int SlotPos;
	}
	List<WeaponBox> WeaponBoxes = [];
	int SelectedWeaponBox;
	int SelectedSlideDir;
	int SelectedBoxPosition;
	int SelectedSlot;
	BaseCombatWeapon? LastWeapon;
	[PanelAnimationVar(nameof(WeaponBoxOffset), "WeaponBoxOffset", "0")] protected float WeaponBoxOffset;

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

	void ActivateWeaponHighlight(BaseCombatWeapon selectedWeapon) {
		BasePlayer? player = BasePlayer.GetLocalPlayer();
		if (player == null)
			return;

		MakeReadyForUse();

		BaseCombatWeapon? weapon = GetWeaponInSlot(SelectedSlot, SelectedBoxPosition);
		if (weapon == null)
			return;

		clientMode.GetViewportAnimationController()?.StartAnimationSequence("WeaponHighlight");
	}

	float GetWeaponBoxAlpha(bool selected) {
		if (selected)
			return SelectionAlphaOverride;
		return SelectionAlphaOverride * (AlphaOverride / 255.0f);
	}

	public override void Paint() {
		int width;
		int xpos;
		int ypos;

		if (!ShouldDraw())
			return;

		BasePlayer? localPlayer = BasePlayer.GetLocalPlayer();
		if (localPlayer == null)
			return;

		BaseCombatWeapon? selectedWeapon;
		selectedWeapon = hud_fastswitch.GetInt() switch {
			HUDTYPE_FASTSWITCH or HUDTYPE_CAROUSEL => localPlayer.GetActiveWeapon(),
			_ => GetSelectedWeapon(),
		};

		if (selectedWeapon == null)
			return;

		bool bPushedViewport = false;
		if (hud_fastswitch.GetInt() == HUDTYPE_FASTSWITCH || hud_fastswitch.GetInt() == HUDTYPE_PLUS) {
			using MatRenderContextPtr renderContext = new(materials);
			if (renderContext.GetRenderTarget() != null) {
				// surface.PushFullscreenViewport();
				bPushedViewport = true;
			}
		}

		float percentageDone = 1.0f;
		int largeBoxWide = (int)(SmallBoxSize + ((LargeBoxWide - SmallBoxSize) * percentageDone));
		int largeBoxTall = (int)(SmallBoxSize + ((LargeBoxTall - SmallBoxSize) * percentageDone));
		Color selectedColor = new();
		for (int i = 0; i < 4; i++)
			selectedColor[i] = (byte)(BoxColor[i] + ((SelectedBoxColor[i] - BoxColor[i]) * percentageDone));

		switch (hud_fastswitch.GetInt()) {
			case HUDTYPE_CAROUSEL: {
					ypos = 0;
					if (SelectedWeaponBox == -1 || WeaponBoxes.Count <= 1)
						return;
					else if (WeaponBoxes.Count < MAX_CAROUSEL_SLOTS) {
						width = (int)((WeaponBoxes.Count - 1) * (LargeBoxWide + BoxGap) + LargeBoxWide);
						xpos = (GetWide() - width) / 2;
						for (int i = 0; i < WeaponBoxes.Count; i++) {
							BaseCombatWeapon? weapon = GetWeaponInSlot(WeaponBoxes[i].Slot, WeaponBoxes[i].SlotPos);
							if (weapon == null)
								break;

							byte alpha = (byte)GetWeaponBoxAlpha(i == SelectedWeaponBox);
							if (i == SelectedWeaponBox)
								DrawLargeWeaponBox(weapon, true, xpos, ypos, (int)LargeBoxWide, (int)LargeBoxTall, selectedColor, alpha, -1);
							else
								DrawLargeWeaponBox(weapon, false, xpos, ypos, (int)LargeBoxWide, (int)(LargeBoxTall / 1.5f), BoxColor, alpha, -1);

							xpos += (int)(LargeBoxWide + BoxGap);
						}
					}
					else {
						xpos = GetWide() / 2 + (int)HorizWeaponSelectOffsetPoint - largeBoxWide / 2;
						int i = SelectedWeaponBox;
						while (true) {
							BaseCombatWeapon? weapon = GetWeaponInSlot(WeaponBoxes[i].Slot, WeaponBoxes[i].SlotPos);
							if (weapon == null)
								break;

							byte alpha;
							if (i == SelectedWeaponBox && HorizWeaponSelectOffsetPoint == 0) {
								alpha = (byte)GetWeaponBoxAlpha(true);
								DrawLargeWeaponBox(weapon, true, xpos, ypos, largeBoxWide, largeBoxTall, selectedColor, alpha, -1);
							}
							else {
								alpha = (byte)GetWeaponBoxAlpha(false);
								DrawLargeWeaponBox(weapon, false, xpos, ypos, largeBoxWide, (int)(largeBoxTall / 1.5f), BoxColor, alpha, -1);
							}

							xpos += (int)(largeBoxWide + BoxGap);
							if (xpos >= GetWide())
								break;

							++i;
							if (i >= WeaponBoxes.Count)
								i = 0;
						}

						xpos = (int)(GetWide() / 2 + HorizWeaponSelectOffsetPoint - (3 * largeBoxWide / 2 + BoxGap));
						i = SelectedWeaponBox - 1;
						while (true) {
							if (i < 0)
								i = WeaponBoxes.Count - 1;

							BaseCombatWeapon? weapon = GetWeaponInSlot(WeaponBoxes[i].Slot, WeaponBoxes[i].SlotPos);
							if (weapon == null)
								break;

							byte alpha;
							if (i == SelectedWeaponBox && HorizWeaponSelectOffsetPoint == 0) {
								alpha = (byte)GetWeaponBoxAlpha(true);
								DrawLargeWeaponBox(weapon, true, xpos, ypos, largeBoxWide, largeBoxTall, selectedColor, alpha, -1);
							}
							else {
								alpha = (byte)GetWeaponBoxAlpha(false);
								DrawLargeWeaponBox(weapon, false, xpos, ypos, largeBoxWide, (int)(largeBoxTall / 1.5f), BoxColor, alpha, -1);
							}

							xpos -= (int)(largeBoxWide + BoxGap);
							if (xpos + largeBoxWide <= 0)
								break;

							--i;
						}
					}
				}
				break;
			case HUDTYPE_PLUS: {
					float fCenterX = 0, fCenterY = 0;
					bool bBehindCamera = false;
					// CHudCrosshair::GetDrawPosition(&fCenterX, &fCenterY, &bBehindCamera); TODO

					if (bBehindCamera)
						return;

					int screenCenterX = (int)fCenterX;
					int screenCenterY = (int)fCenterY - 15;

					int[] xModifiers = [0, 1, 0, -1, -1, 1];
					int[] yModifiers = [-1, 0, 1, 0, 1, 1];

					for (int i = 0; i < MAX_WEAPON_SLOTS; ++i) {
						int xPos = screenCenterX - (int)(MediumBoxWide / 2);
						int yPos = screenCenterY - (int)(MediumBoxTall / 2);

						int lastSlotPos = -1;
						for (int slotPos = 0; slotPos < MAX_WEAPON_POSITIONS; ++slotPos) {
							BaseCombatWeapon? weapon = GetWeaponInSlot(i, slotPos);
							if (weapon != null)
								lastSlotPos = slotPos;
						}

						for (int slotPos = 0; slotPos <= lastSlotPos; ++slotPos) {
							xPos += (int)(MediumBoxWide + 5) * xModifiers[i];
							yPos += (int)(MediumBoxTall + 5) * yModifiers[i];

							int boxWide = (int)MediumBoxWide;
							int boxTall = (int)MediumBoxTall;
							int x = xPos;
							int y = yPos;

							BaseCombatWeapon? weapon = GetWeaponInSlot(i, slotPos);
							bool bSelectedWeapon = false;
							if (i == SelectedSlot && slotPos == SelectedBoxPosition)
								bSelectedWeapon = true;

							DrawLargeWeaponBox(weapon, bSelectedWeapon, x, y, boxWide, boxTall, bSelectedWeapon ? selectedColor : BoxColor, (byte)GetWeaponBoxAlpha(bSelectedWeapon), -1);
						}
					}
				}
				break;
			case HUDTYPE_BUCKETS: {
					width = (MAX_WEAPON_SLOTS - 1) * (int)(SmallBoxSize + BoxGap) + largeBoxWide;
					xpos = (GetWide() - width) / 2;
					ypos = 0;

					int activeSlot = SelectedWeapon != null ? SelectedWeapon.GetSlot() : -1;

					for (int i = 0; i < MAX_WEAPON_SLOTS; i++) {
						if (i == activeSlot) {
							bool drawBucketNumber = true;
							int lastPos = GetLastPosInSlot(i);

							for (int slotpos = 0; slotpos <= lastPos; slotpos++) {
								BaseCombatWeapon? weapon = GetWeaponInSlot(i, slotpos);
								if (weapon == null) {
									if (!hud_showemptyweaponslots.GetBool())
										continue;
									DrawBox(xpos, ypos, largeBoxWide, largeBoxTall, EmptyBoxColor, AlphaOverride, drawBucketNumber ? i + 1 : -1);
								}
								else {
									bool bSelected = weapon == SelectedWeapon;
									DrawLargeWeaponBox(weapon, bSelected, xpos, ypos, largeBoxWide, largeBoxTall, bSelected ? selectedColor : BoxColor, (byte)GetWeaponBoxAlpha(bSelected), drawBucketNumber ? i + 1 : -1);
								}

								ypos += (int)(largeBoxTall + BoxGap);
								drawBucketNumber = false;
							}

							xpos += largeBoxWide;
						}
						else {
							if (GetFirstPos(i) != null)
								DrawBox(xpos, ypos, (int)SmallBoxSize, (int)SmallBoxSize, BoxColor, AlphaOverride, i + 1);
							else
								DrawBox(xpos, ypos, (int)SmallBoxSize, (int)SmallBoxSize, EmptyBoxColor, AlphaOverride, -1);

							xpos += (int)SmallBoxSize;
						}

						ypos = 0;
						xpos += (int)BoxGap;
					}
				}
				break;
			default:
				break;
		}

		if (bPushedViewport) {
			// surface.PopFullscreenViewport();
		}
	}

	void DrawLargeWeaponBox(BaseCombatWeapon? weapon, bool bSelected, int xpos, int ypos, int boxWide, int boxTall, Color selectedColor, byte alpha, int number) {
		Color col = bSelected ? SelectedFgColor : GetFgColor();

		switch (hud_fastswitch.GetInt()) {
			case HUDTYPE_BUCKETS: {
					DrawBox(xpos, ypos, boxWide, boxTall, selectedColor, alpha, number);

					col[3] *= (byte)(alpha / 255.0f);
					// if (weapon.GetSpriteActive()) { // todo
					// 	int iconWidth = weapon.GetSpriteActive().Width();
					// 	int iconHeight = weapon.GetSpriteActive().Height();
					// 	int x_offs = (boxWide - iconWidth) / 2;
					// 	int y_offs;

					// 	if (bSelected && hud_fastswitch.GetInt() != 0)
					// 		y_offs = (int)(boxTall / 1.5f - iconHeight) / 2;
					// 	else
					// 		y_offs = (boxTall - iconHeight) / 2;

					// 	if (!weapon.CanBeSelected()) // todo
					// 		col = new(255, 0, 0, col[3]);
					// 	else if (bSelected) {
					// 		col[3] = alpha;
					// 		weapon.GetSpriteActive().DrawSelf(xpos + x_offs, ypos + y_offs, col);
					// 	}

					// 	weapon.GetSpriteInactive().DrawSelf(xpos + x_offs, ypos + y_offs, col);
					// }
				}
				break;
			case HUDTYPE_PLUS:
			case HUDTYPE_CAROUSEL: {
					if (weapon == null) {
						if (bSelected)
							selectedColor.SetColor(255, 0, 0, 40);

						DrawBox(xpos, ypos, boxWide, boxTall, selectedColor, alpha, number);
						return;
					}
					else
						DrawBox(xpos, ypos, boxWide, boxTall, selectedColor, alpha, number);

					// int iconWidth;
					// int iconHeight;
					// int x_offs;
					// int y_offs;

					col[3] *= (byte)(alpha / 255.0f);

					// if (weapon.GetSpriteInactive()) { // todo
					// 	iconWidth = weapon.GetSpriteInactive().Width();
					// 	iconHeight = weapon.GetSpriteInactive().Height();

					// 	x_offs = (boxWide - iconWidth) / 2;
					// 	if (bSelected && HUDTYPE_CAROUSEL == hud_fastswitch.GetInt())
					// 		y_offs = (int)(boxTall / 1.5f - iconHeight) / 2;
					// 	else
					// 		y_offs = (boxTall - iconHeight) / 2;

					// 	if (!weapon.CanBeSelected())// todo
					// 		col = new(255, 0, 0, col[3]);

					// 	weapon.GetSpriteInactive().DrawSelf(xpos + x_offs, ypos + y_offs, iconWidth, iconHeight, col);
					// }

					// if (bSelected && weapon.GetSpriteActive()) {// todo
					// 	iconWidth = weapon.GetSpriteActive().Width();
					// 	iconHeight = weapon.GetSpriteActive().Height();

					// 	x_offs = (boxWide - iconWidth) / 2;
					// 	if (HUDTYPE_CAROUSEL == hud_fastswitch.GetInt())
					// 		y_offs = (int)(boxTall / 1.5f - iconHeight) / 2;
					// 	else
					// 		y_offs = (boxTall - iconHeight) / 2;

					// 	col[3] = 255;
					// 	for (float fl = Blur; fl > 0.0f; fl -= 1.0f) {
					// 		if (fl >= 1.0f)
					// 			weapon.GetSpriteActive().DrawSelf(xpos + x_offs, ypos + y_offs, col);
					// 		else {
					// 			col[3] *= (byte)fl;
					// 			weapon.GetSpriteActive().DrawSelf(xpos + x_offs, ypos + y_offs, col);
					// 		}
					// 	}
					// }
				}
				break;
			default:
				break;
		}

		if (HUDTYPE_PLUS == hud_fastswitch.GetInt())
			return;

		col = TextColor;
		FileWeaponInfo weaponInfo = weapon!.GetWpnData();

		if (bSelected) {
			Span<char> text = stackalloc char[128];
			ReadOnlySpan<char> tempString = localize.Find(weaponInfo.PrintName);

			if (!tempString.IsEmpty)
				tempString.ClampedCopyTo(text);
			else
				strcpy(text, weaponInfo.PrintName);

			surface.DrawSetTextColor(col);
			surface.DrawSetTextFont(TextFont);

			int slen = 0, charCount = 0, maxslen = 0;
			int firstslen = 0;
			for (char pch = text[0]; pch != '\0'; pch = text[++charCount]) {
				if (pch == '\n') {
					if (slen > maxslen)
						maxslen = slen;

					if (firstslen == 0)
						firstslen = slen;

					slen = 0;
				}
				else if (pch != '\r') {
					slen += surface.GetCharacterWidth(TextFont, pch);
					charCount++;
				}
			}

			if (slen > maxslen)
				maxslen = slen;

			if (firstslen == 0)
				firstslen = maxslen;

			int tx = xpos + (int)((LargeBoxWide - firstslen) / 2);
			int ty = ypos + (int)TextYPos;
			surface.DrawSetTextPos(tx, ty);
			charCount *= (int)TextScan;
			for (char pch = text[0]; charCount > 0; pch = text[++charCount]) {
				if (pch == '\n')
					surface.DrawSetTextPos(xpos + ((boxWide - slen) / 2), ty + (int)(surface.GetFontTall(TextFont) * 1.1f));
				else if (pch != '\r') {
					surface.DrawChar(pch);
					charCount--;
				}
			}
		}
	}

	void DrawBox(int x, int y, int wide, int tall, Color color, float normalizedAlpha, int number) {
		base.DrawBox(x, y, wide, tall, color, normalizedAlpha / 255.0f);

		if (number > 0) {
			Color numberColor = NumberColor;
			numberColor.A *= (byte)(normalizedAlpha / 255.0f);
			Surface.DrawSetTextColor(numberColor);
			Surface.DrawSetTextFont(NumberFont);
			Span<char> unicode = stackalloc char[2];
			sprintf(unicode, "%d").D(number);
			Surface.DrawSetTextPos(x + (int)SelectionNumberXPos, y + (int)SelectionNumberYPos);
			Surface.DrawString(unicode);
		}
	}

	public override void ApplySchemeSettings(IScheme scheme) { }

	public override void OpenSelection() {
		Assert(!IsInSelectionMode());

		base.OpenSelection();
		clientMode.GetViewportAnimationController()?.StartAnimationSequence("OpenWeaponSelectionMenu");
		SelectedBoxPosition = 0;
		SelectedSlot = -1;
	}

	public override void HideSelection() {
		base.HideSelection();
		clientMode.GetViewportAnimationController()?.StartAnimationSequence("CloseWeaponSelectionMenu");
		FadingOut = false;
	}

	BaseCombatWeapon? FindNextWeaponInWeaponSelection(int currentSlot, int currentPosition) {
		BasePlayer? player = BasePlayer.GetLocalPlayer();
		if (player == null)
			return null;

		BaseCombatWeapon? nextWeapon = null;
		int lowestNextSlot = MAX_WEAPON_SLOTS;
		int lowestNextPosition = MAX_WEAPON_POSITIONS;

		for (int i = 0; i < MAX_WEAPONS; i++) {
			BaseCombatWeapon? weapon = player.GetWeapon(i);
			if (weapon == null)
				continue;

			if (CanBeSelectedInHUD(weapon)) {
				int weaponSlot = weapon.GetSlot();
				int weaponPosition = weapon.GetPosition();

				if (weaponSlot > currentSlot || (weaponSlot == currentSlot && weaponPosition > currentPosition)) {
					if (weaponSlot < lowestNextSlot || (weaponSlot == lowestNextSlot && weaponPosition < lowestNextPosition)) {
						lowestNextSlot = weaponSlot;
						lowestNextPosition = weaponPosition;
						nextWeapon = weapon;
					}
				}
			}
		}

		return nextWeapon;
	}

	BaseCombatWeapon? FindPrevWeaponInWeaponSelection(int currentSlot, int currentPosition) {
		BasePlayer? player = BasePlayer.GetLocalPlayer();
		if (player == null)
			return null;

		BaseCombatWeapon? prevWeapon = null;
		int highestPrevSlot = -1;
		int highestPrevPosition = -1;

		for (int i = 0; i < MAX_WEAPONS; i++) {
			BaseCombatWeapon? weapon = player.GetWeapon(i);
			if (weapon == null)
				continue;

			if (CanBeSelectedInHUD(weapon)) {
				int weaponSlot = weapon.GetSlot();
				int weaponPosition = weapon.GetPosition();

				if (weaponSlot < currentSlot || (weaponSlot == currentSlot && weaponPosition < currentPosition)) {
					if (weaponSlot > highestPrevSlot || (weaponSlot == highestPrevSlot && weaponPosition > highestPrevPosition)) {
						highestPrevSlot = weaponSlot;
						highestPrevPosition = weaponPosition;
						prevWeapon = weapon;
					}
				}
			}
		}

		return prevWeapon;
	}


	public override void CycleToNextWeapon() {
		BasePlayer? player = BasePlayer.GetLocalPlayer();
		if (player == null)
			return;

		LastWeapon = player.GetActiveWeapon();

		BaseCombatWeapon? nextWeapon;
		if (IsInSelectionMode()) {
			BaseCombatWeapon? weapon = GetSelectedWeapon();
			if (weapon == null)
				return;

			nextWeapon = FindNextWeaponInWeaponSelection(weapon.GetSlot(), weapon.GetPosition());
		}
		else {
			nextWeapon = player.GetActiveWeapon();
			if (nextWeapon != null)
				nextWeapon = FindNextWeaponInWeaponSelection(nextWeapon.GetSlot(), nextWeapon.GetPosition());
		}

		nextWeapon ??= FindNextWeaponInWeaponSelection(-1, -1);

		if (nextWeapon != null) {
			SetSelectedWeapon(nextWeapon);
			SetSelectedSlideDir(1);

			if (hud_fastswitch.GetInt() > 0)
				SelectWeapon();
			else if (!IsInSelectionMode())
				OpenSelection();

			// player.EmitSound("Player.WeaponSelectionMoveSlot");
		}
	}

	public override void CycleToPrevWeapon() {
		BasePlayer? player = BasePlayer.GetLocalPlayer();
		if (player == null)
			return;

		LastWeapon = player.GetActiveWeapon();

		BaseCombatWeapon? prevWeapon;
		if (IsInSelectionMode()) {
			BaseCombatWeapon? weapon = GetSelectedWeapon();
			if (weapon == null)
				return;

			prevWeapon = FindPrevWeaponInWeaponSelection(weapon.GetSlot(), weapon.GetPosition());
		}
		else {
			prevWeapon = player.GetActiveWeapon();
			if (prevWeapon != null)
				prevWeapon = FindPrevWeaponInWeaponSelection(prevWeapon.GetSlot(), prevWeapon.GetPosition());
		}

		prevWeapon ??= FindPrevWeaponInWeaponSelection(MAX_WEAPON_SLOTS, MAX_WEAPON_POSITIONS);

		if (prevWeapon != null) {
			SetSelectedWeapon(prevWeapon);
			SetSelectedSlideDir(-1);

			if (hud_fastswitch.GetInt() > 0)
				SelectWeapon();
			else if (!IsInSelectionMode())
				OpenSelection();

			// player.EmitSound("Player.WeaponSelectionMoveSlot");
		}
	}

	int GetLastPosInSlot(int slot) {
		BasePlayer? player = BasePlayer.GetLocalPlayer();
		if (player == null)
			return -1;

		int maxSlotPos = -1;
		for (int i = 0; i < MAX_WEAPONS; i++) {
			if (player.GetWeapon(i) is not BaseCombatWeapon weapon)
				continue;

			if (weapon.GetSlot() == slot && weapon.GetPosition() > maxSlotPos)
				maxSlotPos = weapon.GetPosition();
		}

		return maxSlotPos;
	}

	BaseCombatWeapon? GetWeaponInSlot(int slot, int slotPos) { return null; }//todo

	void FastWeaponSwitch(int weaponSlot) { }

	void PlusTypeFastWeaponSwitch(int weaponSlot) { }

	public override void SelectWeaponSlot(int slot) {
		--slot;

		BasePlayer? player = BasePlayer.GetLocalPlayer();
		if (player == null)
			return;

		if (slot >= MAX_WEAPON_SLOTS)
			return;

		// if (!player.IsAllowToSwitchWeapons()) todo
		// 	return;

		switch (hud_fastswitch.GetInt()) {
			case HUDTYPE_FASTSWITCH:
			case HUDTYPE_CAROUSEL: {
					FastWeaponSwitch(slot);
					return;
				}
			case HUDTYPE_PLUS: {
					if (!IsInSelectionMode())
						OpenSelection();

					PlusTypeFastWeaponSwitch(slot);
					ActivateWeaponHighlight(GetSelectedWeapon()!);
				}
				break;
			case HUDTYPE_BUCKETS: {
					int slotPos = 0;
					BaseCombatWeapon? activeWeapon = GetSelectedWeapon();

					if (IsInSelectionMode() && activeWeapon != null && activeWeapon.GetSlot() == slot)
						slotPos = activeWeapon.GetPosition() + 1;

					activeWeapon = GetNextActivePos(slot, slotPos);
					activeWeapon ??= GetNextActivePos(slot, 0);

					if (activeWeapon != null) {
						if (!IsInSelectionMode())
							OpenSelection();

						SetSelectedWeapon(activeWeapon);
						SetSelectedSlideDir(0);
					}
				}
				break;
			default:
				break;
		}

		// player.EmitSound("Player.WeaponSelectionMoveSlot");
	}
}