using Source.Common.Commands;
using Source.GUI.Controls;

class TxViewPanel : Frame
{
	Button Refresh;
	ListViewPanel View;
	public TxViewPanel(Panel parent) : base(parent, "TxViewPanel") {
		Refresh = new(this, "Refresh", "Refresh");
		View = new(this, "Textures");

		LoadControlSettings("resource/TxViewPanel.res");

		SetVisible(false);
		SetSizeable(true);
		SetMoveable(true);
	}

	public static TxViewPanel? g_TxViewPanel;
	public static void Install(Panel parent) {
		if (g_TxViewPanel != null)
			return;

		g_TxViewPanel = new(parent);
		Assert(g_TxViewPanel != null);
	}

#if DEBUG
	[ConCommand("txview", "Show/hide the internal texture viewer", FCvar.DontRecord)]
	static private void TxView_f() {
		if (g_TxViewPanel == null)
			return;

		if (g_TxViewPanel.IsVisible())
			g_TxViewPanel.Close();
		else
			g_TxViewPanel.Activate();
	}
#endif
}