using Source.Common;
using Source.Common.Formats.Keyvalues;
using Source.GUI.Controls;

namespace Game.UI;

public struct RadioToAspectMode
{
	public int anamorphic;
	public float aspectRatio;
}

struct AAode
{
	int NumSamples;
	int QualityLevel;
}

// RadioToAspectMode[] RadioToAspectModes = [
// 	new () { anamorphic = 0, aspectRatio = 4.0f / 3.0f },
// 	new () { anamorphic = 1, aspectRatio = 16.0f / 9.0f },
// 	new () { anamorphic = 2, aspectRatio = 16.0f / 10.0f },
// 	new () { anamorphic = 3, aspectRatio = 1.0f }
// ];

// int[] g_DirectXLevels = {
// 	70,
// 	80,
// 	81,
// 	90,
//   // DX_TO_GL_ABSTRACTION -> 92
//   95
// };

class CGammaDialog : Frame
{
	public CGammaDialog(Panel? parent, ReadOnlySpan<char> name) : base(parent, name) {
	}
}

class COptionsSubVideoAvancedDlg : Frame
{
	public COptionsSubVideoAvancedDlg(Panel? parent, ReadOnlySpan<char> name) : base(parent, name) {

	}
}

public class OptionsSubVideo : PropertyPage
{
	readonly public ModInfo ModInfo = Singleton<ModInfo>(); //r

	int SelectedMode;
	bool DisplayedVRModeMessage;

	ComboBox Mode;
	ComboBox Windowed;
	ComboBox AspectRatio;
	ComboBox VRMode;
	Button GammeButton;
	Button Advanced;
	Button Benchmark;
	CheckButton HDContent;

	OptionsSubKeyboardAdvancedDlg OptionsSubKeyboardAdvancedDlg;
	CGammaDialog GammaDialog;

	bool RequiredRestart;
	URLButton ThirdPartyCredits;
	// COptionsSubVideoThirdPartyCreditsDlg OptionsSubVideoThirdPartyCreditsDlg

	// Messages -> ControlModified, TextChanged, OpenAdvanced, LaunchBenchmark, OpenGammDialog, OpenThirdPartVideoCreditsDialog

	readonly static KeyValues KV_LaunchBenchmark = new("LaunchBenchmark");
	readonly static KeyValues KV_OpenAdvanced = new("OpenAdvanced");
	public OptionsSubVideo(Panel? parent, ReadOnlySpan<char> name) : base(parent, null) {
		GammeButton = new(this, "GammaButton", "#GameUI_AdjustGamma");
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

		ReadOnlySpan<char> aspect1 = Localize.Find("#GameUI_AspectRatio4x3");
		ReadOnlySpan<char> aspect2 = Localize.Find("#GameUI_AspectRatio16x9");
		ReadOnlySpan<char> aspect3 = Localize.Find("#GameUI_AspectRatio16x10");

		int NormalItemID = AspectRatio.AddItem(aspect1, null);
		int i16x9ItemID = AspectRatio.AddItem(aspect2, null);
		int i16x10ItemID = AspectRatio.AddItem(aspect3, null);

		// todo materials.GetCurrentConfigForVideoCard();

		LoadControlSettings("resource/OptionsSubVideo.res");

		Benchmark.SetVisible(fileSystem.FileExists("maps/test_hardware.bsp"));
		if (!ModInfo.SupportsVR()) VRMode.SetVisible(false);
		if (!ModInfo.HasHDContent()) HDContent.SetVisible(false);
	}
}