using Source.GUI.Controls;

using Steamworks;

namespace Game.ServerBrowser;

static class ServerListCompare
{
	private static gameserveritem_t Server(uint id) => ServerBrowserDialog.Instance!.GetServer(id);

	public static int Password(ListPanel _, ListPanelItem p1, ListPanelItem p2) {
		gameserveritem_t s1 = Server(p1.UserData);
		gameserveritem_t s2 = Server(p2.UserData);

		if (s1 == null || s2 == null)
			return s1 == s2 ? 0 : (s1 == null ? -1 : 1);

		return s2.m_bPassword.CompareTo(s1.m_bPassword);
	}


	public static int Bots(ListPanel _, ListPanelItem p1, ListPanelItem p2) {
		gameserveritem_t s1 = Server(p1.UserData);
		gameserveritem_t s2 = Server(p2.UserData);

		if (s1 == null || s2 == null)
			return s1 == s2 ? 0 : (s1 == null ? -1 : 1);

		return s2.m_nBotPlayers.CompareTo(s1.m_nBotPlayers);
	}

	public static int Secure(ListPanel _, ListPanelItem p1, ListPanelItem p2) {
		gameserveritem_t s1 = Server(p1.UserData);
		gameserveritem_t s2 = Server(p2.UserData);

		if (s1 == null || s2 == null)
			return s1 == s2 ? 0 : (s1 == null ? -1 : 1);

		return s2.m_bSecure.CompareTo(s1.m_bSecure);
	}

	public static int IPAddress(ListPanel _, ListPanelItem p1, ListPanelItem p2) {
		gameserveritem_t s1 = Server(p1.UserData);
		gameserveritem_t s2 = Server(p2.UserData);

		if (s1 == null || s2 == null)
			return s1 == s2 ? 0 : (s1 == null ? -1 : 1);

		return s1.m_NetAdr.CompareTo(s2.m_NetAdr);
	}

	public static int Ping(ListPanel _, ListPanelItem p1, ListPanelItem p2) {
		gameserveritem_t s1 = Server(p1.UserData);
		gameserveritem_t s2 = Server(p2.UserData);

		if (s1 == null || s2 == null)
			return s1 == s2 ? 0 : (s1 == null ? -1 : 1);

		return s1.m_nPing.CompareTo(s2.m_nPing);
	}

	public static int Map(ListPanel _, ListPanelItem p1, ListPanelItem p2) {
		gameserveritem_t s1 = Server(p1.UserData);
		gameserveritem_t s2 = Server(p2.UserData);

		if (s1 == null || s2 == null)
			return s1 == s2 ? 0 : (s1 == null ? -1 : 1);

		return stricmp(s1.GetMap(), s2.GetMap());
	}

	public static int Game(ListPanel _, ListPanelItem p1, ListPanelItem p2) {
		gameserveritem_t s1 = Server(p1.UserData);
		gameserveritem_t s2 = Server(p2.UserData);

		if (s1 == null || s2 == null)
			return s1 == s2 ? 0 : (s1 == null ? -1 : 1);

		return stricmp(s1.GetGameDescription(), s2.GetGameDescription());
	}

	public static int ServerName(ListPanel _, ListPanelItem p1, ListPanelItem p2) {
		gameserveritem_t s1 = Server(p1.UserData);
		gameserveritem_t s2 = Server(p2.UserData);

		if (s1 == null || s2 == null)
			return s1 == s2 ? 0 : (s1 == null ? -1 : 1);

		return stricmp(s1.GetServerName(), s2.GetServerName());
	}

	public static int Players(ListPanel _, ListPanelItem p1, ListPanelItem p2) {
		gameserveritem_t s1 = Server(p1.UserData);
		gameserveritem_t s2 = Server(p2.UserData);

		if (s1 == null || s2 == null)
			return s1 == s2 ? 0 : (s1 == null ? -1 : 1);

		int s1p = Math.Max(0, s1.m_nPlayers - s1.m_nBotPlayers);
		int s1m = Math.Max(0, s1.m_nMaxPlayers - s1.m_nBotPlayers);
		int s2p = Math.Max(0, s2.m_nPlayers - s2.m_nBotPlayers);
		int s2m = Math.Max(0, s2.m_nMaxPlayers - s2.m_nBotPlayers);

		if (s1p > s2p)
			return -1;
		if (s1p < s2p)
			return 1;

		if (s1m > s2m)
			return -1;
		if (s1m < s2m)
			return 1;

		return 0;
	}

	public static int LastPlayed(ListPanel _, ListPanelItem p1, ListPanelItem p2) {
		gameserveritem_t s1 = Server(p1.UserData);
		gameserveritem_t s2 = Server(p2.UserData);

		if (s1 == null || s2 == null)
			return s1 == s2 ? 0 : (s1 == null ? -1 : 1);

		return s2.m_ulTimeLastPlayed.CompareTo(s1.m_ulTimeLastPlayed);
	}

	public static int Tags(ListPanel _, ListPanelItem p1, ListPanelItem p2) {
		gameserveritem_t s1 = Server(p1.UserData);
		gameserveritem_t s2 = Server(p2.UserData);

		if (s1 == null || s2 == null)
			return s1 == s2 ? 0 : (s1 == null ? -1 : 1);

		return stricmp(s1.GetGameTags(), s2.GetGameTags());
	}

	public static int Replay(ListPanel _, ListPanelItem p1, ListPanelItem p2) {
		throw new NotImplementedException();
	}
}

static class ServerListSort
{
	public static int Quality(in ServerQualitySort left, in ServerQualitySort right) {
		int iMaxP = BaseGamesPage.sb_mod_suggested_maxplayers.GetInt();
		if (iMaxP != 0 && left.MaxPlayerCount != right.MaxPlayerCount) {
			if (left.MaxPlayerCount > iMaxP)
				return 1;
			if (right.MaxPlayerCount > iMaxP)
				return -1;
		}

		if (left.Ping <= 100 && right.Ping <= 100 && left.PlayerCount != right.PlayerCount)
			return right.PlayerCount - left.PlayerCount;

		return left.Ping - right.Ping;
	}

	public static int MapNames(ServerMaps left, ServerMaps right) {
		if ((left.OnDisk && right.OnDisk) || (!left.OnDisk && !right.OnDisk))
			return stricmp(left.FriendlyName, right.FriendlyName);

		return right.OnDisk.CompareTo(left.OnDisk);
	}
}