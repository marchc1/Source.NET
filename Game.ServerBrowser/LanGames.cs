using Source.Common.ServerBrowser;
using Source.GUI.Controls;

using Steamworks;

namespace Game.ServerBrowser;

class LanGames : BaseGamesPage
{
	bool Requesting;
	double RequestTime;
	bool AutoRefresh;

	public LanGames(Panel parent, bool autoRefresh = true, string? customResFilename = null) : base(parent, "LanGames", PageType.LANServer, customResFilename) {
		ServerRefreshCount = 0;
		AutoRefresh = autoRefresh;
		Requesting = false;
	}

	public override void OnPageShow() {
		if (AutoRefresh)
			StartRefresh();
	}

	public override void OnTick() {
		base.OnTick();
		CheckRetryRequest();
	}

	public override bool SupportsItem(IGameList.InterfaceItem item) => item == IGameList.InterfaceItem.Filters;

	public override void StartRefresh() {
		base.StartRefresh();
		// RequestTime
	}

	void ManualShowButtons(bool showConnect, bool showRefreshAll, bool showFilter) { }

	public override void StopRefresh() {
		base.StopRefresh();
		Requesting = false;
	}

	void CheckRetryRequest() {
		if (!Requesting)
			return;

	}

	public override void ServerFailedToRespond(HServerListRequest req, int server) {

	}

	public override void RefreshComplete(HServerListRequest req, EMatchMakingServerResponse response) {
		SetRefreshing(false);
		GameList.SortList();
		ServerRefreshCount = 0;
		GameList.SetEmptyListText("#ServerBrowser_NoLanServers");
		SetEmptyListText();

		base.RefreshComplete(req, response);
	}

	void SetEmptyListText() => GameList.SetEmptyListText("#ServerBrowser_NoLanServers");

	public override void OnOpenContextMenu(int row) { }
}