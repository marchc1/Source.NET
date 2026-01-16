using Source.GUI.Controls;

using Steamworks;

namespace Game.ServerBrowser;

class SpectateGames : InternetGames
{
	public static Panel Create_SpectateGames() => new SpectateGames(null);

	public SpectateGames(Panel parent) : base(parent, "SpectateGames", PageType.SpectatorServer) { }

	public override void GetNewServerList() {
		ServerFilters.Add(new() { m_szKey = "proxy", m_szValue = "1" });
		base.GetNewServerList();
	}

	public override void OnPageShow() { }
	public override bool CheckTagFilter(gameserveritem_t server) => true;
}