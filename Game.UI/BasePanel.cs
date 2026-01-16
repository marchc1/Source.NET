using Source;
using Source.Common;
using Source.Common.Client;
using Source.Common.Commands;
using Source.Common.Filesystem;
using Source.Common.Formats.Keyvalues;
using Source.Common.GameUI;
using Source.Common.GUI;
using Source.Common.Input;
using Source.GUI.Controls;

namespace Game.UI;

public class GameMenuItem : MenuItem
{
	public GameMenuItem(Menu panel, string name, string text) : base(panel, name, text) {
		RightAligned = false;
	}

	bool RightAligned;

	public override void ApplySchemeSettings(IScheme scheme) {
		base.ApplySchemeSettings(scheme);

		SetFgColor(GetSchemeColor("MainMenu.TextColor", scheme));
		SetBgColor(new(0, 0, 0, 0));
		SetDefaultColor(GetSchemeColor("MainMenu.TextColor", scheme), new(0, 0, 0, 0));
		SetArmedColor(GetSchemeColor("MainMenu.ArmedTextColor", scheme), new(0, 0, 0, 0));
		SetDepressedColor(GetSchemeColor("MainMenu.DepressedTextColor", scheme), new(0, 0, 0, 0));
		SetContentAlignment(Alignment.West);
		SetBorder(null);
		SetDefaultBorder(null);
		SetDepressedBorder(null);
		SetKeyFocusBorder(null);

		IFont? mainMenuFont = scheme.GetFont("MainMenuFont", IsProportional());

		if (mainMenuFont != null)
			SetFont(mainMenuFont);
		else
			SetFont(scheme.GetFont("MenuLarge", IsProportional()));

		SetTextInset(0, 0);
		SetArmedSound("UI/buttonrollover.wav");
		SetDepressedSound("UI/buttonclick.wav");
		SetReleasedSound("UI/buttonclickrelease.wav");
		SetButtonActivationType(ActivationType.OnPressed);

		if (RightAligned)
			SetContentAlignment(Alignment.East);
	}

	public void SetRightAlignedText(bool state) => RightAligned = state;
}

public enum BackgroundState
{
	Initial,
	Loading,
	MainMenu,
	Level,
	Disconnected,
	Exiting,
}

public class GameMenu(Panel parent, string name) : Menu(parent, name)
{
	protected override void LayoutMenuBorder() { }
	public override int AddMenuItem(ReadOnlySpan<char> itemName, ReadOnlySpan<char> itemText, ReadOnlySpan<char> command, Panel? target, KeyValues? userData = null) {
		MenuItem item = new GameMenuItem(this, new string(itemName), new string(itemText));
		item.AddActionSignalTarget(target);
		item.SetCommand(command);
		item.SetText(itemText);
		item.SetUserData(userData);
		return base.AddMenuItem(item);
	}
	public override int AddMenuItem(ReadOnlySpan<char> itemName, ReadOnlySpan<char> itemText, KeyValues command, Panel? target, KeyValues? userData = null) {
		MenuItem item = new GameMenuItem(this, new string(itemName), new string(itemText));
		item.AddActionSignalTarget(target);
		item.SetCommand(command);
		item.SetText(itemText);
		item.SetUserData(userData);
		return base.AddMenuItem(item);
	}
	public void UpdateMenuItemState(bool isInGame, bool isMultiplayer) {
		for (int i = 0; i < GetChildCount(); i++) {
			Panel child = GetChild(i);
			if (child is MenuItem menuItem) {
				bool shouldBeVisible = true;
				// filter the visibility
				KeyValues? kv = menuItem.GetUserData();
				if (kv == null)
					continue;

				bool vrEnabled = false, vrActive = false;

				if (!isInGame && kv.GetInt("OnlyInGame") != 0) shouldBeVisible = false;
				if (!isInGame && !isMultiplayer && kv.GetInt("notsingle") != 0) shouldBeVisible = false;
				else if (isMultiplayer && kv.GetInt("notmulti") != 0) shouldBeVisible = false;
				else if (!vrEnabled && kv.GetInt("OnlyWhenVREnabled") != 0) shouldBeVisible = false;
				else if (!vrActive && kv.GetInt("OnlyWhenVRActive") != 0) shouldBeVisible = false;
				else if (vrEnabled && kv.GetInt("OnlyWhenVRInactive") != 0) shouldBeVisible = false;

				menuItem.SetVisible(shouldBeVisible);
			}
		}

		InvalidateLayout();
	}


	IPanel? MainMenuOverridePanel;

	public override void SetVisible(bool state) {
		if (MainMenuOverridePanel != null) {
			MainMenuOverridePanel.SetVisible(true);
			if (!state)
				MainMenuOverridePanel.MoveToBack();
		}

		base.SetVisible(true);

		if (!state)
			MoveToBack();
	}
	public override void ApplySchemeSettings(IScheme scheme) {
		base.ApplySchemeSettings(scheme);

		SetMenuItemHeight(int.TryParse(scheme.GetResourceString("MainMenu.MenuItemHeight"), out int r) ? r : 0);
		SetBgColor(new(0, 0, 0, 0));
		SetBorder(null);
	}
	public override void OnSetFocus() {
		base.OnSetFocus();
	}
	public override void OnKeyCodePressed(ButtonCode code) {
		int dir = 0;
		switch (code) {
			case ButtonCode.KeyUp:
				dir = -1;
				break;
			case ButtonCode.KeyDown:
				dir = 1;
				break;
		}

		if (dir != 0) {

		}

		base.OnKeyCodePressed(code);
	}

	public override void OnKillFocus(Panel? newPanel) {
		base.OnKillFocus(newPanel);

		if (MainMenuOverridePanel != null)
			Surface.MovePopupToBack(MainMenuOverridePanel);
		else
			Surface.MovePopupToBack(this);
	}
}

public class MainMenuGameLogo : EditablePanel
{
	readonly public IEngineClient engine = Singleton<IEngineClient>();

	int OffsetX;
	int OffsetY;

	public MainMenuGameLogo(Panel? parent, string name) : base(parent, name) {

	}
	public override void ApplySettings(KeyValues resourceData) {
		base.ApplySettings(resourceData);

		OffsetX = resourceData.GetInt("offsetX", 0);
		OffsetY = resourceData.GetInt("offsetY", 0);
	}
	public override void ApplySchemeSettings(IScheme scheme) {
		base.ApplySchemeSettings(scheme);

		KeyValues conditions = new KeyValues("conditions");
		Span<char> background = stackalloc char[MAX_PATH];
		engine.GetMainMenuBackgroundName(background);

		KeyValues subKey = new KeyValues(background);
		conditions.AddSubKey(subKey);

		LoadControlSettings("resource/GameLogo.res", null, null, conditions);
	}
}
public class BackgroundMenuButton : Button
{
	public BackgroundMenuButton(Panel parent, string name) : base(parent, name, "") {

	}
	public override void ApplySchemeSettings(IScheme scheme) {
		base.ApplySchemeSettings(scheme);

		SetFgColor(new(255, 255, 255, 255));
		SetBgColor(new(0, 0, 0, 0));
		SetDefaultColor(new(255, 255, 255, 255), new(0, 0, 0, 0));
		SetArmedColor(new(255, 255, 0, 255), new(0, 0, 0, 0));
		SetDepressedColor(new(255, 255, 0, 255), new(0, 0, 0, 0));
		SetContentAlignment(Alignment.West);
		SetBorder(null);
		SetDefaultBorder(null);
		SetDepressedBorder(null);
		SetKeyFocusBorder(null);
		SetTextInset(0, 0);
	}
}
public class QuitQueryBox : QueryBox
{
	public QuitQueryBox(string title, string queryText, Panel? parent = null) : base(title, queryText, parent) {

	}

	IGameUI GameUI = Singleton<IGameUI>();

	public override void DoModal(Frame? frameOver) {
		base.DoModal(frameOver);
		Surface.RestrictPaintToSinglePanel(this);
		GameUI.PreventEngineHideGameUI();
	}
	public override void OnKeyCodeTyped(ButtonCode code) {
		if (code == ButtonCode.KeyEscape)
			Close();
		else
			base.OnKeyCodeTyped(code);
	}
	public override void OnClose() {
		base.OnClose();
		Surface.RestrictPaintToSinglePanel(null);
		GameUI.AllowEngineHideGameUI();
	}
}
public class BasePanel : Panel
{
	GameMenu? GameMenu;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
	readonly public IFileSystem FileSystem = Singleton<IFileSystem>();
	readonly public GameUI GameUI;
	readonly public IEngineClient engine = Singleton<IEngineClient>();
	readonly public ModInfo ModInfo = Singleton<ModInfo>();
#pragma warning restore CS8618

	TextureID BackgroundImageID = TextureID.INVALID;

	OptionsDialog? OptionsDialog;
	static BasePanel? g_BasePanel;


	public override void OnCommand(ReadOnlySpan<char> command) {
		RunMenuCommand(command);
	}

	[ConCommand("gamemenucommand")]
	static void Gamemenucommand(in TokenizedCommand args) {
		if (args.ArgC() < 2)
			return;

		g_BasePanel?.RunMenuCommand(args[1]);
	}

	private void RunMenuCommand(ReadOnlySpan<char> command) {
		DevMsg($"Incoming BasePanel message '{command}'\n");
		switch (command) {
			case "OpenGameMenu": break;
			case "OpenPlayerListDialog": break;
			case "OpenNewGameDialog": break;
			case "OpenLoadGameDialog": break;
			case "OpenSaveGameDialog": break;
			case "OpenBonusMapsDialog": break;
			case "OpenOptionsDialog": OnOpenOptionsDialog(); break;
			case "OpenControllerDialog": break;
			case "OpenBenchmarkDialog": break;
			case "OpenServerBrowser": OnOpenServerBrowser(); break;
			case "OpenFriendsDialog": break;
			case "OpenLoadDemoDialog": break;
			case "OpenCreateMultiplayerGameDialog": break;
			case "OpenChangeGameDialog": break;
			case "OpenLoadCommentaryDialog": break;
			case "OpenLoadSingleplayerCommentaryDialog": break;
			case "OpenMatchmakingBasePanel": break;
			case "OpenAchievementsDialog": break;
			case "OpenCSAchievementsDialog": break;
			case "AchievementsDialogClosing": break;
			case "Quit":
				OnOpenQuitConfirmationDialog();
				break;
			case "QuitNoConfirm":
				SetVisible(false);
				Surface.RestrictPaintToSinglePanel(this);
				engine.ClientCmd_Unrestricted("quit\n");
				break;
			case "QuitRestartNoConfirm": break;
			case "ResumeGame": GameUI.HideGameUI(); break;
			case "Disconnect": engine.ClientCmd_Unrestricted("disconnect"); break;
			case "DisconnectNoConfirm": break;
			case "ReleaseModalWindow": Surface.RestrictPaintToSinglePanel(null); break;
			case "ShowSigninUI": break;
			case "ShowDeviceSelector": break;
			case "SignInDenied": break;
			case "RequiredSignInDenied": break;
			case "RequiredStorageDenied": break;
			case "StorageDeviceDenied": break;
			case "clear_storage_deviceID": break;
			case "RestartWithNewLanguage": break;
			default:
				base.OnCommand(command);
				break;
		}
	}

	private void OnOpenOptionsDialog() {
		if (!OptionsDialog.IsValid()) {
			OptionsDialog = new OptionsDialog(this);

			PositionDialog(OptionsDialog);
		}

		OptionsDialog.Activate();
	}

	// FIXME: This is meant to be done through some ModuleLoader system, but that doesn't exist yet,
	// 				so for now I'm cheating
	static ServerBrowser.ServerBrowser? ServerBrowser;
	private void OnOpenServerBrowser() {
		ServerBrowser ??= new();
		if (ServerBrowser.Initialize())
			if (ServerBrowser.PostInitialize())
				ServerBrowser.Activate();
	}

	public void PositionDialog(Panel? dialog) {
		if (!dialog.IsValid())
			return;

		Surface.GetWorkspaceBounds(out int x, out int y, out int ww, out int wt);
		dialog.GetSize(out int wide, out int tall);
		dialog.SetPos(x + ((ww - wide) / 2), y + ((wt - tall) / 2));
	}

	public void OnOpenQuitConfirmationDialog() {
		if (GameUI.IsConsoleUI()) {
			throw new NotImplementedException();
		}

		if (GameUI.IsInLevel() && engine.GetMaxClients() == 1) {
			throw new NotImplementedException();
		}
		else {
			QueryBox box = new QuitQueryBox("#GameUI_QuitConfirmationTitle", "#GameUI_QuitConfirmationText", this);
			box.SetOKButtonText("#GameUI_Quit");
			box.SetOKCommand(new KeyValues("Command", "command", "QuitNoConfirm"));
			box.SetCancelCommand(new KeyValues("Command", "command", "ReleaseModalWindow"));
			box.AddActionSignalTarget(this);
			box.DoModal();
		}
	}

	static BackgroundMenuButton CreateMenuButton(BasePanel parent, ReadOnlySpan<char> panelName, ReadOnlySpan<char> panelText) {
		BackgroundMenuButton button = new BackgroundMenuButton(parent, new string(panelName));
		button.SetProportional(true);
		button.SetCommand("OpenGameMenu");
		button.SetText(panelText);
		return button;
	}

	public BasePanel(GameUI gameUI) : base(null, "BaseGameUIPanel") {
		GameUI = gameUI;
		g_BasePanel = this;
		CreateGameMenu();
		CreateGameLogo();

		SetMenuAlpha(255);

		GameMenuButtons.Add(CreateMenuButton(this, "GameMenuButton", ModInfo!.GetGameTitle()));
		GameMenuButtons.Add(CreateMenuButton(this, "GameMenuButton2", ModInfo!.GetGameTitle2()));
	}

	[PanelAnimationVar("0")] float BackgroundFillAlpha;

	IFont? FontTest;

	public override void PaintBackground() {
		if (!GameUI.IsInLevel() || GameUI.LoadingDialog != null || ExitingFrameCount > 0)
			DrawBackgroundImage();

		if (BackgroundFillAlpha > 0) {
			Surface.DrawSetColor(0, 0, 0, (int)BackgroundFillAlpha);
			Surface.GetScreenSize(out int wide, out int tall);
			Surface.DrawFilledRect(0, 0, wide, tall);
		}
	}

	Coord GameMenuPos;
	int GameMenuInset;

	public override void PerformLayout() {
		base.PerformLayout();

		Surface.GetScreenSize(out int wide, out int tall);
		GameMenu!.GetSize(out int menuWide, out int menuTall);
		int idealMenuY = GameMenuPos.Y;
		if (idealMenuY + menuTall + GameMenuInset > tall)
			idealMenuY = tall - menuTall - GameMenuInset;

		int yDiff = idealMenuY - GameMenuPos.Y;

		for (int i = 0; i < GameMenuButtons.Count; ++i) {
			GameMenuButtons[i].SizeToContents();
			GameMenuButtons[i].SetPos(GameTitlePos[i].X, GameTitlePos[i].Y + yDiff);
		}

		for (int i = 0; i < GameMenuButtons.Count; i++) {
			GameMenuButtons[i].SizeToContents();
		}

		GameMenu.SetPos(GameMenuPos.X, idealMenuY);

		UpdateGameMenus();
	}

	List<Coord> GameTitlePos = [];
	List<BackgroundMenuButton> GameMenuButtons = [];
	float FrameFadeInTime;
	Color BackdropColor;
	int ExitingFrameCount;
	BackgroundState BackgroundState;

	public void SetBackgroundRenderState(BackgroundState state) {
		BackgroundState = state;
	}

	public void UpdateBackgroundState() {
		if (ExitingFrameCount != 0)
			SetBackgroundRenderState(BackgroundState.Exiting);
		else if (GameUI.IsInLevel())
			SetBackgroundRenderState(BackgroundState.Level);
		else if (GameUI.IsInBackgroundLevel() && !LevelLoading)
			SetBackgroundRenderState(BackgroundState.MainMenu);
		else if (LevelLoading)
			SetBackgroundRenderState(BackgroundState.Loading);
		else if (EverActivated && PlatformMenuInitialized)
			SetBackgroundRenderState(BackgroundState.Disconnected);

		if (!PlatformMenuInitialized)
			return;

		int i;
		bool haveActiveDialogs = false;
		bool bIsInLevel = GameUI.IsInLevel();
		for (i = 0; i < GetChildCount(); ++i) {
			IPanel? child = GetChild(i);
			if (child != null && child.IsVisible() && child.IsPopup() && child != GameMenu)
				haveActiveDialogs = true;
		}

		IPanel? parent = GetParent();
		for (i = 0; i < (parent?.GetChildCount() ?? 0); ++i) {
			IPanel? child = parent?.GetChild(i);
			if (child != null && child.IsVisible() && child.IsPopup() && child != this)
				haveActiveDialogs = true;
		}

		bool needDarkenedBackground = (haveActiveDialogs || bIsInLevel);
		if (HaveDarkenedBackground != needDarkenedBackground) {
			float targetAlpha, duration;
			if (needDarkenedBackground) {
				targetAlpha = BackdropColor[3];
				duration = FrameFadeInTime;
			}
			else {
				targetAlpha = 0.0f;
				duration = 2.0f;
			}

			HaveDarkenedBackground = needDarkenedBackground;
			GetAnimationController().RunAnimationCommand(this, "BackgroundFillAlpha", targetAlpha, 0.0f, duration, Interpolators.Linear);
		}

		if (LevelLoading)
			return;

		bool bNeedDarkenedTitleText = haveActiveDialogs;
		if (HaveDarkenedTitleText != bNeedDarkenedTitleText || ForceTitleTextUpdate) {
			float targetTitleAlpha, duration;
			if (haveActiveDialogs) {
				duration = FrameFadeInTime;
				targetTitleAlpha = 32.0f;
			}
			else {
				duration = 2.0f;
				targetTitleAlpha = 255.0f;
			}

			if (GameLogo != null)
				GetAnimationController().RunAnimationCommand(GameLogo, "alpha", targetTitleAlpha, 0.0f, duration, Interpolators.Linear);

			for (i = 0; i < GameMenuButtons.Count; ++i) {
				GetAnimationController().RunAnimationCommand(GameMenuButtons[i], "alpha", targetTitleAlpha, 0.0f, duration, Interpolators.Linear);
			}
			HaveDarkenedTitleText = bNeedDarkenedTitleText;
			ForceTitleTextUpdate = false;
		}
	}

	bool HaveDarkenedBackground;
	bool HaveDarkenedTitleText;
	bool ForceTitleTextUpdate;
	bool PlatformMenuInitialized;
	bool LevelLoading;

	public void RunFrame() {
		InvalidateLayout();

		UpdateBackgroundState();

		if (!PlatformMenuInitialized)
			PlatformMenuInitialized = true;
	}

	public override void ApplySchemeSettings(IScheme scheme) {
		base.ApplySchemeSettings(scheme);

		GameMenuInset = int.TryParse(scheme.GetResourceString("MainMenu.Inset"), out int r) ? r : 0;
		GameMenuInset *= 2;

		IScheme? clientScheme = SchemeManager.LoadSchemeFromFile("resource/ClientScheme.res", "ClientScheme");
		List<Color> buttonColor = [];
		if (clientScheme != null) {
			GameTitlePos.Clear();

			for (int i = 0; i < GameMenuButtons.Count; ++i) {
				GameMenuButtons[i].SetFont(clientScheme.GetFont("ClientTitleFont", true));
				GameTitlePos.Add(new Coord() {
					X = SchemeManager.GetProportionalScaledValue(int.TryParse(clientScheme.GetResourceString($"Main.Title{i + 1}.X"), out int x) ? x : 0),
					Y = SchemeManager.GetProportionalScaledValue(int.TryParse(clientScheme.GetResourceString($"Main.Title{i + 1}.Y"), out int y) ? y : 0),
				});

				buttonColor.Add(clientScheme.GetColor($"Main.Title{i + 1}.Color", new Color(255, 255, 255, 255)));
			}

			GameMenuPos.X = int.TryParse(clientScheme.GetResourceString("Main.Menu.X"), out r) ? r : 0;
			GameMenuPos.X = SchemeManager.GetProportionalScaledValue(GameMenuPos.X);
			GameMenuPos.Y = int.TryParse(clientScheme.GetResourceString("Main.Menu.Y"), out r) ? r : 0;
			GameMenuPos.Y = SchemeManager.GetProportionalScaledValue(GameMenuPos.Y);

			GameMenuInset = int.TryParse(clientScheme.GetResourceString("Main.BottomBorder"), out r) ? r : 0;
			GameMenuInset = SchemeManager.GetProportionalScaledValue(GameMenuInset);
		}
		else {
			for (int i = 0; i < GameMenuButtons.Count; ++i) {
				GameMenuButtons[i].SetFont(scheme.GetFont("TitleFont"));
				buttonColor.Add(new Color(255, 255, 255, 255));
			}
		}

		for (int i = 0; i < GameMenuButtons.Count; ++i) {
			GameMenuButtons[i].SetDefaultColor(buttonColor[i], new Color(0, 0, 0, 0));
			GameMenuButtons[i].SetArmedColor(buttonColor[i], new Color(0, 0, 0, 0));
			GameMenuButtons[i].SetDepressedColor(buttonColor[i], new Color(0, 0, 0, 0));
		}

		FrameFadeInTime = float.TryParse(scheme.GetResourceString("Frame.TransitionEffectTime"), out float f) ? f : 0;
		BackdropColor = scheme.GetColor("mainmenu.backdrop", new Color(0, 0, 0, 128));

		FontTest = scheme.GetFont("TitleFont");

		Surface.GetScreenSize(out int screenWide, out int screenTall);
		float aspectRatio = (float)screenWide / screenTall;
		bool isWidescreen = aspectRatio >= 1.5999f;

		Span<char> filename = stackalloc char[MAX_PATH];
		Span<char> background = stackalloc char[MAX_PATH];
		engine.GetMainMenuBackgroundName(background); background = background.SliceNullTerminatedString();
		Span<char> finalFilename = sprintf(filename, "console/%s").S(background);

		if (BackgroundImageID == TextureID.INVALID)
			BackgroundImageID = Surface.CreateNewTextureID();

		Surface.DrawSetTextureFile(BackgroundImageID, finalFilename, 0, false);
	}

	private void DrawBackgroundImage() {
		int alpha = 255;

		GetSize(out int wide, out int tall);

		TextureID imageID = BackgroundImageID;

		Surface.DrawSetColor(255, 255, 255, alpha);
		Surface.DrawSetTexture(imageID);
		Surface.DrawTexturedRect(0, 0, wide, tall);
	}

	private void SetMenuAlpha(int alpha) {
		GameMenu!.SetAlpha(alpha);
	}

	private void CreateGameMenu() {
		KeyValues datafile = new KeyValues("GameMenu");
		datafile.UsesEscapeSequences(true);
		if (datafile.LoadFromFile(FileSystem, "resource/GameMenu.res"))
			GameMenu = RecursiveLoadGameMenu(datafile);

		if (!GameMenu.IsValid())
			Error("Could not load file Resource/GameMenu.res\n");
		else {
			GameMenu.MakeReadyForUse();
			GameMenu.SetAlpha(0);
		}
	}

	public override void OnThink() {
		base.OnThink();
	}

	private GameMenu RecursiveLoadGameMenu(KeyValues datafile) {
		GameMenu menu = new GameMenu(this, new string(datafile.Name));
		for (KeyValues? dat = datafile.GetFirstSubKey(); dat != null; dat = dat.GetNextKey()) {
			ReadOnlySpan<char> label = dat.GetString("label", "<unknown>");
			ReadOnlySpan<char> cmd = dat.GetString("command", null);
			ReadOnlySpan<char> name = dat.GetString("name", label);

			menu.AddMenuItem(name, label, cmd, this, dat);
		}
		return menu;
	}

	MainMenuGameLogo? GameLogo;

	private void CreateGameLogo() {
		if (ModInfo.UseGameLogo()) {
			GameLogo = new MainMenuGameLogo(this, "GameLogo");

			GameLogo.MakeReadyForUse();
			GameLogo.InvalidateLayout(true, true);
		}
	}

	bool EverActivated;

	internal void OnGameUIActivated() {
		// Map load failed?

		if (!EverActivated) {
			UpdateGameMenus();
			EverActivated = true;
		}
	}

	private void UpdateGameMenus() {
		bool isInGame = GameUI.IsInLevel();
		bool isMulti = isInGame && engine.GetMaxClients() > 1;
		GameMenu!.UpdateMenuItemState(isInGame, isMulti);

		InvalidateLayout();
		GameMenu!.SetVisible(true);
	}

	internal void OnLevelLoadingStarted() {
		LevelLoading = true;
	}

	internal void OnLevelLoadingFinished() {
		LevelLoading = false;
	}

	internal void OnGameUIHidden() {

	}

	public int GetMenuAlpha() {
		return GameMenu!.GetAlpha();
	}
}
