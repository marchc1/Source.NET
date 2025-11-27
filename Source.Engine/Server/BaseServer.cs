using CommunityToolkit.HighPerformance;

using SDL;

using Source;
using Source.Common;
using Source.Common.Bitbuffers;
using Source.Common.Client;
using Source.Common.Commands;
using Source.Common.Engine;
using Source.Common.Hashing;
using Source.Common.Networking;
using Source.Common.Server;
using Source.Common.Utilities;
using Source.Engine;
using Source.Engine.Server;

namespace Source.Engine.Server;

public enum ServerState
{
	Dead,
	Loading,
	Active,
	Paused
}

/// <summary>
/// Base server, in SERVER
/// </summary>
public abstract class BaseServer : IServer
{

	protected readonly Net Net = Singleton<Net>();
	protected readonly Host Host = Singleton<Host>();
	internal static readonly ConVar sv_region = new( "sv_region","-1", FCvar.None, "The region of the world to report this server in." );
	internal static readonly ConVar sv_instancebaselines = new( "sv_instancebaselines", "1", FCvar.DevelopmentOnly, "Enable instanced baselines. Saves network overhead." );
	internal static readonly ConVar sv_stats = new( "sv_stats", "1", 0, "Collect CPU usage stats" );
	internal static readonly ConVar sv_enableoldqueries = new( "sv_enableoldqueries", "0", 0, "Enable support for old style (HL1) server queries" );
	internal static readonly ConVar sv_password = new("sv_password", "", FCvar.Notify | FCvar.Protected | FCvar.DontRecord, "Server password for entry into multiplayer games");

	public virtual int GetNumClients() {
		int count = 0;

		for (int i = 0; i < Clients.Count; i++)
			if (Clients[i].IsConnected())
				count++;

		return count;
	}
	public virtual int GetNumProxies() {
		int count = 0;

		for (int i = 0; i < Clients.Count; i++)
			if (Clients[i].IsConnected() && Clients[i].IsHLTV())
				count++;

		return count;
	}
	public virtual int GetNumFakeClients() {
		int count = 0;

		for (int i = 0; i < Clients.Count; i++)
			if (Clients[i].IsFakeClient())
				count++;

		return count;
	}
	public virtual int GetMaxClients() => MaxClients;  // returns current client limit
	public virtual int GetUDPPort() => Net.GetUDPPort(Socket);
	public virtual IClient? GetClient(int index) => Clients[index];  // returns interface to client 
	public virtual int GetClientCount() => Clients.Count; // for iteration;
	public virtual TimeUnit_t GetTime() {
		return TickCount * TickInterval;
	}
	public virtual long GetTick() => TickCount;
	public virtual TimeUnit_t GetTickInterval() => TickInterval;
	public virtual ReadOnlySpan<char> GetName() {
		return Host.host_name.GetString();
	}
	public virtual ReadOnlySpan<char> GetMapName() => ((ReadOnlySpan<char>)MapName).SliceNullTerminatedString();
	public virtual int GetSpawnCount() => SpawnCount;
	public virtual int GetNumClasses() => ServerClasses;
	public virtual int GetClassBits() => ServerClassBits;
	public virtual void GetNetStats(out double avgIn, out double avgOut) {
		avgIn = avgOut = 0.0f;

		for (int i = 0; i < Clients.Count; i++) {
			BaseClient cl = Clients[i];

			// Fake clients get killed in here.
			if (cl.IsFakeClient())
				continue;

			if (!cl.IsConnected())
				continue;

			NetChannel netchan = (NetChannel)cl.GetNetChannel()!;

			avgIn += netchan.GetAvgData(NetFlow.FLOW_INCOMING);
			avgOut += netchan.GetAvgData(NetFlow.FLOW_OUTGOING);
		}
	}
	public virtual int GetNumPlayers() {
		int count = 0;
		if (GetUserInfoTable() == null) {
			return 0;
		}

		int maxPlayers = GetUserInfoTable()!.GetNumStrings();

		for (int i = 0; i < maxPlayers; i++) {
			Span<PlayerInfo> pi = UserInfoTable!.GetStringUserData(i).AsSpan().Cast<byte, PlayerInfo>();
			if (pi.IsEmpty)
				continue;

			if (pi[0].FakePlayer)
				continue;

			count++;
		}

		return count;
	}
	public virtual bool GetPlayerInfo(int clientIndex, out PlayerInfo pinfo) {
		if (clientIndex < 0 || GetUserInfoTable() == null || clientIndex >= GetUserInfoTable()!.GetNumStrings()) {
			pinfo = default;
			return false;
		}

		Span<PlayerInfo> pi = UserInfoTable!.GetStringUserData(clientIndex).AsSpan().Cast<byte, PlayerInfo>();
		if (pi.IsEmpty) {
			pinfo = default;
			return false;
		}
		pinfo = pi[0];
		return true;
	}
	public virtual float GetCPUUsage() => CPUPercent;

	public virtual bool IsActive() => State >= ServerState.Active;
	public virtual bool IsLoading() => State == ServerState.Loading;
	public virtual bool IsDedicated() => Dedicated;
	public virtual bool IsPaused() => State == ServerState.Paused;
	public virtual bool IsMultiplayer() => MaxClients > 1;
	public virtual bool IsPausable() => false;

	public virtual bool IsHLTV() => false;
	public virtual bool IsReplay() => false;

	public virtual void BroadcastMessage(INetMessage msg, bool onlyActive = false, bool reliable = false) {
		for (int i = 0; i < Clients.Count; i++) {
			BaseClient cl = Clients[i];

			if ((onlyActive && !cl.IsActive()) || !cl.IsSpawned())
				continue;

			if (!cl.SendNetMsg(msg, reliable)) {
				if (msg.IsReliable() || reliable) {
					DevMsg($"BroadcastMessage: Reliable broadcast message overflow for client {cl.GetClientName()}");
				}
			}
		}
	}
	public virtual void BroadcastMessage(INetMessage msg, IRecipientFilter filter) {
		throw new NotImplementedException();
	}
	public virtual void BroadcastPrintf(ReadOnlySpan<char> msg) {
		throw new NotImplementedException();
	}

	public virtual ReadOnlySpan<char> GetPassword() {
		string password = sv_password.GetString();

		// if password is empty or "none", return NULL
		if (password.Length == 0 || password.Equals("none", StringComparison.OrdinalIgnoreCase))
			return null;

		return password;
	}

	public virtual void SetMaxClients(int number) {
		MaxClients = Math.Clamp(number, 1, Constants.ABSOLUTE_PLAYER_LIMIT);
	}
	public virtual void SetPaused(bool paused) {
		if (!IsPausable()) {
			return;
		}

		if (!IsActive())
			return;

		if (paused)
			State = ServerState.Paused;
		else
			State = ServerState.Active;
		// TODO: SEND THE NET MESSAGE!!!!!!!!!!!!!!!!!
	}
	public virtual void SetPassword(ReadOnlySpan<char> password) {
		if (!password.IsEmpty)
			password.ClampedCopyTo(Password);
		else
			Password[0] = '\0';
	}

	public virtual void DisconnectClient(IClient client, ReadOnlySpan<char> reason) => client.Disconnect(reason);

	public virtual void WriteDeltaEntities(BaseClient client, ClientFrame to, ClientFrame from, bf_write pBuf) {
		throw new NotImplementedException();
	}
	public virtual void WriteTempEntities(BaseClient client, FrameSnapshot to, FrameSnapshot from, bf_write pBuf, int nMaxEnts) {
		throw new NotImplementedException();
	}

	public virtual bool ProcessConnectionlessPacket(NetPacket packet) {
		throw new NotImplementedException();
	}

	public virtual void Init(bool isDedicated) {
		MaxClients = 0;
		SpawnCount = 0;
		UserID = 1;
		NumConnections = 0;
		Dedicated = isDedicated;
		Socket = NetSocketType.Server;

		Signon.DebugName = "m_Signon";

		// TODO: cvar.InstallGlobalChangeCallback(ServerNotifyVarChangeCallback);
		SetMasterServerRulesDirty();

		Clear();
	}
	public virtual void Clear() {
		if (StringTables != null) {
			StringTables.RemoveAllTables();
			StringTables = null;
		}

		InstanceBaselineTable = null;
		LightStyleTable = null;
		UserInfoTable = null;
		ServerStartupTable = null;

		State = ServerState.Dead;

		TickCount = 0;

		memreset((Span<char>)MapName);
		memreset((Span<char>)Skyname);
		memreset(ref WorldmapMD5);

		// Use a different limit on the signon buffer, so we can save some memory in SP (for xbox).
		if (IsMultiplayer() || IsDedicated())
			SignonBuffer.EnsureCapacity(Protocol.MAX_PAYLOAD);
		else
			SignonBuffer.EnsureCapacity(16384);

		Signon.StartWriting(SignonBuffer.Base(), SignonBuffer.Count(), 0);
		Signon.DebugName = "m_Signon";

		ServerClasses = 0;
		ServerClassBits = 0;

		LastRandomNonce = CurrentRandomNonce = 0;
		PausedTimeEnd = -1;
	}
	public virtual void Shutdown() {
		if (!IsActive())
			return;

		State = ServerState.Dead;

		// Only drop clients if we have not cleared out entity data prior to this.
		for (int i = Clients.Count - 1; i >= 0; i--) {
			BaseClient cl = Clients[i];
			if (cl.IsConnected())
				cl.Disconnect("Server shutting down");
			else {
				// free any memory do this out side here in case the reason the server is shutting down 
				// is because the listen server client typed disconnect, in which case we won't call
				// cl->DropClient, but the client might have some frame snapshot references left over, etc.
				cl.Clear();
			}

			cl.Dispose();

			Clients.RemoveAt(i);
		}

		// Let drop messages go out
		Sys.Sleep(100);

		// clear everything
		Clear();
	}
	public virtual BaseClient CreateFakeClient(ReadOnlySpan<char> name) {
		throw new NotImplementedException();
	}
	public virtual void RemoveClientFromGame(BaseClient cl) { }
	public virtual void SendClientMessages(bool bSendSnapshots) {

	}

	public virtual void FillServerInfo(svc_ServerInfo serverinfo) {

	}

	public virtual void UserInfoChanged(int nClientIndex) {

	}

	public bool GetClassBaseline(ServerClass pClass, out ReadOnlySpan<byte> pData) {
		throw new NotImplementedException();
	}
	public void RunFrame() {
		throw new NotImplementedException();
	}
	public void InactivateClients() {

	}
	public void ReconnectClients() {
		for (int i = 0; i < Clients.Count; i++) {
			BaseClient cl = Clients[i];

			if (cl.IsConnected()) {
				cl.SignOnState = SignOnState.Connected;
				NET_SignonState signon = new(cl.SignOnState, -1);
				cl.SendNetMsg(signon);
			}
		}
	}
	public void CheckTimeouts() {
		throw new NotImplementedException();
	}
	public void UpdateUserSettings() {
		throw new NotImplementedException();
	}
	public void SendPendingServerInfo() {
		throw new NotImplementedException();
	}

	public ReadOnlySpan<char> CompressPackedEntity(ServerClass pServerClass, ReadOnlySpan<byte> data, out int bits) {
		throw new NotImplementedException();
	}
	public ReadOnlySpan<char> UncompressPackedEntity(PackedEntity pPackedEntity, out int size) {
		throw new NotImplementedException();
	}

	public INetworkStringTable? GetInstanceBaselineTable() {
		throw new NotImplementedException();
	}
	public INetworkStringTable? GetLightStyleTable() {
		throw new NotImplementedException();
	}
	public INetworkStringTable? GetUserInfoTable() {
		throw new NotImplementedException();
	}

	public virtual void RejectConnection(NetAddress adr, int clientChallenge, ReadOnlySpan<char> s) {
		throw new NotImplementedException();
	}

	public TimeUnit_t GetFinalTickTime() {
		throw new NotImplementedException();
	}

	public virtual bool CheckIPRestrictions(NetAddress adr, int nAuthProtocol) {
		throw new NotImplementedException();
	}

	public void SetMasterServerRulesDirty() {
		throw new NotImplementedException();
	}
	public void SendQueryPortToClient(NetAddress adr) {
		throw new NotImplementedException();
	}

	public void RecalculateTags() {
		throw new NotImplementedException();
	}
	public void AddTag(ReadOnlySpan<char> pszTag) {
		throw new NotImplementedException();
	}
	public void RemoveTag(ReadOnlySpan<char> pszTag) {
		throw new NotImplementedException();
	}

	public int GetNumConnections() => NumConnections;

	public void SetReportNewFakeClients(bool bReportNewFakeClients) { ReportNewFakeClients = bReportNewFakeClients; }

	public void SetPausedForced(bool bPaused, TimeUnit_t flDuration = -1) {
		throw new NotImplementedException();
	}

	protected virtual IClient ConnectClient(NetAddress adr, int protocol, int challenge, int clientChallenge, int authProtocol,
							ReadOnlySpan<char> name, ReadOnlySpan<char> password, ReadOnlySpan<char> hashedCDkey, int cdKeyLen) {
		throw new NotImplementedException();
	}

	protected virtual BaseClient GetFreeClient(NetAddress adr) {
		throw new NotImplementedException();
	}

	protected virtual BaseClient CreateNewClient(int i) { AssertMsg(false, "BaseServer.CreateNewClient() being called - must be implemented in derived class!"); return null!; } // must be derived


	protected virtual bool FinishCertificateCheck(NetAddress adr, int a, ReadOnlySpan<char> b, int c) { return true; }

	protected virtual int GetChallengeNr(NetAddress adr) {
		throw new NotImplementedException();
	}
	protected virtual int GetChallengeType(NetAddress adr) {
		throw new NotImplementedException();
	}

	protected virtual bool CheckProtocol(NetAddress adr, int nProtocol, int clientChallenge) {
		throw new NotImplementedException();
	}
	protected virtual bool CheckChallengeNr(NetAddress adr, int nChallengeValue) {
		throw new NotImplementedException();
	}
	protected virtual bool CheckChallengeType(BaseClient client, int nNewUserID, NetAddress adr, int nAuthProtocol, ReadOnlySpan<char> pchLogonCookie, int cbCookie, int clientChallenge) {
		throw new NotImplementedException();
	}
	protected virtual bool CheckPassword(NetAddress adr, ReadOnlySpan<char> password, ReadOnlySpan<char> name) {
		throw new NotImplementedException();
	}
	protected virtual bool CheckIPConnectionReuse(NetAddress adr) {
		throw new NotImplementedException();
	}

	protected virtual void ReplyChallenge(NetAddress adr, int clientChallenge) {
		throw new NotImplementedException();
	}
	protected virtual void ReplyServerChallenge(NetAddress adr) {
		throw new NotImplementedException();
	}

	protected virtual void CalculateCPUUsage() {
		throw new NotImplementedException();
	}

	// Keep the master server data updated.
	protected virtual bool ShouldUpdateMasterServer() {
		throw new NotImplementedException();
	}

	protected void CheckMasterServerRequestRestart() {
		throw new NotImplementedException();
	}
	protected void UpdateMasterServer() {
		throw new NotImplementedException();
	}
	protected void UpdateMasterServerRules() {
		throw new NotImplementedException();
	}
	protected virtual void UpdateMasterServerPlayers() { }
	protected void ForwardPacketsFromMasterServerUpdater() {
		throw new NotImplementedException();
	}

	protected void SetRestartOnLevelChange(bool state) { RestartOnLevelChange = state; }

	protected bool RequireValidChallenge(NetAddress adr) {
		throw new NotImplementedException();
	}
	protected bool ValidChallenge(NetAddress adr, int challengeNr) {
		throw new NotImplementedException();
	}
	protected bool ValidInfoChallenge(NetAddress adr, ReadOnlySpan<char> nugget) {
		throw new NotImplementedException();
	}


	public ServerState State;     // some actions are only valid during load
	public NetSocketType Socket;       // network socket 
	public long TickCount;   // current server tick
	public bool SimulatingTicks;        // whether or not the server is currently simulating ticks
	public InlineArray64<char> MapName;       // map name
	public InlineArray64<char> MapFilename;   // map filename, may bear no resemblance to map name
	public InlineArray64<char> Skyname;       // skybox name
	public InlineArray32<char> Password;        // server password

	public MD5Value WorldmapMD5;     // For detecting that client has a hacked local copy of map, the client will be dropped if this occurs.

	public NetworkStringTableContainer? StringTables;   // newtork string table container

	public INetworkStringTable? InstanceBaselineTable;
	public INetworkStringTable? LightStyleTable;
	public INetworkStringTable? UserInfoTable;
	public INetworkStringTable? ServerStartupTable;
	public INetworkStringTable? DownloadableFileTable;

	// This will get set to NET_MAX_PAYLOAD if the server is MP.
	public readonly bf_write Signon = new();
	public readonly UtlMemory<byte> SignonBuffer = new();

	public int ServerClasses;      // number of unique server classes
	public int ServerClassBits; // log2 of serverclasses

	// Gets the next user ID mod SHRT_MAX and unique (not used by any active clients).
	private int GetNextUserID() {
		for (int i = 0; i < Clients.Count + 1; i++) {
			int nTestID = (UserID + i + 1) % short.MaxValue;

			// Make sure no client has this user ID.		
			int iClient;
			for (iClient = 0; iClient < Clients.Count; iClient++) {
				if (Clients[iClient].GetUserID() == nTestID)
					break;
			}

			// Ok, no client has this ID, so return it.		
			if (iClient == Clients.Count)
				return nTestID;
		}

		AssertMsg(false, "GetNextUserID: can't find a unique ID.");
		return UserID + 1;
	}
	private int UserID;          // increases by one with every new client


	protected int MaxClients;         // Current max #
	protected int SpawnCount;          // Number of servers spawned since start,
									   // used to check late spawns (e.g., when d/l'ing lots of
									   // data)
	protected TimeUnit_t TickInterval;     // time for 1 tick in seconds


	protected readonly List<BaseClient> Clients = [];     // array of up to [maxclients] client slots.

	protected bool Dedicated;

	protected uint CurrentRandomNonce;
	protected uint LastRandomNonce;
	protected TimeUnit_t LastRandomNumberGenerationTime;
	protected float CPUPercent;
	protected TimeUnit_t StartTime;
	protected TimeUnit_t LastCPUCheckTime;

	// This is only used for Steam's master server updater to refer to this server uniquely.
	protected bool RestartOnLevelChange;

	protected bool MasterServerRulesDirty;
	protected TimeUnit_t LastMasterServerUpdateTime;

	protected int NumConnections;      //Number of successful client connections.

	protected bool ReportNewFakeClients; // Whether or not newly created fake clients should be included in server browser totals
	protected TimeUnit_t PausedTimeEnd;
}
