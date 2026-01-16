using Source.Common.ServerBrowser;
using Source.GUI.Controls;

using Steamworks;

namespace Game.ServerBrowser;

class FavoriteGames : BaseGamesPage
{
	bool RefreshListOnReload;

	public FavoriteGames(Panel parent) : base(parent, "FavoriteGames", PageType.FavoritesServer) => RefreshListOnReload = false;

	public void LoadFavoritesList() {
		if (SteamMatchmaking.GetFavoriteGameCount() == 0)
			GameList.SetEmptyListText("#ServerBrowser_NoFavoriteServers");
		else
			GameList.SetEmptyListText("#ServerBrowser_NoInternetGamesResponded");

		if (RefreshListOnReload) {
			RefreshListOnReload = false;
			StartRefresh();
		}
	}

	public override bool SupportsItem(IGameList.InterfaceItem item) {
		if (item == IGameList.InterfaceItem.Filters || item == IGameList.InterfaceItem.AddServer)
			return true;

		if (item == IGameList.InterfaceItem.AddCurrentServer)
			return FiltersVisible;

		return false;
	}

	public override void RefreshComplete(HServerListRequest req, EMatchMakingServerResponse response) {
		SetRefreshing(false);

		if (SteamMatchmaking.GetFavoriteGameCount() == 0)
			GameList.SetEmptyListText("#ServerBrowser_NoFavoriteServers");
		else
			GameList.SetEmptyListText("#ServerBrowser_NoInternetGamesResponded");
		GameList.SortList();

		base.RefreshComplete(req, response);
	}

	public override void OnOpenContextMenu(int itemID) { }

	void OnRemoveFromFavorites() { }

	void OnAddServerByName() { }

	void OnAddCurrentServer() { }

	public override void OnCommand(ReadOnlySpan<char> command) { }

	void OnConnectToGame() { }

	void OnDisconnectFromGame() { }
}