using Source.GUI.Controls;

namespace Game.UI;

public class OptionsSubVoice : PropertyPage
{
	public OptionsSubVoice(Panel? parent, ReadOnlySpan<char> name) : base(parent, name) {
		LoadControlSettings("resource/OptionsSubVoice.res");
	}
}