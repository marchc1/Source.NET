using Source.Common;
using Source.Common.Engine;
using Source.Common.Formats.Keyvalues;
using Source.Common.Input;
using Source.Common.MaterialSystem;
using Source.Engine;
using Source.GUI.Controls;

namespace Game.UI;

struct RatioToAspectMode
{
	public int Anamorphic;
	public float AspectRatio;
}

struct AAMode
{
	int NumSamples;
	int QualityLevel;
}

class GammaDialog : Frame
{
	public GammaDialog(Panel? parent, ReadOnlySpan<char> name) : base(parent, name) {

	}
}

class OptionsSubVideoAvancedDlg : Frame
{
	public OptionsSubVideoAvancedDlg(Panel? parent, ReadOnlySpan<char> name) : base(parent, name) {

	}
}

public class OptionsSubVideo : PropertyPage
{
	readonly public ModInfo ModInfo = Singleton<ModInfo>();

	readonly RatioToAspectMode[] RatioToAspectModes = [
		new () { Anamorphic = 0, AspectRatio = 4.0f / 3.0f },
		new () { Anamorphic = 1, AspectRatio = 16.0f / 9.0f },
		new () { Anamorphic = 2, AspectRatio = 16.0f / 10.0f },
		new () { Anamorphic = 3, AspectRatio = 1.0f }
	];

	int[] DirectXLevels = [
		70,
		80,
		81,
		90,
		95
	];

	int SelectedMode;
	bool DisplayedVRModeMessage;

	ComboBox Mode;
	ComboBox Windowed;
	ComboBox AspectRatio;
	ComboBox VRMode;
	Button GammaButton;
	Button Advanced;
	Button Benchmark;
	CheckButton HDContent;

	OptionsSubKeyboardAdvancedDlg OptionsSubKeyboardAdvancedDlg;
	OptionsSubVideoThirdPartyCreditsDlg OptionsSubVideoThirdPartyCreditsDlg;
	GammaDialog GammaDialog;

	bool RequiredRestart;
	URLButton ThirdPartyCredits;

	// Messages -> ControlModified, TextChanged, OpenAdvanced, LaunchBenchmark, OpenGammDialog, OpenThirdPartVideoCreditsDialog

	readonly static KeyValues KV_LaunchBenchmark = new("LaunchBenchmark");
	readonly static KeyValues KV_OpenAdvanced = new("OpenAdvanced");
	public OptionsSubVideo(Panel? parent, ReadOnlySpan<char> name) : base(parent, null) {
		GammaButton = new(this, "GammaButton", "#GameUI_AdjustGamma");
		Mode = new(this, "Resolution", 8, false);
		AspectRatio = new(this, "AspectRatio", 6, false);
		VRMode = new(this, "VRMode", 2, false);
		Advanced = new(this, "AdvancedButton", "#GameUI_AdvancedEllipsis");
		Advanced.SetCommand(KV_OpenAdvanced);
		Benchmark = new(this, "BenchmarkButton", "#GameUI_LaunchBenchmark");
		Benchmark.SetCommand(KV_LaunchBenchmark);
		ThirdPartyCredits = new(this, "ThirdPartyVideoCredits", "#GameUI_ThirdPartyTechCredits");
		ThirdPartyCredits.SetCommand(new KeyValues("OpenThirdPartyVideoCreditsDialog"));//static
		HDContent = new(this, "HDContentButton", "#GameUI_HDContent");

		ReadOnlySpan<char> aspect1 = Localize.Find("#GameUI_AspectNormal");
		ReadOnlySpan<char> aspect2 = Localize.Find("#GameUI_AspectWide16x9");
		ReadOnlySpan<char> aspect3 = Localize.Find("#GameUI_AspectWide16x10");

		int NormalItemID = AspectRatio.AddItem(aspect1, null);
		int i16x9ItemID = AspectRatio.AddItem(aspect2, null);
		int i16x10ItemID = AspectRatio.AddItem(aspect3, null);

		MaterialSystem_Config config = Materials.GetCurrentConfigForVideoCard();

		int aspectMode = GetScreenAspectMode(config.VideoMode.Width, config.VideoMode.Height);
		switch (aspectMode) {
			case 0:
				AspectRatio.ActivateItem(NormalItemID);
				break;
			case 1:
				AspectRatio.ActivateItem(i16x9ItemID);
				break;
			case 2:
				AspectRatio.ActivateItem(i16x10ItemID);
				break;
		}


		Windowed = new(this, "DisplayModeCombo", 5, false);
		Windowed.AddItem("#GameUI_Fullscreen", null);
		Windowed.AddItem("#GameUI_Windowed", null);

		PrepareResolutionList();

		LoadControlSettings("resource/OptionsSubVideo.res");

		Benchmark.SetVisible(fileSystem.FileExists("maps/test_hardware.bsp"));
		if (!ModInfo.SupportsVR()) VRMode.SetVisible(false);
		if (!ModInfo.HasHDContent()) HDContent.SetVisible(false);
	}

	// FIXME #37
	public override void Dispose() {
		base.Dispose();
		OptionsSubVideoThirdPartyCreditsDlg?.MarkForDeletion();
	}

	private void PrepareResolutionList() {
		Span<char> sz = stackalloc char[256];
		Mode.GetText(sz);

		new ScanF(sz, "%i x %i").Read(out int currentWidth).Read(out int currentHeight);

		Mode.RemoveAll();
		AspectRatio.SetItemEnabled(1, false);
		AspectRatio.SetItemEnabled(2, false);

		VMode[] list = [];
		// gameuifuncs.GetVideoModes // todo

		MaterialSystem_Config config = Materials.GetCurrentConfigForVideoCard();

		bool windowed = Windowed.GetActiveItem() >= (Windowed.GetItemCount() - 1);
		int desktopWidth = 1600, desktopHeight = 900; // todo gameuifuncs.GetDesktopResolution

		bool newFullscreenDisplay = !windowed && (/*todo*/ 0 != Windowed.GetActiveItem());
		if (newFullscreenDisplay) {
			currentWidth = desktopWidth;
			currentHeight = desktopHeight;
		}

		bool foundWidescreen = false;
		int selectedItemID = -1;
		foreach (VMode mode in list) {
			if (mode.Width > desktopWidth || mode.Height > desktopHeight)
				continue;

			GetResolutionName(mode, sz, desktopWidth, desktopHeight);

			int itemID = -1;
			int aspectMode = GetScreenAspectMode(mode.Width, mode.Height);
			if (aspectMode > 0) {
				AspectRatio.SetItemEnabled(aspectMode, true);
				foundWidescreen = true;
			}

			if (aspectMode == AspectRatio.GetActiveItem())
				itemID = Mode.AddItem(sz, null);

			if (mode.Width == currentWidth && mode.Height == currentHeight)
				selectedItemID = itemID;
			else if (selectedItemID == -1 && mode.Width == config.VideoMode.Width && mode.Height == config.VideoMode.Height)
				selectedItemID = itemID;
		}

		AspectRatio.SetEnabled(foundWidescreen);

		SelectedMode = selectedItemID;

		if (selectedItemID != -1)
			Mode.ActivateItem(selectedItemID);
		else {
			int width = config.VideoMode.Width;
			int height = config.VideoMode.Height;

			if (newFullscreenDisplay || (width > desktopWidth) || (height > desktopHeight)) {
				width = desktopWidth;
				height = desktopHeight;
			}

			sprintf(sz, "%i x %i").I(width).I(height);
			Mode.SetText(sz);
		}
	}

	private bool BUseHDContent() {
		throw new NotImplementedException();
	}

	private void SetUseHDContent(bool use) {
		throw new NotImplementedException();
	}

	public override void OnResetData() {

	}

	private void SetCurrentResolutionComboItem() {
		throw new NotImplementedException();
	}

	public override void OnApplyChanges() {

	}

	public override void PerformLayout() {
		base.PerformLayout();

		if (GammaButton != null) {
			MaterialSystem_Config config = Materials.GetCurrentConfigForVideoCard();
			GammaButton.SetEnabled(!config.Windowed());
		}
	}

	public override void OnTextChanged(Panel from) {

	}

	private void OnDataChanged() => PostActionSignal(new KeyValues("ApplyButtonEnabled"));//static

	private bool RequiresRestart() {
		throw new NotImplementedException();
	}

	private void OpenAdvanced() {
		OptionsSubKeyboardAdvancedDlg ??= new(BasePanel.g_BasePanel!.FindChildByName("OptionsDialog"));
		OptionsSubKeyboardAdvancedDlg.Activate();
	}

	private void OpenGammaDialog() {
		GammaDialog ??= new(this, "GammaDialog");
		GammaDialog.Activate();
	}

	private void LaunchBenchmark() {
		// BasePanel.g_BasePanel?.OnOpenBenchmarkDialog(); // todo
	}

	private void OpenThirdPartyVideoCreditsDialog() {
		OptionsSubVideoThirdPartyCreditsDlg ??= new(this);
		OptionsSubVideoThirdPartyCreditsDlg.Activate();
	}

	private void GetNameForDXLevel(int level, Span<char> name) {
		if (level >= 92 && level <= 95)
			strcpy(name, "DirectX v9.0+");
		else {
			strcpy(name, $"DirectX v{level / 10.0f:F1}");
		}
	}

	private int GetScreenAspectMode(int width, int height) {
		float aspectRatio = width / height;
		float closestAspectRatioDist = 99999.0f;
		int closestAnamorphic = 0;

		for (int i = 0; i < RatioToAspectModes.Length; i++) {
			float dist = MathF.Abs(RatioToAspectModes[i].AspectRatio - aspectRatio);
			if (dist < closestAspectRatioDist) {
				closestAspectRatioDist = dist;
				closestAnamorphic = RatioToAspectModes[i].Anamorphic;
			}
		}

		return closestAnamorphic;
	}

	private void GetResolutionName(VMode mode, Span<char> name, int desktopWidth, int desktopHeight)
		=> sprintf(name, "%i x %i%s").I(mode.Width).I(mode.Height).S(mode.Width == desktopWidth && mode.Height == desktopHeight ? " (native)" : "");

	FileStream FOpenGameHDFile(FileMode mode) {
		// ReadOnlySpan<char> gameDir = engine.GetGameDirectory(); // todo

		// Span<char> modSteamInfPath = stackalloc char[1024];
		// sprintf(modSteamInfPath, "{0}/game_hd.txt").S(gameDir);

		// return File.Open(modSteamInfPath.ToString(), mode);

		throw new NotImplementedException();
	}
}

class OptionsSubVideoThirdPartyCreditsDlg : Frame
{
	public OptionsSubVideoThirdPartyCreditsDlg(Panel? parent) : base(null, null) {
		SetTitle("#GameUI_ThirdPartyVideo_Title", true);
		SetSize(500, 200);
		LoadControlSettings("resource/OptionsSubVideoThirdPartyDlg.res");
		MoveToCenterOfScreen();
		SetSizeable(false);
		SetDeleteSelfOnClose(true);
	}

	public override void Activate() {
		base.Activate();
		Input.SetAppModalSurface(this);
	}

	public override void OnKeyCodeTyped(ButtonCode code) {
		if (code == ButtonCode.KeyEscape)
			Close();
		else
			base.OnKeyCodeTyped(code);
	}
}