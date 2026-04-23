
using Source;
using Source.Common;
using Source.Common.Commands;
using Source.Common.Filesystem;
using Source.Common.Formats.Keyvalues;
using Source.Common.GUI;
using Source.Common.Input;
using Source.Engine;
using Source.GUI.Controls;

namespace Game.UI;

struct ChallengeDescription
{
	public string Name;
	public string Description;
	public int Type;
	public int Bronze;
	public int Silver;
	public int Gold;
	public int Best;
}

struct BonusMapDescription
{
	public bool IsFolder;
	public string ShortName;
	public string FileName;
	public string MapFileName;
	public string ChapterName;
	public string ImageName;
	public string MapName;
	public string Comment;
	public bool Locked;
	public bool Complete;
}

struct Chapter
{
	public string Filename;
}

class NewGamePlayButton : Button
{
	public NewGamePlayButton(Panel parent, ReadOnlySpan<char> name, ReadOnlySpan<char> text, Panel? actionSignalTarget = null, string? cmd = null) : base(parent, name, text, actionSignalTarget, cmd) { }
}

class SelectionOverlayPanel : Panel
{
	int ChapterIndex;
	NewGameDialog SelectionTarget;

	public SelectionOverlayPanel(Panel parent, NewGameDialog selectionTarget, int chapterIndex) : base(parent, null) {
		ChapterIndex = chapterIndex;
		SelectionTarget = selectionTarget;
		SetPaintEnabled(false);
		SetPaintBackgroundEnabled(false);
	}

	public override void OnMousePressed(ButtonCode code) {
		if (GetParent()!.IsEnabled())
			SelectionTarget.SetSelectedChapterIndex(ChapterIndex);
	}

	public override void OnMouseDoublePressed(ButtonCode code) {
		OnMousePressed(code);
		if (GetParent()!.IsEnabled())
			PostMessage(SelectionTarget, new("Command", "command", "play"));
	}
}

class GameChapterPanel : EditablePanel
{
	ImagePanel LevelPicBorder;
	ImagePanel LevelPic;
	ImagePanel? CommentaryIcon;
	Label ChapterLabel;
	Label ChapterNameLabel;

	Color TextColor;
	Color DisabledColor;
	Color SelectedColor;
	Color FillColor;

	InlineArrayMaxPath<char> ConfigFile;
	InlineArray32<char> Chapter;

	bool TeaserChapter;
	bool HasBonusContent;
	bool CommentaryMode;
	bool bIsSelected;

	public GameChapterPanel(NewGameDialog parent, ReadOnlySpan<char> name, ReadOnlySpan<char> chapterName, int chapterIndex, ReadOnlySpan<char> chapterNumber, ReadOnlySpan<char> chapterConfigFile, bool commentaryMode) : base(parent, name) {
		strcpy(ConfigFile, chapterConfigFile);
		strcpy(Chapter, chapterName);

		LevelPicBorder = new(this, "LevelPicBorder");
		LevelPicBorder.MakeReadyForUse();

		LevelPic = new(LevelPicBorder, "LevelPic");
		LevelPic.MakeReadyForUse();

		CommentaryIcon = null;
		CommentaryMode = commentaryMode;
		bIsSelected = false;

		Span<char> text = stackalloc char[32];
		Span<char> num = stackalloc char[32];
		ReadOnlySpan<char> chapter = Localize.Find("#GameUI_Chapter");
		sprintf(text, "%s %s").S(!chapter.IsEmpty ? chapter : "CHAPTER").S(chapterNumber);

		if (false) { // ModInfo.IsSinglePlayerOnly()
			ChapterLabel = new(this, "ChapterLabel", text);
			ChapterNameLabel = new(this, "ChapterNameLabel", chapterName);
		}
		else {
			ChapterLabel = new(this, "ChapterLabel", chapterName);
			ChapterNameLabel = new(this, "ChapterNameLabel", "#GameUI_LoadCommentary");
		}

		SetPaintBackgroundEnabled(false);
		LoadControlSettings("resource/NewGameChapterPanel.res", null);

		LevelPic.GetPos(out _, out int py);
		SetSize(LevelPicBorder.GetWide(), py + LevelPicBorder.GetTall());

		SelectionOverlayPanel overlay = new(this, parent, chapterIndex);
		overlay.SetBounds(0, 0, GetWide(), GetTall());
		overlay.MoveToFront();

		Span<char> temp = stackalloc char[256];
		ChapterNameLabel.GetText(temp);
		TeaserChapter = temp.Equals("Coming Soon", StringComparison.Ordinal);
		HasBonusContent = false;
	}

	public override void ApplySchemeSettings(IScheme scheme) {
		TextColor = scheme.GetColor("NewGame.TextColor", new(255, 255, 255, 255));
		FillColor = scheme.GetColor("NewGame.FillColor", new(255, 255, 255, 255));
		DisabledColor = scheme.GetColor("NewGame.DisabledColor", new(255, 255, 255, 255));
		SelectedColor = scheme.GetColor("NewGame.SelectionColor", new(255, 255, 255, 255));

		base.ApplySchemeSettings(scheme);

		if (TeaserChapter)
			ChapterLabel.SetVisible(false);

		CommentaryIcon = (ImagePanel?)FindChildByName("CommentaryIcon");
		CommentaryIcon?.SetVisible(CommentaryMode);
	}

	bool IsSelected() => bIsSelected;

	public void SetSelected(bool state) {
		bIsSelected = state;

		if (!IsEnabled()) {
			ChapterLabel.SetFgColor(DisabledColor);
			ChapterNameLabel.SetFgColor(new(0, 0, 0, 0));
			LevelPicBorder.SetFillColor(DisabledColor);
			LevelPic.SetAlpha(128);
			return;
		}

		if (state) {
			ChapterLabel.SetFgColor(SelectedColor);
			ChapterNameLabel.SetFgColor(SelectedColor);
			LevelPicBorder.SetFillColor(SelectedColor);
		}
		else {
			ChapterLabel.SetFgColor(TextColor);
			ChapterNameLabel.SetFgColor(TextColor);
			LevelPicBorder.SetFillColor(FillColor);
		}

		LevelPic.SetAlpha(255);
	}

	ReadOnlySpan<char> GetConfigFile() => ConfigFile;
	ReadOnlySpan<char> GetCharster() => Chapter;
	bool IsTeaserChapter() => TeaserChapter;
	bool HasBonus() => HasBonusContent;

	public void SetCommentaryMode(bool commentaryMode) {
		CommentaryMode = commentaryMode;
		CommentaryIcon?.SetVisible(commentaryMode);
	}
}

class NewGameDialog : Frame
{
	const int INVALID_INDEX = -1;
	const int SLOT_OFFLEFT = 0;
	const int SLOW_LEFT = 1;
	const int SLOT_CENTER = 2;
	const int SLOW_RIGHT = 3;
	const int SLOT_OFFRIGHT = 4;
	const int NUM_SLOTS = 5;

	enum EScrollDirection
	{
		Right = -1,
		None = 0,
		Left = 1
	}

	EScrollDirection ScrollDirection;
	int SelectedChapter;
	Button PlayButton, NextButton, PrevButton;
	Panel CenterBg;
	readonly List<GameChapterPanel> ChapterPanels = [];
	readonly Label[] ChapterTitleLabels = new Label[2];
	Label BonusSelection;
	ImagePanel BonusSelectionBorder;
	FooterPanel? Footer;
	bool CommentaryMode;
	Label? CommentaryLabel;
	int[] PanelXPos = new int[NUM_SLOTS];
	int[] PanelYPos = new int[NUM_SLOTS];
	float[] PanelAlpha = new float[NUM_SLOTS];
	int[] PanelIndex = new int[NUM_SLOTS];
	float ScrollSpeed;
	int ButtonPressed;
	int ScrollCt;
	bool Scrolling;
	char ActiveTitleIdx;
	bool MapStarting;
	int iBonusSelection;
	bool ScrollToFirstBonusMap;
	KeyRepeatHandler KeyRepeat;

	public NewGameDialog(Panel? parent, bool commentaryMode) : base(parent, "NewGameDialog") {
		SetDeleteSelfOnClose(true);
		SetBounds(0, 0, 372, 160);
		SetSizeable(false);

		SelectedChapter = -1;
		ActiveTitleIdx = '\0';

		CommentaryMode = commentaryMode;
		MapStarting = false;
		Scrolling = false;
		ScrollCt = 0;
		ScrollSpeed = 0.0f;
		ButtonPressed = 0;
		ScrollDirection = 0;
		CommentaryLabel = null;

		iBonusSelection = 0;
		ScrollToFirstBonusMap = false;

		SetTitle("#GameUI_NewGame", true);

		NextButton = new Button(this, "Next", "#gameui_next");
		PrevButton = new Button(this, "Prev", "#gameui_prev");
		PlayButton = new NewGamePlayButton(this, "Play", "#GameUI_Play");
		PlayButton.SetCommand("Play");

		Button cancel = new(this, "Cancel", "#GameUI_Cancel");
		cancel.SetCommand("Close");

		CenterBg = new(this, "CenterBG");
		CenterBg.MakeReadyForUse();
		CenterBg.SetVisible(false);

		Footer = null;

		const int MAX_CHAPTERS = 32;
		Chapter[] chapters = new Chapter[MAX_CHAPTERS];

		Span<char> fullFileName = stackalloc char[MAX_PATH];
		int chapterIndex = 0;

		ReadOnlySpan<char> fileName = "cfg/chapter*.cfg";
		fileName = fileSystem.FindFirstEx(fileName, null, out FileFindHandle_t findHandle);

		while (!fileName.IsEmpty && chapterIndex < MAX_CHAPTERS) {
			sprintf(fullFileName, "cfg/%s").S(fileName);
			IFileHandle? f = fileSystem.Open(fullFileName, FileOpenOptions.Read);
			if (f != null) {
				if (fileSystem.Size(fullFileName) > 0) {
					chapters[chapterIndex].Filename = new(fileName);
					++chapterIndex;
				}
				fileSystem.FindClose(findHandle);
			}

			fileName = fileSystem.FindNext(findHandle);
		}

		ConVarRef sv_unlockedchapters = new("sv_unlockedchapters");

		ReadOnlySpan<char> unlockedChapter = sv_unlockedchapters.IsValid() ? sv_unlockedchapters.GetString() : "1";
		int iUnlockedChapter = int.Parse(unlockedChapter);

		Span<char> chapterID = stackalloc char[32];
		Span<char> chapterName = stackalloc char[64];

		for (int i = 0; i < chapterIndex; i++) {
			ReadOnlySpan<char> fName = chapters[i].Filename;

			chapterID.Clear();
			new ScanF(fName, "chapter%s").Read(out int id);
			strcpy(chapterID, id.ToString());

			int extIdx = chapterID.IndexOf(".cfg", StringComparison.OrdinalIgnoreCase);
			if (extIdx != -1)
				chapterID[extIdx] = '\0';

			ReadOnlySpan<char> gameDir = Common.Gamedir;
			chapterName.Clear();
			sprintf(chapterName, "#%s_Chapter%s_Title").S(gameDir).S(chapterID);

			GameChapterPanel chapterPanel = new(this, null, chapterName, i, chapterID, fName, commentaryMode);
			chapterPanel.SetVisible(false);
			UpdatePanelLockedStatus(iUnlockedChapter, i + 1, chapterPanel);

			ChapterPanels.Add(chapterPanel);
		}

		LoadControlSettings("Resource/NewGameDialog.res", null);

		for (int i = 0; i < NUM_SLOTS; i++)
			PanelIndex[i] = INVALID_INDEX;

		if (ChapterPanels.Count == 0) {
			UpdateMenuComponents(EScrollDirection.None);
			return;
		}

		int panelWidth = ChapterPanels[0].GetWide() + 16;
		int dialogWidth = GetWide();

		PanelXPos[2] = (dialogWidth - panelWidth) / 2 + 8;

		if (ChapterPanels.Count > 1) {
			PanelXPos[1] = PanelXPos[2] - panelWidth;
			PanelXPos[0] = PanelXPos[1];
			PanelXPos[3] = PanelXPos[2] + panelWidth;
			PanelXPos[4] = PanelXPos[3];
		}
		else PanelXPos[0] = PanelXPos[1] = PanelXPos[3] = PanelXPos[4] = PanelXPos[2];

		PanelAlpha[0] = 0;
		PanelAlpha[1] = 255;
		PanelAlpha[2] = 255;
		PanelAlpha[3] = 255;
		PanelAlpha[4] = 0;

		ChapterPanels[0].GetSize(out _, out int panelHeight);
		CenterBg.SetWide(panelWidth + 16);
		CenterBg.SetPos(PanelXPos[2] - 8, PanelYPos[2] - (CenterBg.GetTall() - panelHeight) + 8);
		CenterBg.SetBgColor(new(190, 115, 0, 255));

		SetSelectedChapterIndex(0);
	}

	public override void Activate() {
		MapStarting = false;

		SetTitle(CommentaryMode ? "#GameUI_LoadCommentary" : "#GameUI_NewGame", true);

		CommentaryLabel?.SetVisible(CommentaryMode);

		ConVarRef var = new("sv_unlockedchapters");
		ReadOnlySpan<char> unlockedChapter = var.IsValid() ? var.GetString() : "1";
		int iUnlockedChapter = int.Parse(unlockedChapter);

		for (int i = 0; i < ChapterPanels.Count; i++) {
			GameChapterPanel chapterPanel = ChapterPanels[i];

			if (chapterPanel != null) {
				chapterPanel.SetCommentaryMode(CommentaryMode);
				UpdatePanelLockedStatus(iUnlockedChapter, i + 1, chapterPanel);
			}
		}

		base.Activate();
	}

	public override void ApplySettings(KeyValues inResourceData) {
		base.ApplySettings(inResourceData);

		int ypos = inResourceData.GetInt("chapterypos", 40);
		for (int i = 0; i < NUM_SLOTS; ++i)
			PanelYPos[i] = ypos;

		CenterBg.SetTall(inResourceData.GetInt("centerbgtall", 0));

		//g_ ScrollSpeedSlow = inResourceData.GetFloat("scrollslow", 0.0f);
		//g_ ScrollSpeedFast = inResourceData.GetFloat("scrollfast", 0.0f);
		SetFastScroll(false);
	}

	public override void ApplySchemeSettings(IScheme scheme) {
		base.ApplySchemeSettings(scheme);

		UpdateMenuComponents(EScrollDirection.None);

		CommentaryLabel = (Label?)FindChildByName("CommentaryLabel");
		CommentaryLabel?.SetVisible(CommentaryMode);
	}

	void UpdateMenuComponents(EScrollDirection dir) {
		int centerIdx = SLOT_CENTER;
		if (dir == EScrollDirection.Left)
			++centerIdx;
		else if (dir == EScrollDirection.Right)
			--centerIdx;

		int leftIdx = centerIdx - 1;
		int rightIdx = centerIdx + 1;

		if (PanelIndex[leftIdx] == INVALID_INDEX || PanelIndex[leftIdx] == 0) {
			PrevButton.SetVisible(false);
			PrevButton.SetEnabled(false);
		}
		else {
			PrevButton.SetVisible(true);
			PrevButton.SetEnabled(true);
		}

		if (ChapterPanels.Count < 4) {
			NextButton.SetVisible(true);
			NextButton.SetEnabled(false);
		}
		else if (PanelIndex[rightIdx] == INVALID_INDEX || PanelIndex[rightIdx] == ChapterPanels.Count - 1) {
			NextButton.SetVisible(false);
			NextButton.SetEnabled(false);
		}
		else {
			NextButton.SetVisible(true);
			NextButton.SetEnabled(true);
		}
	}

	void UpdateBonusSelection() { }

	public void SetSelectedChapterIndex(int index) {
		SelectedChapter = index;

		for (int i = 0; i < ChapterPanels.Count; i++) {
			if (i == index)
				ChapterPanels[i].SetSelected(true);
			else
				ChapterPanels[i].SetSelected(false);
		}

		PlayButton?.SetEnabled(true);

		int selectedSlot = index % 3 + 1;
		int currIdx = index;
		for (int i = selectedSlot; i >= 0 && currIdx >= 0; --i) {
			PanelIndex[i] = currIdx;
			--currIdx;
			InitPanelIndexForDisplay(i);
		}

		currIdx = index + 1;
		for (int i = selectedSlot + 1; i < NUM_SLOTS && currIdx < ChapterPanels.Count; ++i) {
			PanelIndex[i] = currIdx;
			++currIdx;
			InitPanelIndexForDisplay(i);
		}

		UpdateMenuComponents(EScrollDirection.None);
	}

	void SetSelectedChapter(ReadOnlySpan<char> chapter) { }

	void UpdatePanelLockedStatus(int unlockedChapter, int i, GameChapterPanel chapterPanel) {
		if (unlockedChapter <= 0)
			unlockedChapter = 1;

		bool locked = false;

		if (CommentaryMode)
			locked = unlockedChapter <= i;
		else {
			if (unlockedChapter < i)
				locked = i != 0;
		}

		chapterPanel.SetEnabled(!locked);
	}

	void PreScroll(EScrollDirection dir) { }

	void PostScroll(EScrollDirection dir) { }

	void ScrollSelectionPanels(EScrollDirection dir) { }

	void ScrollBonusSelection(EScrollDirection dir) { }

	void AnimateSelectionPanels() { }

	void ShiftPanelIndices(int offset) { }

	bool IsValidPanel(int idx) {
		throw new NotImplementedException();
	}

	void InitPanelIndexForDisplay(int idx) {
		GameChapterPanel panel = ChapterPanels[PanelIndex[idx]];
		if (panel != null) {
			panel.SetPos(PanelXPos[idx], PanelYPos[idx]);
			panel.SetAlpha(PanelAlpha[idx]);
			panel.SetVisible(true);
			if (PanelAlpha[idx] > 0)
				panel.SetZPos(50);
		}
	}

	void SetFastScroll(bool fast) { }

	void ContinueScrolling() { }

	void FinishScroll() { }

	void StartGame() { }

	public override void OnClose() { }

	public override void OnCommand(ReadOnlySpan<char> command) { }

	public override void OnKeyCodePressed(ButtonCode code) { }

	public override void OnKeyCodeReleased(ButtonCode code) { }

	public override void OnThink() {
		// ButtonCode code = KeyRepeat.KeyRepeated();
		// todo
	}
}