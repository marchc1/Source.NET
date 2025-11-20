using Source.GUI.Controls;

namespace Game.UI;

public class OptionsSubVideo : PropertyPage
{
	public OptionsSubVideo(Panel? parent, ReadOnlySpan<char> name) : base(parent, name) {
		LoadControlSettings("resource/OptionsSubVideo.res");
	}
}