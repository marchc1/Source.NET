using Source.Common.ServerBrowser;
using Source.GUI.Controls;

using Steamworks;

namespace Game.ServerBrowser;

class HistoryGames : BaseGamesPage
{
	bool RefreshOnListReload;
	public HistoryGames(Panel parent) : base(parent, "HistoryGames", PageType.HistoryServer) {
		RefreshOnListReload = false;
		GameList.AddColumnHeader(10, "LastPlayed", "#ServerBrowser_LastPlayed", 100);
		GameList.SetSortFunc(0, ServerListCompare.LastPlayed);
		GameList.SetSortColumn(10);
	}

	public void LoadHistoryList() {
		GameList.SetEmptyListText("#ServerBrowser_NoServersPlayed");

		if (RefreshOnListReload) {
			RefreshOnListReload = false;
			StartRefresh();
		}
	}

	public override bool SupportsItem(IGameList.InterfaceItem item) => item == IGameList.InterfaceItem.Filters;

	public override void RefreshComplete(HServerListRequest req, EMatchMakingServerResponse response) {
		SetRefreshing(false);
		GameList.SetEmptyListText("#ServerBrowser_NoServersPlayed");
		GameList.SortList();
		base.RefreshComplete(req, response);
	}

	public override void OnOpenContextMenu(int itemID) { }

	void OnRemoveFromHistory() { }
}