using Source.GUI.Controls;

namespace Game.UI;

enum SoundQuality
{
	Low,
	Medium,
	High
}

public class OptionsSubAudio : PropertyPage
{
	ComboBox SpeakerSetupCombo;
	ComboBox SoundQualityCombo;
	CCvarSlider SFXSlider;
	CCvarSlider MusicSlider;
	ComboBox CloseCaptionCombo;
	bool RequireRestart;
	ComboBox SpokenLanguageCombo;
	// ELanguage CurrentAudioLanguage;
	// char[] UpdatedAudioLanguage;
	CCvarToggleCheckButton SoundMuteLoseFocusCheckButton;

	public OptionsSubAudio(Panel? parent, ReadOnlySpan<char> name) : base(parent, name) {
		SFXSlider = new(this, "SFXSlider", "#GameUI_SoundEffectVolume", 0.0f, 1.0f, "volume");
		MusicSlider = new(this, "MusicSlider", "#GameUI_MusicVolume", 0.0f, 1.0f, "Snd_MusicVolume");

		CloseCaptionCombo = new(this, "CloseCaptionCheck", 6, false);
		CloseCaptionCombo.AddItem("#GameUI_NoClosedCaptions", null);
		CloseCaptionCombo.AddItem("#GameUI_SubtitlesAndSoundEffects", null);
		CloseCaptionCombo.AddItem("#GameUI_Subtitles", null);

		SoundQualityCombo = new(this, "SoundQuality", 6, false);
		SoundQualityCombo.AddItem("#GameUI_High", null);
		SoundQualityCombo.AddItem("#GameUI_Medium", null);
		SoundQualityCombo.AddItem("#GameUI_Low", null);

		SpeakerSetupCombo = new(this, "SpeakerSetup", 6, false);
#if !POSIX
		SpeakerSetupCombo.AddItem("#GameUI_Headphones", null);
#endif
		SpeakerSetupCombo.AddItem("#GameUI_2Speakers", null);
#if !POSIX
		SpeakerSetupCombo.AddItem("#GameUI_4Speakers", null);
		SpeakerSetupCombo.AddItem("#GameUI_5Speakers", null);
		SpeakerSetupCombo.AddItem("#GameUI_7Speakers", null);
#endif
		SpokenLanguageCombo = new(this, "AudioSpokenLanguage", 6, false);

		SoundMuteLoseFocusCheckButton = new(this, "snd_mute_losefocus", "#GameUI_SndMuteLoseFocus", "snd_mute_losefocus");

		LoadControlSettings("resource/OptionsSubAudio.res");
	}
}