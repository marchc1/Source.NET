using Source.Common.Formats.Keyvalues;
using Source.GUI.Controls;

namespace Game.ServerBrowser;

class ServerContextMenu : Menu
{
	public ServerContextMenu(Panel parent) : base(parent, "ServerContextMenu") { }

	public void ShowMenu(Panel target, int serverID, bool showConnect, bool showViewGameInfo, bool showRefresh, bool showAddToFavorites) {
		if (showConnect)
			AddMenuItem("ConnectToServer", "#ServerBrowser_ConnectToServer", new KeyValues("ConnectToServer", "serverID", serverID), target);

		if (showViewGameInfo)
			AddMenuItem("ViewGameInfo", "#ServerBrowser_ViewServerInfo", new KeyValues("ViewGameInfo", "serverID", serverID), target);

		if (showRefresh)
			AddMenuItem("RefreshServer", "#ServerBrowser_RefreshServer", new KeyValues("RefreshServer", "serverID", serverID), target);

		if (showAddToFavorites) {
			AddMenuItem("AddToFavorites", "#ServerBrowser_AddServerToFavorites", new KeyValues("AddToFavorites", "serverID", serverID), target);
			AddMenuItem("AddToBlacklist", "#ServerBrowser_AddServerToBlacklist", new KeyValues("AddToBlacklist", "serverID", serverID), target);
		}

		Input.GetCursorPos(out int x, out int y);
		GetPos(out int mx, out int my);
		SetPos(x - mx, y - my);
		SetVisible(true);
	}
}