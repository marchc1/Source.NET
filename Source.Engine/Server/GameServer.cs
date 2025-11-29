using Source.Common;
using Source.Common.Bitbuffers;
using Source.Common.Client;
using Source.Common.Commands;
using Source.Common.Engine;
using Source.Common.Filesystem;
using Source.Common.Networking;
using Source.Common.Server;
using Source.Common.Utilities;

namespace Source.Engine.Server;

/// <summary>
/// Base server, in SERVER. Often referred to by 'sv'
/// </summary>
public class GameServer : BaseServer
{
#if GMOD_DLL
	readonly ConVar sv_startup_time = new("sv_startup_time", "0", 0, "Sets the starting CurTime of the server (in seconds). This is used for finding issues on a server that has been running for a long time. You don't need to change this.");
	readonly ConVar sv_hibernate_think = new("sv_hibernate_think", "0", 0, "Forces the server to think even when hibernating");
	readonly ConVar sv_hibernate_drop_bots = new("sv_hibernate_drop_bots", "1", 0, "Kicks bots when the server enters hibernation");
#endif
	protected readonly ED ED = Singleton<ED>();
	protected readonly Scr Scr = Singleton<Scr>();
	protected readonly SV SV = Singleton<SV>();
	protected readonly ICommandLine CommandLine = Singleton<ICommandLine>();
	public override void SetMaxClients(int number) {
		MaxClients = Math.Clamp(number, 1, MaxClientsLimit);
		Host.deathmatch.SetValue(MaxClients > 1);
	}

	public override void Init(bool dedicated) {

	}

	public override void Shutdown() {


	}

	public void SetQueryPortFromSteamServer() {
		// todo
	}
	internal bool IsLevelMainMenuBackground() {
		return LevelMainMenuBackground;
	}

	public bool LoadGame;           // handle connections specially

	public InlineArray64<char> Startspot;

	public int NumEdicts;
	public int MaxEdicts;
	public int FreeEdicts;
	public Edict[]? Edicts;
	IChangeInfoAccessor? edictchangeinfo;

	public int MaxClientsLimit;    // Max allowed on server.

	public bool AllowSignOnWrites;
	public bool DLLInitialized;    // Have we loaded the game dll.

	public bool LevelMainMenuBackground;  // true if the level running only as the background to the main menu

	public readonly List<EventInfo> TempEntities = [];     // temp entities

	public readonly bf_write FullSendTables = new();
	public readonly UtlMemory<byte> FullSendTablesBuffer = new();

	public bool LoadedPlugins;

	public void CreateEngineStringTables() {

	}

	public INetworkStringTable? GetModelPrecacheTable() => ModelPrecacheTable;
	public INetworkStringTable? GetGenericPrecacheTable() => GenericPrecacheTable;
	public INetworkStringTable? GetSoundPrecacheTable() => SoundPrecacheTable;
	public INetworkStringTable? GetDecalPrecacheTable() => DecalPrecacheTable;

	public INetworkStringTable? GetDynamicModelsTable() => DynamicModelsTable;


	public int PrecacheModel(ReadOnlySpan<char> name, Res flags, Model? model = null) {
		if (ModelPrecacheTable == null)
			return -1;
		int idx = ModelPrecacheTable.AddString(true, name);
		if (idx == INetworkStringTable.INVALID_STRING_INDEX)
			return -1;
		throw new NotImplementedException();
	}
	public Model? GetModel(int index) {
		if (index <= 0 || ModelPrecacheTable == null)
			return null;
		if (index >= ModelPrecacheTable.GetNumStrings())
			return null;
		PrecacheItem slot = ModelPrecache![index];
		return slot.GetModel();
	}
	public int LookupModelIndex(ReadOnlySpan<char> name) {
		if (ModelPrecacheTable == null)
			return -1;
		int idx = ModelPrecacheTable.FindStringIndex(name);
		return idx == INetworkStringTable.INVALID_STRING_INDEX ? -1 : idx;
	}

	// Accessors to model precaching stuff
	public int PrecacheSound(ReadOnlySpan<char> name, Res flags) {
		if (SoundPrecacheTable == null)
			return -1;
		int idx = SoundPrecacheTable.AddString(true, name);
		if (idx == INetworkStringTable.INVALID_STRING_INDEX)
			return -1;
		throw new NotImplementedException();
	}
	public ReadOnlySpan<char> GetSound(int index) {
		if (index <= 0 || SoundPrecacheTable == null)
			return null;
		if (index >= SoundPrecacheTable.GetNumStrings())
			return null;
		PrecacheItem slot = SoundPrecache![index];
		return slot.GetName();
	}
	public int LookupSoundIndex(ReadOnlySpan<char> name) {
		if (SoundPrecacheTable == null)
			return -1;
		int idx = SoundPrecacheTable.FindStringIndex(name);
		return idx == INetworkStringTable.INVALID_STRING_INDEX ? -1 : idx;
	}

	public int PrecacheGeneric(ReadOnlySpan<char> name, Res flags) {
		if (GenericPrecacheTable == null)
			return -1;
		int idx = GenericPrecacheTable.AddString(true, name);
		if (idx == INetworkStringTable.INVALID_STRING_INDEX)
			return -1;
		throw new NotImplementedException();
	}
	public ReadOnlySpan<char> GetGeneric(int index) {
		if (index <= 0 || GenericPrecacheTable == null)
			return null;
		if (index >= GenericPrecacheTable.GetNumStrings())
			return null;
		PrecacheItem slot = GenericPrecache![index];
		return slot.GetName();
	}
	public int LookupGenericIndex(ReadOnlySpan<char> name) {
		if (GenericPrecacheTable == null)
			return -1;
		int idx = GenericPrecacheTable.FindStringIndex(name);
		return idx == INetworkStringTable.INVALID_STRING_INDEX ? -1 : idx;
	}

	public int PrecacheDecal(ReadOnlySpan<char> name, Res flags) {
		if (DecalPrecacheTable == null)
			return -1;
		int idx = DecalPrecacheTable.AddString(true, name);
		if (idx == INetworkStringTable.INVALID_STRING_INDEX)
			return -1;
		throw new NotImplementedException();
	}
	public int LookupDecalIndex(ReadOnlySpan<char> name) {
		if (DecalPrecacheTable == null)
			return -1;
		int idx = DecalPrecacheTable.FindStringIndex(name);
		return idx == INetworkStringTable.INVALID_STRING_INDEX ? -1 : idx;
	}

	public void DumpPrecacheStats(INetworkStringTable? table) {

	}

	public bool IsHibernating() => Hibernating;
	public void UpdateHibernationState() {
		// todo
	}


	public PrecacheItem[]? ModelPrecache;
	public PrecacheItem[]? GenericPrecache;
	public PrecacheItem[]? SoundPrecache;
	public PrecacheItem[]? DecalPrecache;

	public GameClient Client(int i) => (GameClient)Clients[i];

	private void SetHibernating(bool hibernating) {
		// todo
	}

	internal void InitMaxClients() {
		// todo
	}

	int CurrentSkill;

	internal bool SpawnServer(ReadOnlySpan<char> mapName, ReadOnlySpan<char> mapFile, ReadOnlySpan<char> startspot) {
		mapName = mapName.SliceNullTerminatedString();
		mapFile = mapFile.SliceNullTerminatedString();
		startspot = startspot.SliceNullTerminatedString();
		modelloader.ResetModelServerCounts();

		// ReloadWhitelist(mapName);
		Common.TimestampedLog($"SV_SpawnServer({mapName})");
#if !SWDS
		EngineVGui().UpdateProgressBar(LevelLoadingProgress.SpawnServer);
#endif
#if !SWDS
		Scr.CenterStringOff();
#endif

		if (!startspot.IsEmpty)
			ConDMsg("Spawn Server: {mapName}: [{startspot}]\n");
		else
			ConDMsg($"Spawn Server: {mapName}\n");

		Host.SpawnCount = ++SpawnCount;

		Host.deathmatch.SetValue(IsMultiplayer() ? 1 : 0);
		if (Host.coop.GetInt() != 0)
			Host.deathmatch.SetValue(0);

		CurrentSkill = (int)(Host.skill.GetFloat() + 0.5);
		CurrentSkill = Math.Max(CurrentSkill, 0);
		CurrentSkill = Math.Min(CurrentSkill, 3);

		Host.skill.SetValue((float)CurrentSkill);

		Common.TimestampedLog("StaticPropMgr()->LevelShutdown()");

#if !SWDS
		// g_pShadowMgr->LevelShutdown();
#endif
		// StaticPropMgr()->LevelShutdown();

		Common.TimestampedLog("Host_FreeToLowMark");

		Host.FreeStateAndWorld(true);

		serverGlobalVariables.MapVersion = 0;

		Common.TimestampedLog("sv.Clear()");

		Clear();

		Common.TimestampedLog("framesnapshotmanager->LevelChanged()");

		// framesnapshotmanager->LevelChanged();

		// set map name
		strcpy(MapName, mapName);
		strcpy(MapFilename, mapFile);

		// set startspot
		if (!startspot.IsEmpty)
			strcpy(Startspot, startspot);
		else
			Startspot[0] = '\0';

		// Preload any necessary data from the xzps:
		// g_pFileSystem.SetupPreloadData();
		// g_pMDLCache.InitPreloadData(false);

		// Allocate server memory
		MaxEdicts = Constants.MAX_EDICTS;


		serverGlobalVariables.MaxEntities = MaxEdicts;
		serverGlobalVariables.MaxClients = GetMaxClients();
#if !SWDS
		clientGlobalVariables.NetworkProtocol = Protocol.VERSION;
#endif

		NumEdicts = GetMaxClients() + 1;

		Common.TimestampedLog("SV_AllocateEdicts");

		SV.AllocateEdicts();

		SV.ServerGameEnts!.SetDebugEdictBase(Edicts!);

		AllowSignOnWrites = true;

		ServerClasses = 0;
		ServerClassBits = 0;

		AssignClassIds();

		Common.TimestampedLog("Set up players");

		for (int i = 0; i < GetClientCount(); i++) {
			GameClient pClient = Client(i);

			pClient.Edict = Edicts![i + 1];
			InitializeEntityDLLFields(pClient.Edict!);
		}

		Common.TimestampedLog("Set up players(done)");

		State = ServerState.Loading;

		// Set initial time values.
		TickInterval = host_state.IntervalPerTick;
#if GMOD_DLL
		TickCount = (int)(1.0 / host_state.IntervalPerTick) + 1 + TIME_TO_TICKS(sv_startup_time.GetInt()); // Verify: Does GMOD set sv_startup_time here?
#else
		TickCount = (int)(1.0 / host_state.interval_per_tick) + 1; // Start at appropriate 1
#endif

		serverGlobalVariables.TickCount = TickCount;
		serverGlobalVariables.CurTime = GetTime();

		g_pFileSystem.AddSearchPath(mapFile, "GAME", SearchPathAdd.ToHead);
		g_pFileSystem.BeginMapAccess();

		Common.TimestampedLog($"modelloader->GetModelForName({mapFile}) -- Start");

		host_state.SetWorldModel(modelloader.GetModelForName(mapFile, ModelLoaderFlags.Server));
		if (host_state.WorldModel == null) {
			ConMsg($"Couldn't spawn server {mapFile.SliceNullTerminatedString()}\n");
			State = ServerState.Dead;
			g_pFileSystem.EndMapAccess();
			return false;
		}

		Common.TimestampedLog($"modelloader->GetModelForName({mapFile}) -- Finished");

		if (IsMultiplayer()) {
#if !SWDS
			EngineVGui().UpdateProgressBar(LevelLoadingProgress.CrcMap);
#endif
			// Server map CRC check.
			memreset(ref WorldmapMD5);
			// todo
#if !SWDS
			EngineVGui().UpdateProgressBar(LevelLoadingProgress.CrcClientDll);
#endif
		}
		else {
			memreset(ref WorldmapMD5);
		}

		StringTables = networkStringTableContainerServer;

		Common.TimestampedLog("SV_CreateNetworkStringTables");

#if !SWDS
		EngineVGui().UpdateProgressBar(LevelLoadingProgress.CreateNetworkStringTables);
#endif

		SV.CreateNetworkStringTables();
		PrecacheModel("", 0);
		PrecacheGeneric("", 0);
		PrecacheSound("", 0);

		Common.TimestampedLog($"Precache world model ({mapFile})");

#if !SWDS
		EngineVGui().UpdateProgressBar(LevelLoadingProgress.PrecacheWorld);
#endif
		PrecacheModel(mapFile, Res.FatalIfMissing | Res.Preload, host_state.WorldModel);

		Common.TimestampedLog("Precache brush models");

		Span<char> localmodel = stackalloc char[5];
		for (int i = 1; i < host_state.WorldBrush!.NumSubModels; i++) {
			// Add in world brush models
			memreset(localmodel);
			sprintf(localmodel, "*%i").I(i);
			PrecacheModel(localmodel, Res.FatalIfMissing | Res.Preload, modelloader.GetModelForName(localmodel, ModelLoaderFlags.Server));
		}

#if !SWDS
		EngineVGui().UpdateProgressBar(LevelLoadingProgress.ClearWorld);
#endif
		Common.TimestampedLog("SV_ClearWorld");

		SV.ClearWorld();

		Common.TimestampedLog("InitializeEntityDLLFields");

		InitializeEntityDLLFields(Edicts![0]);

		ED.ClearFreeFlag(Edicts![0]);

		if (Host.coop.GetFloat() != 0)
			serverGlobalVariables.Coop = (Host.coop.GetInt() != 0);
		else
			serverGlobalVariables.Deathmatch = (Host.deathmatch.GetInt() != 0);

		serverGlobalVariables.MapName = new(((ReadOnlySpan<char>)MapName).SliceNullTerminatedString());
		serverGlobalVariables.StartSpot = new(((ReadOnlySpan<char>)Startspot).SliceNullTerminatedString());

		// set game event
		IGameEvent? ev = g_GameEventManager.CreateEvent("server_spawn");
		if (ev != null) {
			ev.SetString("hostname", Host.host_name.GetString());
			ev.SetString("address", Net.LocalAdr.ToString(false));
			ev.SetInt("port", GetUDPPort());
			ev.SetString("game", Common.Gamedir);
			ev.SetString("mapname", GetMapName());
			ev.SetInt("maxplayers", GetMaxClients());
			ev.SetInt("password", 0);              // TODO
#if WIN32
			ev.SetString("os", "WIN32");
#elif LINUX
		ev.SetString( "os", "LINUX" );
#elif OSX
		ev.etString( "os", "OSX" );
#else
#error Please define your platform
#endif
			ev.SetInt("dedicated", IsDedicated() ? 1 : 0);

			g_GameEventManager.FireEvent(ev);
		}

		Common.TimestampedLog("SV_SpawnServer -- Finished");

		g_pFileSystem.EndMapAccess();
		return true;
	}

	private void InitializeEntityDLLFields(Edict edict) {
		throw new NotImplementedException();
	}

	private void AssignClassIds() {

	}

	INetworkStringTable? ModelPrecacheTable;
	INetworkStringTable? SoundPrecacheTable;
	INetworkStringTable? GenericPrecacheTable;
	INetworkStringTable? DecalPrecacheTable;

	INetworkStringTable? DynamicModelsTable;

	bool Hibernating;    // Are we hibernating.  Hibernation makes server process consume approx 0 CPU when no clients are connected
}
