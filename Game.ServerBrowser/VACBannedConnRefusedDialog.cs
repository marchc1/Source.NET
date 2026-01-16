using Source.GUI.Controls;

namespace Game.ServerBrowser;

class VACBannedConnRefusedDialog : Frame
{
	public VACBannedConnRefusedDialog(Panel parent, ReadOnlySpan<char> name) : base(null, name) {
		SetParent(parent);
		SetSize(480, 220);
		SetSizeable(false);

		LoadControlSettings("servers/VACBannedConnRefusedDialog.res");
		MoveToCenterOfScreen();
	}
}