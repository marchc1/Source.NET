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
	public OptionsSubAudio(Panel? parent, ReadOnlySpan<char> name) : base(parent, name) {
		LoadControlSettings("resource/OptionsSubAudio.res");
	}
}