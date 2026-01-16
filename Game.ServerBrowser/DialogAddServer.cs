using Source.Common.GUI;
using Source.Common.ServerBrowser;
using Source.GUI.Controls;

using Steamworks;

namespace Game.ServerBrowser;

class DialogAddServer : Frame/*, ISteamMatchmakingPingResponse*/
{
	public DialogAddServer(Panel parent, IGameList gameList) : base(parent, "DialogAddServer") {

	}

	void OnTextChanged() { }

	public override void OnCommand(ReadOnlySpan<char> command) { }

	void OnOK() { }

	void TestServers() { }

	void ServerResponded(gameserveritem_t server) { }

	void ServerFailedToRespond() { }

	public override void ApplySchemeSettings(IScheme scheme) { }

	void OnItemSelected() { }

	public virtual void FinishAddServer(gameserveritem_t server) { }
}

class DialogAddBlacklistedServer : DialogAddServer
{
	public DialogAddBlacklistedServer(Panel parent, IGameList gameList) : base(parent, gameList) {

	}

	public override void FinishAddServer(gameserveritem_t server) { }

	public override void ApplySchemeSettings(IScheme pScheme) { }
}