using Source.Common.ServerBrowser;
using Source.GUI.Controls;

using Steamworks;

namespace Game.ServerBrowser;

class FriendsGames : BaseGamesPage
{
	public FriendsGames(Panel parent) : base(parent, "FriendsGames", PageType.FriendsServer) => ServerRefreshCount = 0;

	public override bool SupportsItem(IGameList.InterfaceItem item) => item == IGameList.InterfaceItem.Filters;

	public override void RefreshComplete(HServerListRequest req, EMatchMakingServerResponse response) {
		SetRefreshing(false);
		GameList.SortList();
		GameList.SetEmptyListText("#ServerBrowser_NoFriendsServersFound");
		base.RefreshComplete(req, response);
	}

	public override void OnOpenContextMenu(int itemID) {
		int serverId = GetSelectedServerID();
		if (serverId == -1)
			return;

		ServerContextMenu menu = ServerBrowserDialog.Instance!.GetContextMenu(GetActiveList());
		menu.ShowMenu(this, serverId, true, true, true, true);
	}
}