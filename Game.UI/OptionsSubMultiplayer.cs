using Source.GUI.Controls;

namespace Game.UI;

public class OptionsSubMultiplayer : PropertyPage
{
	public OptionsSubMultiplayer(Panel? parent, ReadOnlySpan<char> name) : base(parent, name) {
		LoadControlSettings("resource/OptionsSubMultiplayer.res");
	}
}