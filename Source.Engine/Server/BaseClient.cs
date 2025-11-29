using CommunityToolkit.HighPerformance;

using Source.Common;
using Source.Common.Formats.Keyvalues;
using Source.Common.Networking;
using Source.Common.Server;

using Steamworks;

using System.Runtime.CompilerServices;

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
		Name[0] = '\0';
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

	public InlineArrayMaxPlayerNameLength<char> Name;
	public InlineArray33<byte> GUID;

	public CSteamID SteamID;
	public uint FriendsID;
	public string FriendsName;

	KeyValues? ConVars;
	bool InitialConVarsSet;
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
	public int BaselineUpdateTick;
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
	public TimeUnit_t SnapshotInterval;

	public bool IsFullyAuthenticated() => FullyAuthenticated;
	public void SetFullyAuthenticated() => FullyAuthenticated = true;
	public void SetPlayerNameLocked(bool bValue) { PlayerNameLocked = bValue; }
	public bool IsPlayerNameLocked() => PlayerNameLocked;

	public void FireGameEvent(IGameEvent ev) {
		throw new NotImplementedException();
	}
	static bool BIgnoreCharInName(char cChar, bool bIsFirstCharacter) {
		return cChar == '%' || cChar == '~' || cChar < 0x09 || (bIsFirstCharacter && cChar == '#');
	}
	public void SetName(ReadOnlySpan<char> playerName) {
		Span<char> name = stackalloc char[Constants.MAX_PLAYER_NAME_LENGTH];
		strcpy(name, playerName);

		PendingNameChange[0] = '\0';

		ValidateName(name);
		if (strcmp(name, Name) == 0)
			return; 

		int i;
		int dupc = 1;
		ReadOnlySpan<char> p, val;

		Span<char> newname = stackalloc char[Constants.MAX_PLAYER_NAME_LENGTH];

		ReadOnlySpan<char> pFrom = name;
		Span<char> pTo = Name;

		while (!pFrom.IsEmpty && pTo.Length > 0) {
			if (!BIgnoreCharInName(pFrom[0], Unsafe.AreSame(ref pTo.DangerousGetReference(), ref ((ReadOnlySpan<char>)Name).DangerousGetReference()))) {
				pTo[0] = pFrom[0];
				pTo = pTo[1..];
			}

			pFrom = pFrom[1..];
		}
		pTo[0] = '\0';

		Assert(Name[0] != '\0');
		if (Name[0] == '\0')
			strcpy(Name, "unnamed");

		val = Name;

		while (true) {
			for (i = 0; i < Server.GetClientCount(); i++) {
				IClient client = Server.GetClient(i)!;

				if (!client.IsConnected() || client == this)
					continue;

				if (stricmp(client.GetClientName(), val) == 0 && !(IsFakeClient() && client.IsFakeClient())) {
					BaseClient? pClient = (BaseClient?)client;
					if (IsFakeClient() && pClient != null) {
						pClient.Name[0] = '\0';
						pClient.SetName(val);
					}
					else {
						break;
					}
				}
			}

			if (i >= Server.GetClientCount())
				break;

			p = val;

			if (val[0] == '(') {
				if (val[2] == ')') 
					p = val[3..];
				else if (val[3] == ')') 
					p = val[4..];
			}

			sprintf(newname, "(%d)%s").D(dupc++).S(p);
			strcpy(Name, newname);

			val = Name;
		}

		ConVars!.SetString("name", Name);
		ConVarsChanged = true;

		Server.UserInfoChanged(ClientSlot);
	}

	static void ValidateName(Span<char> name) {
		if (name.IsEmpty) 
			sprintf(name, "unnamed");
		else {
			StrTools.RemoveAllEvilCharacters(name);

			ReadOnlySpan<char> pChar = name;
			while (!pChar.IsEmpty && pChar[0] != '\0' && (pChar[0] == ' ' || BIgnoreCharInName(pChar[0], true)))
				pChar = pChar[1..];

			if (pChar.IsEmpty || pChar[0] == '\0') 
				sprintf(name, "unnamed");
		}
	}

	public void Connect(ReadOnlySpan<char> name, int userID, INetChannel netChannel, bool fakePlayer, int clientChallenge) {
		Common.TimestampedLog("CBaseClient::Connect");
#if !SWDS
		EngineVGui().UpdateProgressBar(LevelLoadingProgress.SignOnConnect);
#endif
		Clear();

		ConVars = new KeyValues("userinfo");
		InitialConVarsSet = false;

		UserID = userID;

		SetName(name);
		TimeLastNameChange = 0.0;

		FakePlayer = fakePlayer;
		NetChannel = netChannel;

		if (NetChannel!= null && Server != null && Server.IsMultiplayer()) 
			NetChannel.SetCompressionMode(true);

		ClientChallenge = clientChallenge;

		SignOnState = SignOnState.Connected;

		if (fakePlayer) 
			Steam3Server().NotifyLocalClientConnect(this);
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

	public bool IsReplay() {
		return false;
	}

	public bool ShouldReportThisFakeClient() {
		return false;
	}

	internal void SetSteamID(in CSteamID steamID) {
		SteamID = steamID;
	}
}
