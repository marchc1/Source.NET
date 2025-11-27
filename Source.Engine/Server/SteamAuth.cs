#define USE_GS_AUTH_API
global using static Source.Engine.Server.Steam3ServerAccessor;

using Source.Common;
using Source.Common.Commands;
using Source.Common.Server;

using Steamworks;

using System;
using System.Diagnostics;

namespace Source.Engine.Server;

public enum ServerType
{
	Normal,
	TVRelay
}

[EngineComponent]
public static class Steam3ServerAccessor
{
	[Dependency] static Steam3Server _server = null!;
	public static Steam3Server Steam3Server() => _server;
}

[EngineComponent]
public class Steam3Server : IDisposable
{
	readonly ICommandLine CommandLine;
	public Steam3Server(ICommandLine commandLine) {
		CommandLine = commandLine;

		HasActivePlayers = false;
		LogOnResult = false;
		ServerMode = EServerMode.eServerModeInvalid;
		ServerType = ServerType.Normal;
		WantsSecure = false;
		Initialized = false;
		WantsPersistentAccountLogon = false;
		LogOnFinished = false;
		MasterServerUpdaterSharingGameSocket = false;
		steamIDLanOnly.InstancedSet((AccountID_t)0, 0, EUniverse.k_EUniversePublic, EAccountType.k_EAccountTypeInvalid);
		SteamIDGS.InstancedSet((AccountID_t)1, 0, EUniverse.k_EUniverseInvalid, EAccountType.k_EAccountTypeInvalid);
		QueryPort = 0;
	}

	EServerMode ServerMode;
	ServerType ServerType;

	bool MasterServerUpdaterSharingGameSocket;
	bool LogOnFinished;
	bool LoggedOn;
	bool LogOnResult;
	bool HasActivePlayers;
	CSteamID SteamIDGS;
	CSteamID steamIDLanOnly;
	bool Active;
	bool WantsSecure;
	bool Initialized;
	bool WantsPersistentAccountLogon;

	uint IP;
	ushort Port;
	ushort QueryPort;

	string? AccountToken;

	CSteamID SteamIDGroupForBlocking;

	public bool BSecure() => SteamGameServer.BSecure();
	public bool BIsActive() => ServerMode >= EServerMode.eServerModeNoAuthentication;
	public bool BLanOnly() => ServerMode == EServerMode.eServerModeNoAuthentication;
	public bool BWantsSecure() => ServerMode == EServerMode.eServerModeAuthenticationAndSecure;
	public bool BLoggedOn() => SteamGameServer.BLoggedOn();

	public SteamIPAddress_t GetPublicIP() {
		return SteamGameServer.GetPublicIP();
	}

	public EServerMode GetCurrentServerMode() {
		if (SV.sv_lan.GetBool())
			return EServerMode.eServerModeNoAuthentication;
		else if (CommandLine.FindParm("-insecure") != 0)
			return EServerMode.eServerModeAuthentication;
		else
			return EServerMode.eServerModeAuthenticationAndSecure;
	}

	public void Activate(ServerType serverType) {
		if (GetCurrentServerMode() == ServerMode && ServerType == serverType)
			return;

		if (BIsActive())
			Shutdown();

		IP = 0;
		Port = 26900;

		if (CommandLine.FindParm("-steamport") != 0)
			Port = (ushort)CommandLine.ParmValue("-steamport", 26900);

		ServerMode = GetCurrentServerMode();
		ServerType = serverType;

		switch (ServerMode) {
			case EServerMode.eServerModeNoAuthentication:
				Msg("Initializing Steam libraries for LAN server\n");
				break;
			case EServerMode.eServerModeAuthentication:
				Msg("Initializing Steam libraries for INSECURE Internet server.  Authentication and VAC not requested.\n");
				break;
			case EServerMode.eServerModeAuthenticationAndSecure:
				Msg("Initializing Steam libraries for secure Internet server\n");
				break;
			default:
				Warning($"Bogus ServerMode {ServerMode}!\n");
				AssertMsg(false, "Bogus server mode?!");
				break;
		}

		if (!Init()) {
			Assert(false);
			return;
		}
	}
	protected Callback<SteamServersConnected_t> m_CallbackSteamServersConnected = null!;
	protected Callback<SteamServerConnectFailure_t> m_CallbackSteamServersConnectFailure = null!;
	protected Callback<SteamServersDisconnected_t> m_CallbackSteamServersDisconnected = null!;
	protected Callback<GSPolicyResponse_t> m_CallbackPolicyResponse = null!;
	protected Callback<ValidateAuthTicketResponse_t> m_CallbackGSAuthTicketResponse = null!;
	protected Callback<P2PSessionRequest_t> m_CallbackP2PSessionRequest = null!;
	protected Callback<P2PSessionConnectFail_t> m_CallbackP2PSessionConnectFail = null!;
	public bool Init() {
		SteamAPI.Init();

		m_CallbackSteamServersConnected = Callback<SteamServersConnected_t>.CreateGameServer(OnLogonSuccess);
		m_CallbackSteamServersConnectFailure = Callback<SteamServerConnectFailure_t>.CreateGameServer(OnLogonFailure);
		m_CallbackSteamServersDisconnected = Callback<SteamServersDisconnected_t>.CreateGameServer(OnLoggedOff);
		m_CallbackPolicyResponse = Callback<GSPolicyResponse_t>.CreateGameServer(OnPolicyResponse);
		m_CallbackGSAuthTicketResponse = Callback<ValidateAuthTicketResponse_t>.CreateGameServer(OnValidateAuthTicketResponse);
		m_CallbackP2PSessionRequest = Callback<P2PSessionRequest_t>.CreateGameServer(OnP2PSessionRequest);
		m_CallbackP2PSessionConnectFail = Callback<P2PSessionConnectFail_t>.CreateGameServer(OnP2PSessionConnectFail);

		return true;
	}
	Filter? _filter = null; Filter Filter => _filter ??= Singleton<Filter>();
	SV? _SV = null; SV SV => _SV ??= Singleton<SV>();

	void OnLogonSuccess(SteamServersConnected_t logonSuccess) {
		if (!BIsActive())
			return;

		if (!LogOnResult)
			LogOnResult = true;

		if (!BLanOnly()) {
			Msg("Connection to Steam servers successful.\n");
			SteamIPAddress_t ip = SteamGameServer.GetPublicIP();
			Msg($"   Public IP is {ip.ToIPAddress()}\n");
		}

		if (SteamGameServer.GetSteamID().IsValid()) {
			SteamIDGS = SteamGameServer.GetSteamID();
			if (SteamIDGS.BAnonGameServerAccount())
				Msg($"Assigned anonymous gameserver Steam ID {SteamIDGS}.\n");
			else if (SteamIDGS.BPersistentGameServerAccount())
				Msg($"Assigned persistent gameserver Steam ID {SteamIDGS}.\n");
			else {
				Warning($"Assigned Steam ID {SteamIDGS}, which is of an unexpected type!\n");
				AssertMsg(false, "Unexpected steam ID type!");
			}

			if (CommandLine.FindParm("-p2p") != 0) {
				ConMsg("\n------------------------ ");
				ConColorMsg(new Color(249, 241, 165), "Steam P2P");
				ConMsg(" ------------------------\n");

				ConMsg("Run the following command in a client's console to connect:\n");
				ConMsg("    `connect p2p:0`\n"); // ToDo: Finish this!
				ConMsg("-----------------------------------------------------------\n");
			}
		}
		else
			SteamIDGS = CSteamID.NotInitYetGS;

		// send updated server details
		// OnLogonSuccess() gets called each time we logon, so if we get dropped this gets called
		// again and we get need to retell the AM our details
		SendUpdatedServerDetails();
	}
	void OnLogonFailure(SteamServerConnectFailure_t logonFailure) {
		if (!BIsActive())
			return;

		if (!LogOnResult) {
			if (logonFailure.m_eResult == EResult.k_EResultServiceUnavailable) {
				if (!BLanOnly())
					Msg("Connection to Steam servers successful (SU).\n");
			}
			else {
				if (!BLanOnly())
					Warning($"Could not establish connection to Steam servers.  (Result = {logonFailure.m_eResult})\n");
			}
		}

		LogOnResult = true;
	}
	void OnLoggedOff(SteamServersDisconnected_t pLoggedOff) {
		if (!BLanOnly()) {
			Warning($"Connection to Steam servers lost.  (Result = {pLoggedOff.m_eResult})\n");
		}
	}
	void OnPolicyResponse(GSPolicyResponse_t pPolicyResponse) {

	}

	static void sv_setsteamblockingcheck_f(IConVar pConVar, in ConVarChangeContext ctx) {
		if (SV.sv_lan.GetBool())
			Warning("Warning: sv_steamblockingcheck is not applicable in LAN mode.\n");
	}

	static readonly ConVar sv_steamblockingcheck = new("sv_steamblockingcheck", "0", 0, "Check each new player for Steam blocking compatibility, 1 = message only, 2 >= drop if any member of owning clan blocks,\n3 >= drop if any player has blocked, 4 >= drop if player has blocked anyone on server", callback: sv_setsteamblockingcheck_f);

	public BaseClient? ClientFindFromSteamID(in CSteamID steamIDFind) {
		for (int i = 0; i < sv.GetClientCount(); i++) {
			BaseClient cl = (BaseClient)sv.GetClient(i)!;

			if (!cl.IsConnected() || cl.IsFakeClient())
				continue;

			if (cl.GetNetworkID().IDType != Source.Common.IDType.Steam)
				continue;

			USERID id = cl.GetNetworkID();
			if (id.SteamID == steamIDFind)
				return cl;
		}
		return null;
	}

	void OnValidateAuthTicketResponse(ValidateAuthTicketResponse_t validateAuthTicketResponse) {
		if (!BIsActive())
			return;

		BaseClient? client = ClientFindFromSteamID(validateAuthTicketResponse.m_SteamID);
		if (client == null)
			return;

		if (validateAuthTicketResponse.m_eAuthSessionResponse != EAuthSessionResponse.k_EAuthSessionResponseOK) {
			OnValidateAuthTicketResponseHelper(client, validateAuthTicketResponse.m_eAuthSessionResponse);
			return;
		}

		if (Filter.IsUserBanned(client.GetNetworkID())) {
			sv.RejectConnection(client.GetNetChannel()!.GetRemoteAddress()!, client.GetClientChallenge(), "#GameUI_ServerRejectBanned");
			client.Disconnect($"STEAM UserID {client.GetNetworkIDString()} is banned");
		}
		else if (CheckForDuplicateSteamID(client))
			client.Disconnect($"STEAM UserID {client.GetNetworkIDString()} is already\nin use on this server");
		else {
			Span<char> msg = stackalloc char[512];
			sprintf(msg, "\"%s<%i><%s><>\" STEAM USERID validated\n").S(client.GetClientName()).I(client.GetUserID()).S(client.GetNetworkIDString());

			DevMsg(msg);
			SV.ServerGameClients.NetworkIDValidated(client.GetClientName(), client.GetNetworkIDString());
		}

		if (sv_steamblockingcheck.GetInt() >= 1)
			SteamGameServer.ComputeNewPlayerCompatibility(validateAuthTicketResponse.m_SteamID);

		client.SetFullyAuthenticated();
	}

	public bool CheckForDuplicateSteamID(BaseClient client) {
		if (BLanOnly())
			return false;

		for (int i = 0; i < sv.GetClientCount(); i++) {
			IClient cl = sv.GetClient(i)!;

			if (!cl.IsConnected() || cl.IsFakeClient())
				continue;

			if (cl.GetNetworkID().IDType != IDType.Steam)
				continue;

			if (client == cl)
				continue;

			if (!CompareUserID(client.GetNetworkID(), cl.GetNetworkID()))
				continue;

			return true;
		}

		return false;
	}

	private void OnValidateAuthTicketResponseHelper(BaseClient client, EAuthSessionResponse m_eAuthSessionResponse) {
		throw new NotImplementedException();
	}

	void OnP2PSessionRequest(P2PSessionRequest_t pCallback) {

	}
	void OnP2PSessionConnectFail(P2PSessionConnectFail_t pCallback) {

	}
	public void SendUpdatedServerDetails() {
		if (!BIsActive())
			return;

		int nNumClients = sv.GetNumClients();
		int nMaxClients = sv.GetMaxClients();
		int nFakeClients = sv.GetNumFakeClients();

		for (int i = 0; i < sv.GetClientCount(); ++i) {
			BaseClient cl = (BaseClient)sv.GetClient(i)!;
			if (!cl.IsConnected())
				continue;

			bool hideClient = false;
			if (cl.IsReplay() || cl.IsHLTV()) {
				Assert(cl.IsFakeClient());
				hideClient = true;
			}

			if (cl.IsFakeClient() && !cl.ShouldReportThisFakeClient())
				hideClient = true;

			if (hideClient) {
				--nNumClients;
				--nMaxClients;
				--nFakeClients;

				if (cl.SteamID.IsValid()) {
					Assert(cl.SteamID.BAnonGameServerAccount());
					SteamGameServer.SendUserDisconnect_DEPRECATED(cl.SteamID);
					cl.SteamID = new();
				}
			}
		}

		// sv_visiblemaxplayers todo

		SteamGameServer.SetMaxPlayerCount(nMaxClients);
		SteamGameServer.SetBotPlayerCount(nFakeClients);
		SteamGameServer.SetPasswordProtected(!sv.GetPassword().IsEmpty);
		SteamGameServer.SetRegion(BaseServer.sv_region.GetString());
		SteamGameServer.SetServerName(new(sv.GetName()));
		SteamGameServer.SetMapName(new(sv.GetMapName()));

		SteamGameServer.SetSpectatorPort(0);

		// UpdateGroupSteamID(false);
	}

	public void Shutdown() {
		if (!BIsActive())
			return;

		HasActivePlayers = false;
		LogOnResult = false;
		SteamIDGS = CSteamID.NotInitYetGS;
		ServerMode = EServerMode.eServerModeInvalid;

		Clear();
	}
	public void Clear() {

	}
	public void Dispose() {
		Shutdown();
		GC.SuppressFinalize(this);
	}

	internal bool CompareUserID(USERID id1, USERID id2) {
		if (id1.IDType != id2.IDType)
			return false;

		switch (id1.IDType) {
			case Source.Common.IDType.Steam:
			case Source.Common.IDType.Valve:
				return (id1.SteamID == id2.SteamID);
			default:
				break;
		}

		return false;
	}

	internal void NotifyClientDisconnect(BaseClient baseClient) {
		throw new NotImplementedException();
	}

	public void RunFrame() {

	}
}
