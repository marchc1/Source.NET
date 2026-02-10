using Source.Common.Formats.Keyvalues;
using Source.GUI.Controls;

namespace Game.UI;

// TODO: These probably shouldn't be here
enum VoiceTweakControl
{
	MicrophoneVolume = 0,
	OtherSpeakerScale,
	MicBoost,
	SpeakingVolume
}

interface IVoiceTweak
{
	int StartVoiceTweakMode(); // 0 on error
	void EndVoiceTweakMode();
	void SetControlFloat(VoiceTweakControl control, float value);
	float GetControlFloat(VoiceTweakControl control);
	bool IsStillTweaking(); // This can return false if the user restarts the sound system during voice tweak mode
}

public class OptionsSubVoice : PropertyPage
{
	IVoiceTweak? VoiceTweak;
	CheckButton MicBoost;
	ImagePanel MicMeter;
	ImagePanel MicMeter2;
	Button TestMicrophoneButton;
	Label MicrophoneSliderLabel;
	Slider MicrophoneVolume;
	Label ReceiveSliderLabel;
	CvarSlider ReceiveVolume;
	CvarToggleCheckButton VoiceEnableCheckButton;

	int MicVolumeValue;
	bool MicBoostSelected;
	float fReceiveVolume;
	int ReceiveSliderValue;
	bool VoiceOn;

	public OptionsSubVoice(Panel? parent, ReadOnlySpan<char> name) : base(parent, name) {
		// VoiceTweak = engine.GetVoiceTweakAPI();

		MicMeter = new(this, "MicMeter");
		MicMeter2 = new(this, "MicMeter2");

		ReceiveSliderLabel = new(this, "ReceiveLabel", "#GameUI_VoiceReceiveVolume");
		ReceiveVolume = new(this, "VoiceReceive", "#GameUI_ReceiveVolume", 0.0f, 1.0f, "voice_scale");

		MicrophoneSliderLabel = new(this, "MicrophoneLabel", "#GameUI_VoiceTransmitVolume");
		MicrophoneVolume = new(this, "#GameUI_MicrophoneVolume");
		MicrophoneVolume.SetRange(0, 100);
		MicrophoneVolume.AddActionSignalTarget(this);

		VoiceEnableCheckButton = new CvarToggleCheckButton(this, "voice_modenable", "#GameUI_EnableVoice", "voice_modenable");

		MicBoost = new CheckButton(this, "MicBoost", "#GameUI_BoostMicrophone");
		MicBoost.AddActionSignalTarget(this);

		TestMicrophoneButton = new Button(this, "TestMicrophone", "#GameUI_TestMicrophone");

		LoadControlSettings("resource/OptionsSubVoice.res");

		VoiceOn = false;
		MicMeter2.SetVisible(false);

		if (VoiceTweak == null) {
			ReceiveVolume.SetEnabled(false);
			MicrophoneVolume.SetEnabled(false);
			VoiceEnableCheckButton.SetEnabled(false);
			MicBoost.SetEnabled(false);
			TestMicrophoneButton.SetEnabled(false);
		}
		else
			OnResetData();
	}

	// FIXME #37
	public override void Dispose() {
		base.Dispose();
		// if (VoiceOn)
		// EndTestMicrophone();
	}

	public override void OnResetData() {
		if (VoiceTweak == null)
			return;

		float micVolume = VoiceTweak.GetControlFloat(VoiceTweakControl.MicrophoneVolume);
		MicrophoneVolume.SetValue((int)(100.0f * micVolume));
		MicVolumeValue = MicrophoneVolume.GetValue();

		float micBoost = VoiceTweak.GetControlFloat(VoiceTweakControl.MicBoost);
		MicBoost.SetSelected(micBoost != 0.0f);
		MicBoostSelected = MicBoost.IsSelected();

		ReceiveVolume.Reset();
		fReceiveVolume = ReceiveVolume.GetSliderValue();

		VoiceEnableCheckButton.Reset();
	}

	public void OnSlierMoved(int position) {
		if (VoiceTweak != null) {
			if (MicrophoneVolume.GetValue() != MicVolumeValue)
				PostActionSignal(new KeyValues("ApplyButtonEnable"));
		}
	}

	public void OnCheckButtonChecked() {
		if (VoiceTweak != null) {
			if (MicBoost.IsSelected() != MicBoostSelected)
				PostActionSignal(new KeyValues("ApplyButtonEnable"));
		}
	}

	public override void OnApplyChanges() {
		if (VoiceTweak == null)
			return;

		MicVolumeValue = MicrophoneVolume.GetValue();
		float micVolume = MicVolumeValue / 100.0f;
		VoiceTweak.SetControlFloat(VoiceTweakControl.MicrophoneVolume, micVolume);

		MicBoostSelected = MicBoost.IsSelected();
		VoiceTweak.SetControlFloat(VoiceTweakControl.MicBoost, MicBoostSelected ? 1.0f : 0.0f);

		ReceiveVolume.ApplyChanges();
		fReceiveVolume = ReceiveVolume.GetSliderValue();

		VoiceEnableCheckButton.ApplyChanges();
	}

	private void StartTestMicrophone() {
		if (VoiceTweak == null || VoiceOn)
			return;

	}

	private void UseCurrentVoiceParameters() {

	}

	private void ResetVoiceParameters() {

	}

	private void EndTestMicrophone() {
		if (VoiceTweak == null || !VoiceOn)
			return;

	}

	public override void OnCommand(ReadOnlySpan<char> command) {
		if (command.Equals("TestMicrophone", StringComparison.Ordinal)) {
			if (!VoiceOn)
				StartTestMicrophone();
			else
				EndTestMicrophone();
		}
		else if (command.Equals("SteamVoiceSettings", StringComparison.Ordinal))
			Steamworks.SteamFriends.ActivateGameOverlay("VoiceSettings");
		else
			base.OnCommand(command);
	}

	public override void OnPageHide() {
		if (VoiceOn)
			EndTestMicrophone();
		base.OnPageHide();
	}

	private void OnControlModified() => PostActionSignal(new KeyValues("ApplyButtonEnable"));

	private const int BAR_WIDTH = 160;
	private const int BAR_INCREMENT = 8;
	public override void OnThink() {
		base.OnThink();

		if (VoiceOn) {

		}
	}
}