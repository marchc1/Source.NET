using Source.Common;
using Source.Common.Networking;
using Source.Common.Server;

using Steamworks;

namespace Source.Engine.Server;


public abstract class BaseClient : IGameEventListener2, IClient, IClientMessageHandler, IDisposable
{
	public int GetPlayerSlot() => ClientSlot;
	public int GetUserID() => UserID;
	// NetworkID?
	public ReadOnlySpan<char> GetClientName() => Name;
	public INetChannel GetNetChannel() => NetChannel;
	public void Dispose() {

	}

	public int GetClientChallenge() => ClientChallenge;

	static readonly char[] idstr = new char[Constants.MAX_NETWORKID_LENGTH];
	public ReadOnlySpan<char> GetUserIDString(USERID id) {
		idstr[0] = '\0';

		switch (id.IDType) {
			case IDType.Steam:
				if (Steam3Server().BLanOnly() && id.SteamID == CSteamID.Nil)
					strcpy(idstr, "STEAM_ID_LAN");
				else if (id.SteamID == CSteamID.Nil)
					strcpy(idstr, "STEAM_ID_PENDING");
				else
					strcpy(idstr, id.SteamID.Render());
				break;
			case IDType.HLTV: strcpy(idstr, "HLTV"); break;
			case IDType.Replay: strcpy(idstr, "REPLAY"); break;
			default: strcpy(idstr, "UNKNOWN"); break;
		}

		return idstr;
	}

	public USERID GetNetworkID() {
		USERID userID;

		userID.SteamID = SteamID;
		userID.IDType = IDType.Steam;

		return userID;
	}

	public ReadOnlySpan<char> GetNetworkIDString() {
		if (IsFakeClient())
			return "BOT";

		return GetUserIDString(GetNetworkID());
	}

	public void Clear() {
		if (NetChannel != null) {
			NetChannel.Shutdown("Disconnect by server.\n");
			NetChannel = null!;
		}

		//  if (ConVars) 
		//  	ConVars = NULL;
		
		FreeBaselines();

		// This used to be a memset, but memset will screw up any embedded classes
		// and we want to preserve some things like index.
		SignOnState = SignOnState.None;
		DeltaTick = -1;
		SignOnTick = 0;
		StringTableAckTick = 0;
		// LastSnapshot = NULL;
		ForceWaitForTick = -1;
		FakePlayer = false;
		HLTV = false;
		NextMessageTime = 0;
		SnapshotInterval = 0;
		ReceivedPacket = false;
		UserID = 0;
		Name = "";
		FriendsID = 0;
		FriendsName = "";
		SendTableCRC = 0;
		BaselineUpdateTick = -1;
		BaselineUsed = 0;
		FilesDownloaded = 0;
		ConVarsChanged = false;
		SendServerInfo = false;
		FullyAuthenticated = false;
		TimeLastNameChange = 0.0;
		PendingNameChange[0] = '\0';
		memreset(CustomFiles);
	}

	private void FreeBaselines() {

	}

	public bool IsConnected() => SignOnState >= SignOnState.Connected;
	public void Disconnect(ReadOnlySpan<char> str) {
		if (SignOnState == SignOnState.None)
			return;

		Steam3Server().NotifyClientDisconnect(this);
		SignOnState = SignOnState.None;

		Server.UserInfoChanged(ClientSlot);
		ConMsg($"Dropped {GetClientName()} from server ({str})\n");

		g_GameEventManager.RemoveListener(this);

		if (NetChannel != null) {
			NetChannel.Shutdown(str);
			NetChannel = null!;
		}

		Clear(); 
		DedicatedServerUI.NotifyDedicatedServerUI("UpdatePlayers");
		Steam3Server().SendUpdatedServerDetails(); 
	}

	public bool IsActive() => SignOnState == SignOnState.Full;
	public bool IsSpawned() => SignOnState >= SignOnState.New;
	public bool IsFakeClient() => FakePlayer;
	public bool IsHLTV() => HLTV;

	public bool SendNetMsg(INetMessage msg, bool forceReliable = false) {
		if (NetChannel == null)
			return true;

		int nStartBit = NetChannel.GetNumBitsWritten(msg.IsReliable() || forceReliable);
		bool bret = NetChannel.SendNetMsg(msg, forceReliable);
		return bret;
	}

	public int ClientSlot;
	public int EntityIndex;
	public int UserID;

	public string Name;
	public string GUID;

	public CSteamID SteamID;
	public uint FriendsID;
	public string FriendsName;

	// convars...
	public bool ConVarsChanged;
	public bool SendServerInfo;
	public BaseServer Server;
	public bool HLTV;
	public bool Replay;
	public int ClientChallenge;

	public uint SendTableCRC;

	public CustomFile[] CustomFiles;
	public int FilesDownloaded;

	public INetChannel NetChannel;
	public SignOnState SignOnState;
	public int DeltaTick;
	public int StringTableAckTick;
	public int SignOnTick;
	// CSmartPtr<CFrameSnapshot, CRefCountAccessorLongName>
	// CFrameSnapshot baseline
	int BaselineUpdateTick;
	MaxEdictsBitVec BaselinesSent;
	public int BaselineUsed;

	public int ForceWaitForTick;
	public bool FakePlayer;     // JAC: This client is a fake player controlled by the game DLL
	public bool ReportFakeClient; // Should this fake client be reported 
	public bool ReceivedPacket; // true, if client received a packet after the last send packet

	public bool FullyAuthenticated;
	public TimeUnit_t TimeLastNameChange;
	public bool PlayerNameLocked;
	public InlineArrayMaxPlayerNameLength<char> PendingNameChange;
	public TimeUnit_t NextMessageTime;
	TimeUnit_t SnapshotInterval;

	public bool IsFullyAuthenticated() => FullyAuthenticated;
	public void SetFullyAuthenticated() => FullyAuthenticated = true;
	public void SetPlayerNameLocked(bool bValue) { PlayerNameLocked = bValue; }
	public bool IsPlayerNameLocked() => PlayerNameLocked;

	public void FireGameEvent(IGameEvent ev) {
		throw new NotImplementedException();
	}

	public void Connect(ReadOnlySpan<char> name, int userID, INetChannel netChannel, bool fakePlayer, int clientChallenge) {
		throw new NotImplementedException();
	}

	public void Inactivate() {
		throw new NotImplementedException();
	}

	public void Reconnect() {
		throw new NotImplementedException();
	}

	public IServer? GetServer() {
		throw new NotImplementedException();
	}

	public ReadOnlySpan<char> GetUserSetting(ReadOnlySpan<char> cvar) {
		throw new NotImplementedException();
	}

	public void SetRate(int nRate, bool bForce) {
		throw new NotImplementedException();
	}

	public int GetRate() {
		throw new NotImplementedException();
	}

	public void SetUpdateRate(int nUpdateRate, bool bForce) {
		throw new NotImplementedException();
	}

	public int GetUpdateRate() {
		throw new NotImplementedException();
	}

	public int GetMaxAckTickCount() {
		throw new NotImplementedException();
	}

	public bool ExecuteStringCommand(ReadOnlySpan<char> s) {
		throw new NotImplementedException();
	}

	public void ClientPrintf(ReadOnlySpan<char> fmt) {
		throw new NotImplementedException();
	}

	public bool IsHearingClient(int index) {
		throw new NotImplementedException();
	}

	public bool IsProximityHearingClient(int index) {
		throw new NotImplementedException();
	}

	public void SetMaxRoutablePayloadSize(int nMaxRoutablePayloadSize) {
		throw new NotImplementedException();
	}
}
