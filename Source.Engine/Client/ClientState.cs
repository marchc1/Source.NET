using Source.Common.Commands;
using Source.Common.Engine;
using Source.Common.Filesystem;
using Source.Common.Hashing;
using Source.Common.Mathematics;
using Source.Common.Networking;
using Source.Common.Networking.DataTable;
using Source.Common.Server;
using Source.Engine.DataTable;
using Steamworks;

using static Source.Constants;

using GameServer = Source.Engine.Server.GameServer;

namespace Source.Engine.Client;

/// <summary>
/// Client state, in CLIENT. Often referred to by 'cl'
/// </summary>
public class ClientState : BaseClientState
{
	public readonly Host Host;
	readonly IFileSystem fileSystem;
	readonly Net Net;
	readonly CommonHostState host_state;
	readonly CL CL;
	readonly IEngineVGuiInternal? EngineVGui;
	readonly IHostState HostState;
	readonly Scr Scr;


	public double LastServerTickTime;
	public bool InSimulation;

	public int OldTickCount;
	public double TickRemainder;
	public double FrameTime;

	public int LastOutgoingCommand;
	public int ChokedCommands;
	public int LastCommandAck;
	public int CommandAck;
	public int SoundSequence;

	public bool IsHLTV;
	// public bool IsReplay; // RaphaelIT7: Gmod has replay completely removed iirc

	public MD5Value ServerMD5;

	public byte[] AreaBits = new byte[MAX_AREA_STATE_BYTES];
	public byte[] AreaPortalBits = new byte[MAX_AREA_PORTAL_STATE_BYTES];
	public bool AreaBitsValid;

	public QAngle ViewAngles;
	List<AddAngle> AddAngle = new();
	public float AddAngleTotal;
	public float PrevAddAngleTotal;
	public CustomFile[] CustomFiles = new CustomFile[MAX_CUSTOM_FILES];
	public uint FriendsID;
	public string? FriendsName;

	public bool UpdateSteamResources;
	public bool ShownSteamResourceUpdateProgress;
	public bool DownloadResources;
	public bool PrepareClientDLL;
	public bool CheckCRCsWithServer;
	public double LastCRCBatchTime;
	public bool MarkedCRCsUnverified;

	public INetworkStringTable? ModelPrecacheTable;
	public INetworkStringTable? GenericPrecacheTable;
	public INetworkStringTable? SoundPrecacheTable;
	public INetworkStringTable? DecalPrecacheTable;
	public INetworkStringTable? InstanceBaselineTable;
	public INetworkStringTable? LightStyleTable;
	public INetworkStringTable? UserInfoTable;
	public INetworkStringTable? ServerStartupTable;
	public INetworkStringTable? DynamicModelsTable;

	PrecacheItem[] ModelPrecache = new PrecacheItem[PrecacheItem.MAX_MODELS];
	PrecacheItem[] GenericPrecache = new PrecacheItem[PrecacheItem.MAX_GENERIC];
	PrecacheItem[] SoundPrecache = new PrecacheItem[PrecacheItem.MAX_SOUNDS];
	PrecacheItem[] DecalPrecache = new PrecacheItem[PrecacheItem.MAX_BASE_DECAL];

	public static ConVar cl_flushentitypacket = new("0", FCvar.Cheat, "For debugging. Force the engine to flush an entity packet.");

	public static ConVar cl_timeout = new("30", FCvar.Archive, "After this many seconds without receiving a packet from the server, the client will disconnect itself");
	public static ConVar cl_allowdownload = new("1", FCvar.Archive, "Client downloads customization files");
	public static ConVar cl_downloadfilter = new("all", FCvar.Archive, "Determines which files can be downloaded from the server (all, none, nosounds, mapsonly)");

	public ClientState(Host Host, IFileSystem fileSystem, Net Net, CommonHostState host_state, GameServer sv,
		Cbuf Cbuf, Cmd Cmd, ICvar cvar, CL CL, IEngineVGuiInternal? EngineVGui, IHostState HostState, Scr Scr, IEngineAPI engineAPI)
		: base(Host, fileSystem, Net, sv, Cbuf, cvar, EngineVGui, engineAPI) {
		this.Host = Host;
		this.fileSystem = fileSystem;
		this.Net = Net;
		this.host_state = host_state;
		this.CL = CL;
		this.Scr = Scr;
		this.ClockDriftMgr = new(this, Host, host_state);
		this.EngineVGui = EngineVGui;
		this.HostState = HostState;
	}

	public override void Clear()
	{
		base.Clear();

		ModelPrecacheTable = null;
		GenericPrecacheTable = null;
		SoundPrecacheTable = null;
		DecalPrecacheTable = null;
		InstanceBaselineTable = null;
		LightStyleTable = null;
		UserInfoTable = null;
		ServerStartupTable = null;
		DynamicModelsTable = null;

		Array.Clear(AreaBits, 0, AreaBits.Length);
		UpdateSteamResources = false;
		ShownSteamResourceUpdateProgress = false;
		DownloadResources = false;
		PrepareClientDLL = false;

		// DeleteClientFrames(-1);
		ViewAngles.Init();
		LastServerTickTime = 0.0;
		OldTickCount = 0;
		InSimulation = false;

		AddAngle.Clear();
		AddAngleTotal = 0.0f;
		PrevAddAngleTotal = 0.0f;

		Array.Clear(ModelPrecache, 0, ModelPrecache.Length);
		Array.Clear(SoundPrecache, 0, SoundPrecache.Length);
		Array.Clear(DecalPrecache, 0, DecalPrecache.Length);
		Array.Clear(GenericPrecache, 0, GenericPrecache.Length);

		IsHLTV = false;

		if (ServerMD5.Bits != null) // RaphaelIT7: Yes... We can be called so early that the other's constructor's weren't called yet.
			Array.Clear(ServerMD5.Bits, 0, ServerMD5.Bits.Length);

		LastCommandAck = 0;
		CommandAck = 0;
		SoundSequence = 0;
		
		if (SignOnState > SignOnState.Connected)
		{
			SignOnState = SignOnState.Connected;
		}
	}

	public bool ProcessConnectionlessPacket(in NetPacket packet) {
		return false;
	}

	public override void Disconnect(string? reason, bool showMainMenu) {
		base.Disconnect(reason, showMainMenu);

		// CL_ClearState
		Clear(); // RaphaelIT7: Works for now though we should implement CL_ClearState at a later point
	}
	public override void FullConnect(NetAddress adr) {
		base.FullConnect(adr);

		LastOutgoingCommand = -1;
		ChokedCommands = 0;
	}

	public override int GetClientTickCount() => ClockDriftMgr.ClientTick;
	public override void SetClientTickCount(int tick) => ClockDriftMgr.ClientTick = tick;
	public override int GetServerTickCount() => ClockDriftMgr.ServerTick;
	public override void SetServerTickCount(int tick) => ClockDriftMgr.ServerTick = tick;

	public override bool SetSignonState(SignOnState state, int count) {
		if (!base.SetSignonState(state, count)) {
			CL.Retry();
			return false;
		}

		ServerCount = count;

		switch (SignOnState) {
			case SignOnState.Challenge:
				EngineVGui?.UpdateProgressBar(LevelLoadingProgress.SignOnChallenge);
				MarkedCRCsUnverified = false;
				break;
			case SignOnState.Connected:
				EngineVGui?.UpdateProgressBar(LevelLoadingProgress.SignOnConnected);
				Scr.BeginLoadingPlaque();

				NetChannel!.Clear();

				NetChannel.SetTimeout(NetChannel.SIGNON_TIME_OUT);
				NetChannel.SetMaxBufferSize(true, Protocol.MAX_PAYLOAD);

				var convars = new NET_SetConVar();
				Host.BuildConVarUpdateMessage(convars, FCvar.UserInfo, false);
				NetChannel.SendNetMsg(convars);
				break;
			case SignOnState.New:
				EngineVGui?.UpdateProgressBar(LevelLoadingProgress.SignOnNew);
				StartUpdatingSteamResources();
				return true; // Don't tell the server yet we're at this point
			case SignOnState.PreSpawn:
				break;
			case SignOnState.Spawn:
				EngineVGui?.UpdateProgressBar(LevelLoadingProgress.SignOnSpawn);

				break;
			case SignOnState.Full:
				CL.FullyConnected();
				if(NetChannel != null) {
					NetChannel.SetTimeout(cl_timeout.GetDouble());
					NetChannel.SetMaxBufferSize(true, Protocol.MAX_DATAGRAM_PAYLOAD);
				}

				HostState.OnClientConnected();
				break;
			case SignOnState.ChangeLevel:
				NetChannel!.SetTimeout(NetChannel.SIGNON_TIME_OUT);
				if (MaxClients > 1)
					EngineVGui?.EnabledProgressBarForNextLoad();
				Scr.BeginLoadingPlaque();
				if(MaxClients > 1)
					EngineVGui?.UpdateProgressBar(LevelLoadingProgress.ChangeLevel);
				break;
		}

		if (state >= SignOnState.Connected && NetChannel != null) {
			var msg = new NET_SignonState(state, ServerCount);
			NetChannel.SendNetMsg(msg);

			// TODO: where to *actually* send this? Next tick?
			if (SignOnState == SignOnState.PreSpawn) {
				CLC_ListenEvents msg2 = new CLC_ListenEvents();
				NetChannel.SendNetMsg(msg2);

				NET_SetConVar cvr = new NET_SetConVar();
				cvr.AddCVar("cl_playermodel", "combineelite");
				cvr.AddCVar("cl_playercolor", "1.000000 1.000000 1.000000");
				NetChannel.SendNetMsg(cvr);
			}
		}

		return true;
	}

	public override void PacketStart(int incomingSequence, int outgoingAcknowledged) {
		base.PacketStart(incomingSequence, outgoingAcknowledged);
	}

	public override void PacketEnd() {
		base.PacketEnd();
	}

	public override void FileReceived(string fileName, uint transferID) {
		throw new NotImplementedException();
	}
	public override void FileRequested(string fileName, uint transferID) {
		throw new NotImplementedException();
	}
	public override void FileDenied(string fileName, uint transferID) {
		throw new NotImplementedException();
	}
	public override void FileSent(string fileName, uint transferID) {
		throw new NotImplementedException();
	}
	public override void ConnectionCrashed(string reason) {
		throw new NotImplementedException();
	}
	public void StartUpdatingSteamResources() {
		// for now; just make signon state new
		FinishSignonState_New();
	}
	public void CheckUpdatingSteamResources() { }
	public void CheckFileCRCsWithServer() { }
	public void SendClientInfo() {
		CLC_ClientInfo info = new CLC_ClientInfo();
		info.SendTableCRC = CLC.ClientInfoCRC;
		info.ServerCount = ServerCount;
		info.IsHLTV = false;
		info.FriendsID = long.Parse(steamid.GetString());
		info.FriendsName = "";

		// check stuff later

		NetChannel!.SendNetMsg(info);
	}

	public void FinishSignonState_New() {
		if (SignOnState != SignOnState.New)
			return;

		EngineVGui?.UpdateProgressBar(LevelLoadingProgress.SendClientInfo);

		if (NetChannel == null)
			return;

		SendClientInfo();
		var msg1 = new CLC_GMod_ClientToServer();
		NetChannel.SendNetMsg(msg1);
		var msg = new NET_SignonState(SignOnState, ServerCount);
		NetChannel.SendNetMsg(msg);
	}

	public override void RunFrame() => base.RunFrame();

	public double GetTime() {
		int tickCount = GetClientTickCount();
		double tickTime = tickCount * host_state.IntervalPerTick;
		if (InSimulation)
			return tickTime;

		return tickTime + TickRemainder;
	}
	public bool IsPaused() => Paused;
	public double GetPausedExpireTime() => PausedExpireTime;
	public double GetFrameTime() {
		if (!ClockDriftMgr.Enabled && InSimulation) {
			int elapsedTicks = (GetClientTickCount() - OldTickCount);
			return elapsedTicks * host_state.IntervalPerTick;
		}

		return IsPaused() ? 0 : FrameTime;
	}
	public void SetFrameTime(double dt) => FrameTime = dt;
	public double GetClientInterpAmount() => throw new NotImplementedException();
	public override void Connect(string adr, string sourceTag) {
		Socket = Net.GetSocket(NetSocketType.Client);
		base.Connect(adr, sourceTag);
	}

	public void ClearSounds() // RaphaelIT7: This is used by Snd_Restart_f
	{
		Array.Clear(SoundPrecache,  0, SoundPrecache.Length);
	}

	protected override bool ProcessPacketEntities(svc_PacketEntities msg) {
		if ( !msg.IsDelta )
		{
			// we can start recording now that we've received an uncompressed packet
			// demorecorder->SetSignonState(SIGNONSTATE_FULL);

			/*
			// Tell prediction that we're recreating entities due to an uncompressed packet arriving
			if (g_pClientSidePrediction)
			{
				g_pClientSidePrediction->OnReceivedUncompressedPacket();
			}
			 */
		} else {
			if (DeltaTick == -1) {
				// we requested a full update but still got a delta compressed packet. ignore it.
				return true;
			}

			// Preprocessing primarily does client prediction. So if we're processing deltas--do it
			// otherwise, we're about to be told exactly what the state of everything is--so skip it.
			// CL_PreprocessEntities(); // setup client prediction
		}

		if (!EntsParse.ProcessPacketEntities(this, msg))
			return false;

		return base.ProcessPacketEntities( msg );
	}

	protected override bool ProcessClassInfo(svc_ClassInfo msg)
	{
		if (msg.CreateOnClient)
		{
			// if (!demoplayer.IsPlayingBack())
			{
				DTCommon.CreateClientTablesFromServerTables(Host.serverDLL.GetAllServerClasses());

				DTCommon.CreateClientClassInfosFromServerClasses(this, Host.serverDLL.GetAllServerClasses());

				// demorecorder->RecordServerClasses(serverGameDLL.GetAllServerClasses());
			}

			LinkClasses(); // link server and client classes
		} else {
			base.ProcessClassInfo(msg);
		}

		bool AllowMismatches = false;
		//if (!DTRecv.CreateDecoders(Host.serverDLL.GetStandardSendProxies(), AllowMismatches)) // create receive table decoders
		{
		//	Dbg.Host_Error("CL_ParseClassInfo_EndClasses: CreateDecoders failed.\n");
		//	return false;
		}
		
		return true;
	}
}
