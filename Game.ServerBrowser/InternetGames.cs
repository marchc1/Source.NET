using Source.Common.Formats.Keyvalues;
using Source.Common.ServerBrowser;
using Source.Common.Utilities;
using Source.GUI.Controls;

using Steamworks;

namespace Game.ServerBrowser;

// [PanelAlias("CInternetGames")]
class InternetGames : BaseGamesPage
{
	struct Region
	{
		public UtlSymbol Name;
		public char code;
	}
	List<Region> Regions = [];

	TimeUnit_t LastSort;
	bool Dirty;
	bool RequireUpdate;
	bool AnyServersRetreivedFromMaster;
	bool AnyServersRespondedToQuery;
	bool NoServersListedOnMaster;
	bool OfflineMode;

	public InternetGames(Panel parent, ReadOnlySpan<char> panelName = "InternetGames", PageType type = PageType.InternetServer) : base(parent, panelName, type) {
		LastSort = 0;
		Dirty = false;
		RequireUpdate = true;
		OfflineMode = false;

		AnyServersRetreivedFromMaster = false;
		NoServersListedOnMaster = false;
		AnyServersRespondedToQuery = false;

		LocationFilter.RemoveAll();

		KeyValues kv = new("Regions");
		if (kv.LoadFromFile(fileSystem, "servers/Regions.cdf", null)) {
			for (KeyValues? sub = kv.GetFirstSubKey(); sub != null; sub = sub.GetNextKey()) {
				Region region = new() {
					Name = sub.GetString("name", ""),
					code = (char)sub.GetInt("code", 0)
				};

				KeyValues regionKV = new("region", "code", region.code);
				LocationFilter.AddItem(region.Name.ToString(), regionKV);
				Regions.Add(region);
			}
		}
		else
			Assert("Could not load file servesr/Regions.cdf; server brrowser will not function.");

		LoadFilterSettings();

		VGui.AddTickSignal(this, 250);
	}

	public override void PerformLayout() {
		if (!OfflineMode && RequireUpdate && ServerBrowserDialog.Instance!.IsVisible()) {
			RequireUpdate = false;
			PostMessage(this, new KeyValues("GetNewServerList"), 0.01f);//static kv
		}

		if (OfflineMode) {

		}

		base.PerformLayout();
		LocationFilter.SetEnabled(true);
	}

	public override void OnPageShow() {
		if (GameList.GetItemCount() == 0 && ServerBrowserDialog.Instance!.IsVisible())
			base.OnPageShow();
	}

	public override void OnTick() {
		if (OfflineMode) {
			base.OnTick();
			return;
		}

		base.OnTick();

		CheckRedoSort();
	}


	private ReadOnlySpan<char> GetStringNoUnfilteredServers() => "#ServerBrowser_NoInternetGames";
	private ReadOnlySpan<char> GetStringNoUnfilteredServersOnMaster() => "#ServerBrowser_MasterServerHasNoServersListed";
	private ReadOnlySpan<char> GetStringNoServersResponded() => "#ServerBrowser_NoInternetGamesResponded";

	public override void ServerResponded(HServerListRequest req, int server) {
		Dirty = true;
		base.ServerResponded(req, server);
		AnyServersRespondedToQuery = true;
		AnyServersRetreivedFromMaster = true;
	}

	public override void ServerFailedToRespond(HServerListRequest req, int server) {
		Dirty = true;
		gameserveritem_t? serverItem = SteamMatchmakingServers.GetServerDetails(req, server);
		Assert(serverItem != null);

		if (serverItem.m_bHadSuccessfulResponse)
			ServerResponded(req, server);
		else if (Servers.TryGetValue(server, out ServerDisplay display))
			RemoveServer(display);

		ServerRefreshCount++;
	}

	public override void RefreshComplete(HServerListRequest req, EMatchMakingServerResponse response) {
		SetRefreshing(false);
		UpdateFilterSettings();

		if (response != EMatchMakingServerResponse.eServerFailedToRespond) {
			if (AnyServersRespondedToQuery)
				GameList.SetEmptyListText(GetStringNoUnfilteredServers());
			else if (response == EMatchMakingServerResponse.eNoServersListedOnMasterServer)
				GameList.SetEmptyListText(GetStringNoUnfilteredServersOnMaster());
			else
				GameList.SetEmptyListText(GetStringNoServersResponded());
		}
		else
			GameList.SetEmptyListText("#ServerBrowser_MasterServerNotResponsive");

		Dirty = false;
		// LastSort
		if (IsVisible())
			GameList.SortList();

		UpdateStatus();

		base.RefreshComplete(req, response);
	}

	public override void GetNewServerList() {
		base.GetNewServerList();
		UpdateStatus();

		RequireUpdate = false;
		AnyServersRetreivedFromMaster = false;
		AnyServersRespondedToQuery = false;

		GameList.RemoveAll();
	}

	public override bool SupportsItem(IGameList.InterfaceItem item) => item == IGameList.InterfaceItem.Filters || item == IGameList.InterfaceItem.GetNewList;

	void CheckRedoSort() { }

	public override void OnOpenContextMenu(int itemID) { }

	void OnRefreshServer(int serverID) { }

	int GetRegionCodeToFilter() {
		throw new NotImplementedException();
	}

	public override bool CheckTagFilter(gameserveritem_t server) {
		throw new NotImplementedException();
	}
}