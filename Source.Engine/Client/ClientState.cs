using CommunityToolkit.HighPerformance;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;

using Source.Common;
using Source.Common.Audio;
using Source.Common.Bitbuffers;
using Source.Common.Client;
using Source.Common.Commands;
using Source.Common.Engine;
using Source.Common.Filesystem;
using Source.Common.Hashing;
using Source.Common.Mathematics;
using Source.Common.Networking;
using Source.Engine.Server;

using Steamworks;

using System.Buffers;
using System.Runtime.CompilerServices;

using static Source.Constants;

using GameServer = Source.Engine.Server.GameServer;

namespace Source.Engine.Client;

/// <summary>
/// Client state, in CLIENT. Often referred to by 'cl'
/// </summary>
public class ClientState : BaseClientState
{
	readonly Host Host;
	readonly IFileSystem fileSystem;
	readonly Net Net;
	readonly CommonHostState host_state;
	readonly CL CL;
	readonly IEngineVGuiInternal? EngineVGui;
	readonly IHostState HostState;
	readonly ICvar cvar;
	readonly DtCommonEng DtCommonEng;
	readonly EngineRecvTable RecvTable;
	readonly IPrediction ClientSidePrediction;
	readonly IModelLoader modelloader;
	readonly ClientGlobalVariables clientGlobalVariables;
	readonly Lazy<IEngineClient> engineClient_LAZY;
	IEngineClient engineClient => engineClient_LAZY.Value;
	readonly IServiceProvider services;
	readonly ICommandLine CommandLine;
	readonly Scr Scr;

	public TimeUnit_t LastServerTickTime;
	public bool InSimulation;

	public int OldTickCount;
	public TimeUnit_t TickRemainder;
	public TimeUnit_t FrameTime;

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


	readonly PrecacheItem[] ModelPrecache = ClassUtils.BlankInstantiatedArray<PrecacheItem>(PrecacheItem.MAX_MODELS);
	readonly PrecacheItem[] GenericPrecache = ClassUtils.BlankInstantiatedArray<PrecacheItem>(PrecacheItem.MAX_GENERIC);
	readonly PrecacheItem[] SoundPrecache = ClassUtils.BlankInstantiatedArray<PrecacheItem>(PrecacheItem.MAX_SOUNDS);
	readonly PrecacheItem[] DecalPrecache = ClassUtils.BlankInstantiatedArray<PrecacheItem>(PrecacheItem.MAX_BASE_DECAL);

	public static ConVar cl_timeout = new("30", FCvar.Archive, "After this many seconds without receiving a packet from the server, the client will disconnect itself");
	public static ConVar cl_allowdownload = new("1", FCvar.Archive, "Client downloads customization files");
	public static ConVar cl_downloadfilter = new("all", FCvar.Archive, "Determines which files can be downloaded from the server (all, none, nosounds, mapsonly)");

	readonly Common Common;
	readonly Sound Sound;
	public ClientState(Host Host, IFileSystem fileSystem, Net Net, CommonHostState host_state, Common Common,
		Cbuf Cbuf, Cmd Cmd, ICvar cvar, CL CL, IEngineVGuiInternal? EngineVGui, IHostState HostState, Scr Scr, IEngineAPI engineAPI,
		[FromKeyedServices(Realm.Client)] NetworkStringTableContainer networkStringTableContainerClient, IServiceProvider services,
		IModelLoader modelloader, ICommandLine commandLine, IPrediction ClientSidePrediction, DtCommonEng DtCommonEng, EngineRecvTable recvTable,
		ClientGlobalVariables clientGlobalVariables, Sound Sound)
		: base(Host, fileSystem, Net, Cbuf, cvar, EngineVGui, engineAPI, networkStringTableContainerClient) {
		this.Host = Host;
		this.fileSystem = fileSystem;
		this.Net = Net;
		this.host_state = host_state;
		this.cvar = cvar;
		this.CL = CL;
		this.Scr = Scr;
		this.modelloader = modelloader;
		this.ClockDriftMgr = new(Host, host_state, Net, clientGlobalVariables);
		this.EngineVGui = EngineVGui;
		this.HostState = HostState;
		this.DtCommonEng = DtCommonEng;
		this.Common = Common;
		this.services = services;
		this.ClientSidePrediction = ClientSidePrediction;
		this.clientGlobalVariables = clientGlobalVariables;
		this.Sound = Sound;
		engineClient_LAZY = new(ProduceEngineClient);
		CommandLine = commandLine;
		RecvTable = recvTable;
	}

	private IEngineClient ProduceEngineClient() => services.GetRequiredService<IEngineClient>();
	public override void Clear() {
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

		ModelPrecache.ClearInstantiatedReferences();
		SoundPrecache.ClearInstantiatedReferences();
		DecalPrecache.ClearInstantiatedReferences();
		GenericPrecache.ClearInstantiatedReferences();

		IsHLTV = false;

		if (ServerMD5.Bits != null) // RaphaelIT7: Yes... We can be called so early that the other's constructor's weren't called yet.
			Array.Clear(ServerMD5.Bits, 0, ServerMD5.Bits.Length);

		LastCommandAck = 0;
		CommandAck = 0;
		SoundSequence = 0;

		if (SignOnState > SignOnState.Connected) {
			SignOnState = SignOnState.Connected;
		}
	}

	public bool ProcessConnectionlessPacket(in NetPacket packet) {
		return false;
	}

	protected override bool ProcessTick(NET_Tick msg) {
		int tick = msg.Tick;

		NetChannel!.SetRemoteFramerate(msg.HostFrameTime, msg.HostFrameDeviation);

		ClockDriftMgr.SetServerTick(tick);

		LastServerTickTime = tick * host_state.IntervalPerTick;

		clientGlobalVariables.TickCount = tick;
		clientGlobalVariables.CurTime = tick * host_state.IntervalPerTick;
		clientGlobalVariables.FrameTime = (tick - OldTickCount) * host_state.IntervalPerTick;

		return true;
	}

	public override bool HookClientStringTable(ReadOnlySpan<char> tableName) {
		INetworkStringTable? table = GetStringTable(tableName);
		if (table == null) {
			Host.clientDLL?.InstallStringTableCallback(tableName);
			return false;
		}

		StringTableBits.CL_SetupNetworkStringTableBits(StringTableContainer!, tableName);

		switch (tableName) {
			case PrecacheItem.MODEL_PRECACHE_TABLENAME:
				ModelPrecacheTable = table;
				return true;
			case PrecacheItem.GENERIC_PRECACHE_TABLENAME:
				GenericPrecacheTable = table;
				return true;
			case PrecacheItem.SOUND_PRECACHE_TABLENAME:
				SoundPrecacheTable = table;
				return true;
			case PrecacheItem.DECAL_PRECACHE_TABLENAME:
				DecalPrecacheTable = table;
				return true;
			case Protocol.USER_INFO_TABLENAME:
				UserInfoTable = table;
				return true;
		}

		Host.clientDLL?.InstallStringTableCallback(tableName);
		return false;
	}


	public override void Disconnect(ReadOnlySpan<char> reason, bool showMainMenu) {
		base.Disconnect(reason, showMainMenu);

		IGameEvent? ev = g_GameEventManager.CreateEvent("client_disconnect");
		if (ev != null) {
			ev.SetString("message", reason);
			g_GameEventManager.FireEventClientSide(ev);
		}

		Sound.StopAllSounds(true);
		R.DecalTermAll();

		if (MaxClients > 1)
			if (EngineVGui!.IsConsoleVisible() == false)
				EngineVGui!.EnabledProgressBarForNextLoad();

		CL.ClearState();

		CL.HTTPStop_f();

		if (showMainMenu)
			Scr.EndLoadingPlaque();

		EngineVGui!.NotifyOfServerDisconnect();

		if (showMainMenu && !engineClient.IsDrawingLoadingImage())
			EngineVGui.ActivateGameUI();

		HostState.OnClientDisconnected();
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
	public override void ConnectionClosing(ReadOnlySpan<char> reason) {
		if (SignOnState > SignOnState.None) {
			if (reason != null && reason.Length > 0 && reason[0] == '#')
				Common.ExplainDisconnection(true, reason);
			else
				Common.ExplainDisconnection(true, $"Disconnect: {reason}.\n");

			Scr.EndLoadingPlaque();
			Host.Disconnect(true, reason);
		}
	}

	GameEventManager? gem;
	GameEventManager gameEventManager => gem ??= (GameEventManager)Singleton<IGameEventManager2>();
	protected override bool ProcessGameEventList(svc_GameEventList msg) {
		return gameEventManager.ParseEventList(msg);
	}
	protected override bool ProcessGameEvent(svc_GameEvent msg) {
		int startbit = msg.DataIn.BitsRead;

		IGameEvent? ev = gameEventManager.UnserializeEvent(msg.DataIn);

		int length = msg.DataIn.BitsRead - startbit;

		if (length != msg.Length) {
			DevMsg("ClientState.ProcessGameEvent: KeyValue length mismatch.\n");
			return true;
		}

		if (ev == null) {
			DevMsg("ClientState.ProcessGameEvent: UnserializeKeyValue failed.\n");
			return true;
		}

		gameEventManager.FireEventClientSide(ev);
		return true;
	}
	public override bool ProcessClassInfo(svc_ClassInfo msg) {
		if (msg.CreateOnClient) {
			DtCommonEng.CreateClientTablesFromServerTables();
			DtCommonEng.CreateClientClassInfosFromServerClasses(this);

			LinkClasses();
		}
		else
			base.ProcessClassInfo(msg);

		bool allowMismatches = false;
		if (!RecvTable.CreateDecoders(allowMismatches, out _)) {
			Host.EndGame(true, "CL.ProcessClassInfo: CreateDecoders failed.\n");
			return false;
		}
		return true;
	}
	void ProcessSoundsWithProtoVersion(svc_Sounds msg, List<SoundInfo> sounds, int protoVersion) {
		SoundInfo defaultSound = default;
		defaultSound.SetDefault();
		ref SoundInfo pDeltaSound = ref defaultSound;

		sounds.EnsureCapacity(256);

		for (int i = 0; i < msg.NumSounds; i++) {
			int nSound = sounds.Count; sounds.Add(new());
			ref SoundInfo pSound = ref sounds.AsSpan()[nSound];

			pSound.ReadDelta(ref pDeltaSound, msg.DataIn, protoVersion);

			pDeltaSound = pSound;   // copy delta values

			if (msg.ReliableSound) {
				SoundSequence = (SoundSequence + 1) & SOUND_SEQNUMBER_MASK;
				Assert(pSound.SequenceNumber == 0);
				pSound.SequenceNumber = SoundSequence;
			}
		}
	}
	protected override bool ProcessSounds(svc_Sounds msg) {
		if (msg.DataIn.Overflowed)
			return false;

		List<SoundInfo> sounds = ListPool<SoundInfo>.Shared.Alloc();

		int startbit = msg.DataIn.BitsRead;

		ProcessSoundsWithProtoVersion(msg, sounds, clientGlobalVariables.NetworkProtocol);

		int nRelativeBitsRead = msg.DataIn.BitsRead - startbit;

		if (msg.Length != nRelativeBitsRead || msg.DataIn.Overflowed) {
			sounds.Clear();

			int nFallbackProtocol = 0;

			if (clientGlobalVariables.NetworkProtocol == Protocol.PROTOCOL_VERSION_18)
				nFallbackProtocol = Protocol.PROTOCOL_VERSION_19;
			else if (clientGlobalVariables.NetworkProtocol == Protocol.PROTOCOL_VERSION_19)
				nFallbackProtocol = Protocol.PROTOCOL_VERSION_18;

			if (nFallbackProtocol != 0) {
				msg.DataIn.Reset();
				msg.DataIn.Seek(startbit);

				ProcessSoundsWithProtoVersion(msg, sounds, nFallbackProtocol);

				nRelativeBitsRead = msg.DataIn.BitsRead - startbit;
			}
		}

		if (msg.Length == nRelativeBitsRead) {
			Span<SoundInfo> soundsSpan = sounds.AsSpan();
			for (int i = 0; i < soundsSpan.Length; ++i) {
				CL.AddSound(in soundsSpan[i]);
			}

			ListPool<SoundInfo>.Shared.Free(sounds);
			return true;
		}

		msg.DataIn.Reset();
		msg.DataIn.Seek(startbit + msg.Length);

		ListPool<SoundInfo>.Shared.Free(sounds);
		return false;
	}
	protected override bool ProcessEntityMessage(svc_EntityMessage msg) {
		IClientNetworkable? entity = entitylist.GetClientNetworkable(msg.EntityIndex);

		if (entity == null)
			return true;

		byte[] entityData = ArrayPool<byte>.Shared.Rent(MAX_ENTITY_MSG_DATA);
		bf_read entMsg = new("EntityMessage(read)", entityData, entityData.Length);
		int bitsRead = msg.DataIn.ReadBitsClamped(entityData, (uint)msg.Length);
		entMsg.StartReading(entityData, Net.Bits2Bytes(bitsRead));
		entity.ReceiveMessage(msg.ClassID, entMsg);
		ArrayPool<byte>.Shared.Return(entityData, true);

		return true;

	}
	protected override bool ProcessPacketEntities(svc_PacketEntities msg) {
		if (!msg.IsDelta)
			ClientSidePrediction.OnReceivedUncompressedPacket();
		else {
			if (DeltaTick == -1)
				return true;

			CL.PreprocessEntities();
		}

		if (CL.LocalNetworkBackdoor != null) {
			if (SignOnState == SignOnState.Spawn)
				SetSignonState(SignOnState.Full, ServerCount);

			DeltaTick = GetServerTickCount();
			return true;
		}

		if (!CL.ProcessPacketEntities(msg))
			return false;

		return base.ProcessPacketEntities(msg);
	}

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

				Span<char> mapname = stackalloc char[256];
				CL.SetupMapName(modelloader.GetName(host_state.WorldModel!), mapname);
				mapname = mapname.SliceNullTerminatedString();

				g_ClientDLL!.LevelInitPreEntity(mapname);

				break;
			case SignOnState.Full:
				CL.FullyConnected();
				if (NetChannel != null) {
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

				if (MaxClients > 1)
					EngineVGui?.UpdateProgressBar(LevelLoadingProgress.ChangeLevel);
				break;
		}

		if (state >= SignOnState.Connected && NetChannel != null) {
			var msg = new NET_SignonState(state, ServerCount);
			NetChannel.SendNetMsg(msg);
		}

		return true;
	}

	public override void PacketStart(int incomingSequence, int outgoingAcknowledged) {
		CurrentSequence = incomingSequence;
		CommandAck = outgoingAcknowledged;
	}

	public override void PacketEnd() {
		CL.DispatchSounds();
		if (GetServerTickCount() != DeltaTick)
			return;
		int commandsAcknowledged = CommandAck - LastCommandAck;
		LastCommandAck = CommandAck;
		// clientside prediction todo
		g_ClientSidePrediction.PostNetworkDataReceived(commandsAcknowledged);
	}
	readonly LinkedList<EventInfo> Events = [];

	protected override bool ProcessTempEntities(svc_TempEntities msg) {
		bool reliable = false;

		TimeUnit_t fire_time = GetTime();

		if (MaxClients > 1) {
			TimeUnit_t interpAmount = GetClientInterpAmount();
			fire_time += interpAmount;
		}

		if (msg.NumEntries == 0) {
			reliable = true;
			msg.NumEntries = 1;
		}

		EventFlags flags = reliable ? EventFlags.Reliable : 0;

		bf_read buffer = msg.DataIn;

		int classID = -1;
		C_ServerClassInfo? serverClass = null;
		ClientClass? clientClass = null;
		byte[] data = ArrayPool<byte>.Shared.Rent(EventInfo.MAX_EVENT_DATA);

		bf_write toBuf = new(data, data.Length);
		EventInfo? ei = null;

		for (int i = 0; i < msg.NumEntries; i++) {
			float delay = 0.0f;

			if (buffer.ReadOneBit() != 0)
				delay = (float)buffer.ReadSBitLong(8) / 100.0f;

			toBuf.Reset();

			if (buffer.ReadOneBit() != 0) {
				classID = (int)buffer.ReadUBitLong(ServerClassBits);

				serverClass = ServerClasses?[classID - 1];

				if (serverClass == null) {
					DevMsg($"CL_QueueEvent: missing server class info for {classID - 1}.\n");
					ArrayPool<byte>.Shared.Return(data);
					return false;
				}

				clientClass = FindClientClass(serverClass.ClassName);

				if (clientClass == null || clientClass.RecvTable == null) {
					DevMsg($"CL_QueueEvent: missing client receive table for {serverClass.ClassName}.\n");
					ArrayPool<byte>.Shared.Return(data);
					return false;
				}

				RecvTable.MergeDeltas(clientClass.RecvTable, null, buffer, toBuf);
			}
			else {
				Assert(ei != null);

				uint buffer_size = (uint)NetChannel.PAD_NUMBER(NetChannel.Bits2Bytes(ei.Bits), 4);
				bf_read fromBuf = new(ei.Data!, buffer_size);

				RecvTable.MergeDeltas(clientClass!.RecvTable!, fromBuf, buffer, toBuf);
			}

			ei = new();
			Events.AddLast(ei);

			int size = NetChannel.Bits2Bytes(toBuf.BitsWritten);

			ei.ClassID = (short)classID;
			ei.FireDelay = fire_time + delay;
			ei.Flags = flags;
			ei.ClientClass = clientClass;
			ei.Bits = toBuf.BitsWritten;

			ei.Data = new byte[size.AlignValue(4)];
			data.AsSpan()[..size].CopyTo(ei.Data);
		}

		ArrayPool<byte>.Shared.Return(data);
		return true;
	}

	public override void FileReceived(ReadOnlySpan<char> fileName, uint transferID) {
		throw new NotImplementedException();
	}
	public override void FileRequested(ReadOnlySpan<char> fileName, uint transferID) {
		throw new NotImplementedException();
	}
	public override void FileDenied(ReadOnlySpan<char> fileName, uint transferID) {
		throw new NotImplementedException();
	}
	public override void FileSent(ReadOnlySpan<char> fileName, uint transferID) {
		throw new NotImplementedException();
	}
	public override void ConnectionCrashed(ReadOnlySpan<char> reason) {
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
		info.FriendsID = SteamUser.GetSteamID().m_SteamID;
		info.FriendsName = "";

		// check stuff later

		NetChannel!.SendNetMsg(info);
	}

	public void FinishSignonState_New() {
		if (SignOnState != SignOnState.New)
			return;

		if (!MarkedCRCsUnverified) {
			MarkedCRCsUnverified = true;
			fileSystem.MarkAllCRCsUnverified();
		}

		if (!CL.CheckCRCs(LevelFileName))
			Host.Error("Unable to verify map");

		if (sv.State < ServerState.Loading)
			modelloader.ResetModelServerCounts();
		SetModel(1);

		CL.RegisterResources();

		// We can start loading the world now
		Host.Render.LevelInit(); // Tells the rendering system that a new set of world moels exists

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
	public void SetFrameTime(TimeUnit_t dt) => FrameTime = dt;
	ConVar? s_cl_interp_ratio = null;
	ConVar? s_cl_interp = null;
	ConVar_ServerBounded? s_cl_interp_ratio_bounded = null;

	public TimeUnit_t GetClientInterpAmount() {
		if (s_cl_interp_ratio == null) {
			s_cl_interp_ratio = cvar.FindVar("cl_interp_ratio");
			if (s_cl_interp_ratio == null)
				return 0.1;
		}

		if (s_cl_interp == null) {
			s_cl_interp = cvar.FindVar("cl_interp");
			if (s_cl_interp == null)
				return 0.1;
		}

		float interpRatio = s_cl_interp_ratio.GetFloat();
		float interp = s_cl_interp.GetFloat();

		s_cl_interp_ratio_bounded ??= (ConVar_ServerBounded?)s_cl_interp_ratio;
		if (s_cl_interp_ratio_bounded != null)
			interpRatio = s_cl_interp_ratio_bounded.GetFloat();
		return Math.Max(interpRatio / cl_updaterate.GetFloat(), interp);
	}
	public override void Connect(ReadOnlySpan<char> adr, string sourceTag) {
		base.Connect(adr, sourceTag);
	}

	static readonly char[] szHashedKeyBuffer = new char[64];
	public override string GetCDKeyHash() {
		return "12345678901234567890123456789012";
	}

	public void ClearSounds() // RaphaelIT7: This is used by Snd_Restart_f
	{
		SoundPrecache.ClearInstantiatedReferences();
	}

	public Model? GetModel(int index) {
		if (ModelPrecacheTable == null)
			return null;
		if (index <= 0)
			return null;
		if (index >= ModelPrecacheTable.GetNumStrings()) {
			Assert(false);
			return null;
		}

		PrecacheItem p = ModelPrecache[index];
		Model? model = p.GetModel();
		if (model != null)
			return model;

		if (index == 1) {
			Assert(false);
			Warning("Attempting to get world model before it was loaded\n");
			return null;
		}

		ReadOnlySpan<char> name = ModelPrecacheTable.GetString(index);

		model = modelloader.GetModelForName(name, ModelLoaderFlags.Client);
		if (model == null) {
			ref PrecacheUserData data = ref CL.GetPrecacheUserData(ModelPrecacheTable, index);
			if (!Unsafe.IsNullRef(ref data) && (data.Flags & Res.FatalIfMissing) != 0) {
				Common.ExplainDisconnection(true, $"Cannot continue without model {name}, disconnecting\n");
				Host.Disconnect(true, "Missing model");
			}
		}

		p.SetModel(model);
		return model;
	}
	public void SetModel(int tableIndex) {
		if (ModelPrecacheTable == null)
			return;

		if (tableIndex < 0 || tableIndex >= ModelPrecacheTable.GetNumStrings())
			return;

		ReadOnlySpan<char> name = ModelPrecacheTable.GetString(tableIndex);
		if (tableIndex == 1)
			name = LevelFileName;

		PrecacheItem p = ModelPrecache[tableIndex];
		ref PrecacheUserData data = ref CL.GetPrecacheUserData(ModelPrecacheTable, tableIndex);

		bool loadNow = !Unsafe.IsNullRef(ref data) && (data.Flags & Res.Preload) != 0;
		if (CommandLine.FindParm("-nopreload") != 0 || CommandLine.FindParm("-nopreloadmodels") != 0)
			loadNow = false;
		else if (CommandLine.FindParm("-preload") != 0)
			loadNow = true;

		if (loadNow)
			p.SetModel(modelloader.GetModelForName(name, ModelLoaderFlags.Client));
		else
			p.SetModel(null);
	}

	public ClientFrame AllocateFrame() {
		return ClientFramePool.Alloc();
	}

	public void FreeFrame(ClientFrame frame) {
		if (ClientFramePool.IsMemoryPoolAllocated(frame))
			ClientFramePool.Free(frame);
	}

	public ClientFrame? GetClientFrame(int tick, bool exact = true) {
		if (tick < 0)
			return null;

		ClientFrame? frame = Frames;
		ClientFrame? lastFrame = frame;

		while (frame != null) {
			if (frame.TickCount >= tick) {
				if (frame.TickCount == tick)
					return frame;

				if (exact)
					return null;

				return lastFrame;
			}

			lastFrame = frame;
			frame = frame.Next;
		}

		if (exact)
			return null;

		return lastFrame;
	}

	ClientFrame? Frames;
	ClientFrame? LastFrame;
	int NumFrames;
	readonly ClassMemoryPool<ClientFrame> ClientFramePool = new();

	internal int AddClientFrame(ClientFrame frame) {
		Assert(frame.TickCount > 0);

		if (Frames == null) {
			Assert(LastFrame == null && NumFrames == 0);
			Frames = frame;
			LastFrame = frame;
			NumFrames = 1;
			return 1;
		}

		Assert(Frames != null && NumFrames > 0);
		Assert(LastFrame!.Next == null);
		LastFrame.Next = frame;
		LastFrame = frame;
		return ++NumFrames;
	}

	internal void DeleteClientFrames(int tick) {
		if (tick < 0) {
			while (NumFrames > 0) {
				RemoveOldestFrame();
			}
		}
		else {
			ClientFrame? frame = Frames;
			LastFrame = null;
			while (frame != null) {
				if (frame.TickCount < tick) {
					ClientFrame? next = frame.Next;
					if (Frames == frame)
						Frames = next;
					FreeFrame(frame);
					if (--NumFrames == 0) {
						Assert(next == null);
						LastFrame = Frames = null;
						break;
					}
					Assert(LastFrame != frame && NumFrames > 0);
					frame = next;
					if (LastFrame != null)
						LastFrame.Next = next;
				}
				else {
					Assert(LastFrame == null || LastFrame.Next == frame);
					LastFrame = frame;
					frame = frame.Next;
				}
			}
		}
	}

	private void RemoveOldestFrame() {
		ClientFrame? frame = Frames;

		if (frame == null)
			return;

		Assert(NumFrames > 0);
		Frames = frame.Next; // unlink head
												 // deleting frame will decrease global reference counter
		FreeFrame(frame);

		if (--NumFrames == 0) {
			Assert(LastFrame == frame && Frames == null);
			LastFrame = null;
		}
	}

	internal void ReadPacketEntities(EntityReadInfo u) {
		u.NextOldEntity();

		while (u.UpdateType < UpdateType.Finished) {
			u.HeaderCount--;

			u.IsEntity = (u.HeaderCount >= 0) ? true : false;
			if (u.IsEntity)
				CL.ParseDeltaHeader(u);

			u.UpdateType = UpdateType.PreserveEnt;

			while (u.UpdateType == UpdateType.PreserveEnt) {
				if (CL.DetermineUpdateType(u)) {
					switch (u.UpdateType) {
						case UpdateType.EnterPVS:
							ReadEnterPVS(u);
							break;

						case UpdateType.LeavePVS:
							ReadLeavePVS(u);
							break;

						case UpdateType.DeltaEnt:
							ReadDeltaEnt(u);
							break;

						case UpdateType.PreserveEnt:
							ReadPreserveEnt(u);
							break;

						default:
							DevMsg(1, "ReadPacketEntities: unknown updatetype %i\n", u.UpdateType);
							break;
					}
				}
			}
		}

		if (u.AsDelta && u.UpdateType == UpdateType.Finished)
			ReadDeletions(u);

		if (u.Buf!.Overflowed)
			Host.Error("CL.ParsePacketEntities: buffer read overflow\n");

		if (!u.AsDelta)
			NextCmdTime = 0.0;
	}

	protected override void ReadDeletions(EntityReadInfo u) {
		while (u.Buf!.ReadOneBit() != 0) {
			int idx = (int)u.Buf.ReadUBitLong(MAX_EDICT_BITS);
			CL.DeleteDLLEntity(idx, "ReadDeletions");
		}
	}

	protected override void ReadPreserveEnt(EntityReadInfo u) {
		if (!u.AsDelta) {
			Assert(false);
			ConMsg("WARNING: PreserveEnt on full update");
			u.UpdateType = UpdateType.Failed;
			return;
		}

		if (u.OldEntity >= MAX_EDICTS || u.OldEntity < 0 || u.NewEntity >= MAX_EDICTS)
			Host.Error($"CL_ReadPreserveEnt: Entity out of bounds. Old: {u.OldEntity}, New: {u.NewEntity}");

		u.To!.LastEntity = u.OldEntity;
		u.To!.TransmitEntity.Set(u.OldEntity);

		if (CL.cl_entityreport.GetBool())
			CL.RecordEntityBits(u.OldEntity, 0);

		CL.PreserveExistingEntity(u.OldEntity);

		u.NextOldEntity();
	}

	protected override void ReadDeltaEnt(EntityReadInfo u) {
		CL.CopyExistingEntity(u);
		u.NextOldEntity();
	}

	protected override void ReadLeavePVS(EntityReadInfo u) {
		if (!u.AsDelta) {
			Assert(false);
			ConMsg("WARNING: LeavePVS on full update");
			u.UpdateType = UpdateType.Failed;
			return;
		}

		if ((u.UpdateFlags & DeltaEncodingFlags.Delete) != 0)
			CL.DeleteDLLEntity(u.OldEntity, "ReadLeavePVS");

		u.NextOldEntity();
	}

	protected override void ReadEnterPVS(EntityReadInfo u) {
		int iClass = (int)u.Buf!.ReadUBitLong(ServerClassBits);

		int iSerialNum = (int)u.Buf!.ReadUBitLong(NUM_NETWORKED_EHANDLE_SERIAL_NUMBER_BITS);

		CL.CopyNewEntity(u, iClass, iSerialNum);

		if (u.NewEntity == u.OldEntity)
			u.NextOldEntity();
	}

	internal void CopyEntityBaseline(int from, int to) {
		for (int i = 0; i < MAX_EDICTS; i++) {
			PackedEntity? blfrom = EntityBaselines[from][i];
			PackedEntity? blto = EntityBaselines[to][i];

			if (blfrom == null) {
				if (blto != null)
					EntityBaselines[to][i] = null;

				continue;
			}

			if (blto == null) {
				blto = EntityBaselines[to][i] = new PackedEntity();
				blto.ClientClass = null;
				blto.ServerClass = null;
				blto.ReferenceCount = 0;
			}

			Assert(blfrom.EntityIndex == i);
			Assert(!blfrom.IsCompressed());

			blto.EntityIndex = blfrom.EntityIndex;
			blto.ClientClass = blfrom.ClientClass;
			blto.ServerClass = blfrom.ServerClass;
			blto.AllocAndCopyPadded(blfrom.GetData());
		}
	}

	public override bool ProcessServerInfo(svc_ServerInfo msg) {
		// Reset client state
		Clear();

		if (!base.ProcessServerInfo(msg)) {
			Disconnect("CBaseClientState::ProcessServerInfo failed", true);
			return false;
		}

		// is server a HLTV proxy ?
		IsHLTV = msg.IsHLTV;

		// The MD5 of the server map must match the MD5 of the client map. or else
		//  the client is probably cheating.
		ServerMD5 = msg.MapMD5;

		if (MaxClients > 1) {
			/*if (mp_decals.GetInt() < r_decals.GetInt())
			{
				r_decals.SetValue(mp_decals.GetInt());
			}*/
		}

		clientGlobalVariables.MaxClients = MaxClients;
		clientGlobalVariables.NetworkProtocol = msg.Protocol;

		StringTableContainer = KeyedSingleton<NetworkStringTableContainer>(Realm.Client);

		CL.ReallocateDynamicData(MaxClients);

		if (sv.IsPaused()) {
			if (msg.TickInterval != host_state.IntervalPerTick) {
				Host.Error($"Expecting interval_per_tick {host_state.IntervalPerTick}, got {msg.TickInterval}\n");
				return false;
			}
		}
		else {
			host_state.IntervalPerTick = msg.TickInterval;
		}

		// Gmod Specific - a global bool. Should we put this into the host state?
		// g_bIsDedicated = msg->m_bIsDedicated;

		g_ClientDLL?.HudVidInit();

		// gHostSpawnCount = m_nServerCount;

		// videomode->MarkClientViewRectDirty();
		return true;
	}

	internal int LookupModelIndex(ReadOnlySpan<char> name) {
		if (ModelPrecacheTable == null)
			return -1;
		int idx = ModelPrecacheTable.FindStringIndex(name);
		return idx == INetworkStringTable.INVALID_STRING_INDEX ? -1 : idx;
	}

	public int LookupSoundIndex(ReadOnlySpan<char> name) {
		if (SoundPrecacheTable == null)
			return -1;
		int idx = SoundPrecacheTable.FindStringIndex(name);
		return idx == INetworkStringTable.INVALID_STRING_INDEX ? -1 : idx;
	}

	public ReadOnlySpan<char> GetSoundName(int index) {
		if (index <= 0 || SoundPrecacheTable == null)
			return "";

		if (index >= SoundPrecacheTable.GetNumStrings()) {
			return "";
		}

		ReadOnlySpan<char> name = SoundPrecacheTable.GetString(index);
		return name;
	}

	public SfxTable? GetSound(int index) {
		if (index <= 0 || SoundPrecacheTable == null)
			return null;

		if (index >= SoundPrecacheTable.GetNumStrings())
			return null;

		PrecacheItem p = SoundPrecache[index];
		SfxTable? s = p.GetSound();
		if (s != null)
			return s;

		ReadOnlySpan<char> name = SoundPrecacheTable.GetString(index);

		s = Sound.PrecacheSound(name);

		p.SetSound(s!);
		return s;
	}
}
