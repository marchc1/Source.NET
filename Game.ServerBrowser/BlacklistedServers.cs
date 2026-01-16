using Source.Common.Commands;
using Source.Common.GUI;
using Source.GUI.Controls;

using Steamworks;

namespace Game.ServerBrowser;

class BlacklistedServers : PropertyPage
{
	static ConVar sb_showblacklists = new("sb_showblacklists", "0", FCvar.None, "If set to 1, blacklist rules will be printed to the console as they're applied.");
	Button AddServerButton;
	Button AddCurrentServer;
	ListPanel GameList;
	FileOpenDialog ImportDialog;
	TimeUnit_t BlackListTimestamp;

	public BlacklistedServers(Panel parent) : base(parent, "BlacklistedGames") {

	}

	public void LoadBlacklistedList() { }

	bool AddServersFromFile(ReadOnlySpan<char> filename, bool resetTimes) {
		throw new NotImplementedException();
	}

	void SaveBlacklistedList() { }

	void AddServer(gameserveritem_t server) { }

	// BlacklistedServer GetBlacklistedServer(int serverID) { }

	bool IsServerBlacklisted(gameserveritem_t server) {
		throw new NotImplementedException();
	}

	// void UpdateBlacklistUI(BlacklistedServer blackServer) { }

	public override void ApplySchemeSettings(IScheme cheme) { }

	public override void OnPageShow() { }

	int GetSelectedServerID() {
		throw new NotImplementedException();
	}

	void OnOpenContextMenu(int itemID) { }

	void OnAddServerByName() { }

	void OnRemoveFromBlacklist() { }

	void ClearServerList() { }

	void OnAddCurrentServer() { }

	void OnImportBlacklist() { }

	void OnFileSelected(ReadOnlySpan<char> fullpath) { }

	public override void OnCommand(ReadOnlySpan<char> command) { }

	void OnConnectToGame() { }

	void OnDisconnectFromGame() { }
}