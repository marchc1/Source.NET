using Microsoft.Extensions.DependencyInjection;

using Source.Common;
using Source.Common.Client;
using Source.Common.Commands;
using Source.Common.Engine;
using Source.Common.Filesystem;
using Source.Common.Hashing;
using Source.Common.Mathematics;
using Source.Common.Networking;
using Source.Engine.Server;

using Steamworks;

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
	readonly DtCommonEng DtCommonEng;
	readonly EngineRecvTable RecvTable;
	readonly IPrediction ClientSidePrediction;
	readonly IModelLoader modelloader;
	readonly Lazy<IEngineClient> engineClient_LAZY;
	IEngineClient engineClient => engineClient_LAZY.Value;
	readonly IServiceProvider services;
	readonly ICommandLine CommandLine;
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

	GameServer? _sv;
	GameServer sv => _sv ??= Host.sv;

	readonly PrecacheItem[] ModelPrecache = ClassUtils.BlankInstantiatedArray<PrecacheItem>(PrecacheItem.MAX_MODELS);
	readonly PrecacheItem[] GenericPrecache = ClassUtils.BlankInstantiatedArray<PrecacheItem>(PrecacheItem.MAX_GENERIC);
	readonly PrecacheItem[] SoundPrecache = ClassUtils.BlankInstantiatedArray<PrecacheItem>(PrecacheItem.MAX_SOUNDS);
	readonly PrecacheItem[] DecalPrecache = ClassUtils.BlankInstantiatedArray<PrecacheItem>(PrecacheItem.MAX_BASE_DECAL);

	public static ConVar cl_timeout = new("30", FCvar.Archive, "After this many seconds without receiving a packet from the server, the client will disconnect itself");
	public static ConVar cl_allowdownload = new("1", FCvar.Archive, "Client downloads customization files");
	public static ConVar cl_downloadfilter = new("all", FCvar.Archive, "Determines which files can be downloaded from the server (all, none, nosounds, mapsonly)");

	readonly Common Common;
	public ClientState(Host Host, IFileSystem fileSystem, Net Net, CommonHostState host_state, GameServer sv, Common Common,
		Cbuf Cbuf, Cmd Cmd, ICvar cvar, CL CL, IEngineVGuiInternal? EngineVGui, IHostState HostState, Scr Scr, IEngineAPI engineAPI,
		[FromKeyedServices(Realm.Client)] NetworkStringTableContainer networkStringTableContainerClient, IServiceProvider services,
		IModelLoader modelloader, ICommandLine commandLine, IPrediction ClientSidePrediction, DtCommonEng DtCommonEng, EngineRecvTable recvTable)
		: base(Host, fileSystem, Net, sv, Cbuf, cvar, EngineVGui, engineAPI, networkStringTableContainerClient) {
		this.Host = Host;
		this.fileSystem = fileSystem;
		this.Net = Net;
		this.host_state = host_state;
		this.CL = CL;
		this.Scr = Scr;
		this.modelloader = modelloader;
		this.ClockDriftMgr = new(this, Host, host_state);
		this.EngineVGui = EngineVGui;
		this.HostState = HostState;
		this.DtCommonEng = DtCommonEng;
		this.Common = Common;
		this.services = services;
		this.ClientSidePrediction = ClientSidePrediction;
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

	public override bool HookClientStringTable(ReadOnlySpan<char> tableName) {
		INetworkStringTable? table = GetStringTable(tableName);
		if (table == null) {
			Host.clientDLL?.InstallStringTableCallback(tableName);
			return false;
		}

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

		// CL_ClearState
		Clear(); // RaphaelIT7: Works for now though we should implement CL_ClearState at a later point

		if (showMainMenu)
			Scr.EndLoadingPlaque();

		EngineVGui!.NotifyOfServerDisconnect();
		if (showMainMenu && !engineClient.IsDrawingLoadingImage())
			EngineVGui?.ActivateGameUI();

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
	protected override bool ProcessPacketEntities(svc_PacketEntities msg) {
		if (!msg.IsDelta)
			ClientSidePrediction.OnReceivedUncompressedPacket();
		else {
			if (DeltaTick == -1)
				return true;

			CL.PreprocessEntities();
		}

		if (LocalNetworkBackdoor.Global != null) {
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
		base.PacketStart(incomingSequence, outgoingAcknowledged);
	}

	public override void PacketEnd() {
		base.PacketEnd();
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
	public void SetFrameTime(double dt) => FrameTime = dt;
	public double GetClientInterpAmount() => throw new NotImplementedException();
	public override void Connect(string adr, string sourceTag) {
		Socket = Net.GetSocket(NetSocketType.Client);
		base.Connect(adr, sourceTag);
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
			if (frame.TickCount>= tick) {
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

	public override bool ProcessServerInfo(svc_ServerInfo msg)
	{
		// Reset client state
		Clear();

		if (!base.ProcessServerInfo(msg))
		{
			Disconnect("CBaseClientState::ProcessServerInfo failed", true);
			return false;
		}

		// is server a HLTV proxy ?
		IsHLTV = msg.IsHLTV;

		// The MD5 of the server map must match the MD5 of the client map. or else
		//  the client is probably cheating.
		ServerMD5 = msg.MapMD5;

		if (MaxClients > 1)
		{
			/*if (mp_decals.GetInt() < r_decals.GetInt())
			{
				r_decals.SetValue(mp_decals.GetInt());
			}*/
		}

		// RaphaelIT7
		// ToDo: Set g_ClientGlobalVariables.maxClients = m_nMaxClients same for .network_protocol = msg.Protocol
		// ToDo - How do I use this keyed service stuff, still have barelly an idea :sob:
		// [FromKeyedServices(Realm.Client)] NetworkStringTableContainer networkStringTableContainerClient;
		// StringTableContainer = networkStringTableContainerClient;

		// CL_ReallocateDynamicData(MaxClients);

		if (sv.IsPaused())
		{
			if (msg.TickInterval != host_state.IntervalPerTick)
			{
				Host.Error($"Expecting interval_per_tick {host_state.IntervalPerTick}, got {msg.TickInterval}\n");
				return false;
			}
		} else {
			host_state.IntervalPerTick = msg.TickInterval;
		}

        // Gmod Specific - a global bool. Should we put this into the host state?
        // g_bIsDedicated = msg->m_bIsDedicated;

        // gHostSpawnCount = m_nServerCount;

        // videomode->MarkClientViewRectDirty();
		return true;
    }
}
