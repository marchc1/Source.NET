using Source.Common.Formats.Keyvalues;
using Source.Common.GUI;
using Source.Common.Input;
using Source.GUI.Controls;

namespace Game.ServerBrowser;

class MouseMessageForwardingPanel : Panel
{
	public MouseMessageForwardingPanel(Panel parent, ReadOnlySpan<char> panelName) : base(parent, panelName) {
		SetPaintEnabled(false);
		SetPaintBackgroundEnabled(false);
		SetPaintBorderEnabled(false);
	}

	public override void PerformLayout() {
		GetParent()!.GetSize(out int w, out int t);
		SetBounds(0, 0, w, t);
	}

	public override void OnMousePressed(ButtonCode code) => GetParent()?.OnMousePressed(code);
	public override void OnMouseDoublePressed(ButtonCode code) => GetParent()?.OnMouseDoublePressed(code);
	public override void OnMouseWheeled(int delta) => GetParent()?.OnMouseWheeled(delta);
}

class QuickListPanel : EditablePanel
{
	string MapName; //todo: make this an InlineArray128
	ImagePanel LatencyImage;
	Label LatencyLabel;
	Label PlayerCountLabel;
	Label OtherServersLabel;
	Label ServerNameLabel;
	Panel BGroundPanel;
	ImagePanel MapImage;
	Panel ListPanelParent;
	Label GameTypeLabel;
	Label MapNameLabel;
	ImagePanel ReplayImage;
	int ListID;

	public QuickListPanel(Panel parent, ReadOnlySpan<char> panelName) : base(parent, panelName) {
		SetParent(parent);
		ListPanelParent = parent;

		MouseMessageForwardingPanel panel = new(this, null);
		panel.SetZPos(3);

		LatencyImage = new(this, "latencyimage");
		PlayerCountLabel = new(this, "playercount", "");
		OtherServersLabel = new(this, "otherservercount", "");
		ServerNameLabel = new(this, "servername", "");
		BGroundPanel = new(this, "background");
		MapImage = new(this, "mapimage");
		GameTypeLabel = new(this, "gametype", "");
		MapNameLabel = new(this, "mapname", "");
		LatencyLabel = new(this, "latencytext", "");
		ReplayImage = new(this, "replayimage");

		ReadOnlySpan<char> pathID = "PLATFORM";
		if (fileSystem.FileExists("servers/QuickListPanel.res", "MOD"))
			pathID = "MOD";

		LoadControlSettings("servers/QuickListPanel.res", pathID);
	}

	public override void ApplySchemeSettings(IScheme scheme) {
		base.ApplySchemeSettings(scheme);

		if (scheme != null && BGroundPanel != null)
			BGroundPanel.SetBgColor(scheme.GetColor("QuickListBGDeselected", new(255, 255, 255, 0)));
	}

	public void SetRefreshing() { }

	public void SetMapName(ReadOnlySpan<char> mapName) {
		MapName = new(mapName);

		if (MapNameLabel != null) {
			MapNameLabel.SetText(MapName);
			MapNameLabel.SizeToContents();
		}
	}

	public void SetGameType(ReadOnlySpan<char> gameType) {
		if (strlen(gameType) == 0) {
			GameTypeLabel.SetVisible(false);
			return;
		}

		Span<char> buf = stackalloc char[512];
		sprintf(buf, "(%s)").S(gameType);
		GameTypeLabel.SetText(buf);
	}

	void SetServerInfo(KeyValues kv, int listID, int totalServers) { }

	public void SetImage(ReadOnlySpan<char> mapName) { }

	public override void OnMousePressed(ButtonCode code) { }

	public override void OnMouseDoublePressed(ButtonCode code) { }

	public int GetListID() => ListID;
}