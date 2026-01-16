using Source.Common.Input;
using Source.GUI.Controls;

using Steamworks;

namespace Game.ServerBrowser;

class DialogGameInfo : Frame/*, ISteamMatchmakingPlayersResponse, ISteamMatchmakingPingResponse*/
{
	const int PING_TIMES_MAX = 4;

	Button ConnectButton;
	Button CloseButton;
	Button RefreshButton;
	Label InfoLabel;
	ToggleButton AutoRetry;
	RadioButton AutoRetryAlert;
	RadioButton AutoRetryJoin;
	ListPanel PlayerList;

	bool Connecting;
	string Password;
	bool ServerNotResponding;
	bool ServerFull;
	bool ShowAutoRetryToggle;
	bool ShowingExtendedOptions;
	UInt64 SteamIDFriend;
	string ConnectCode;
	gameserveritem_t Server;
	HServerQuery PingQuery;
	HServerQuery PlayersQuery;
	bool PlayerListUpdatePending;

	public DialogGameInfo(Panel parent, int serverIP, int queryPort, uint connectionPort, ReadOnlySpan<char> connectCode) : base(parent, "DialogGameInfo") {

	}

	void SendPlayerQuery(UInt32 ip, UInt16 queryPort) { }

	void Run(ReadOnlySpan<char> titleName) { }

	void ChangeGame(int serverIP, int queryPort, ushort connectionPort) { }

	void OnPersonaStateChange(PersonaStateChange_t personaStateChange) { }

	void SetFriend(UInt64 steamIDFriend) { }

	UInt64 GetAssociatedFriend() {
		throw new NotImplementedException();
	}

	public override void PerformLayout() { }

	public override void OnKeyCodePressed(ButtonCode code) { }

	void Connect() { }

	void OnConnect() { }

	void OnConnectToGame(int ip, int port) { }

	void OnRefresh() { }

	void OnButtonToggled(Panel panel) { }

	void ShowAutoRetryOptions(bool state) { }

	void RequestInfo() { }

	public override void OnTick() { }

	void ServerResponded(gameserveritem_t server) { }

	void ServerFailedToRespond() { }

	void ApplyConnectCommand(gameserveritem_t server) { }

	void ConstructConnectArgs(char pchOptions, int cchOptions, gameserveritem_t server) { }

	void ConnectToServer() { }

	void RefreshComplete(EMatchMakingServerResponse response) { }

	void OnJoinServerWithPassword(ReadOnlySpan<char> password) { }

	void ClearPlayerList() { }

	void AddPlayerToList(ReadOnlySpan<char> playerName, int score, TimeUnit_t timePlayedSeconds) { }

	int PlayerTimeColumnSortFunc(ListPanel panel, ListPanelItem p1, ListPanelItem p2) {
		throw new NotImplementedException();
	}
}
