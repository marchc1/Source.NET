global using static Game.Server.EngineCallbacks;

using Game.Server.GarrysMod;
using Game.Shared;

using Microsoft.Extensions.DependencyInjection;

using Source;
using Source.Common;
using Source.Common.Bitbuffers;
using Source.Common.Commands;
using Source.Common.Engine;
using Source.Common.Filesystem;
using Source.Common.Formats.Keyvalues;
using Source.Common.Mathematics;
using Source.Common.Server;

using System.Numerics;

namespace Game.Server;

[EngineComponent]
public static class GameInterface
{
	public static bf_write? g_pMsgBuffer;
	public static void UserMessageBegin(in IRecipientFilter filter, ReadOnlySpan<char> messagename) {
		Assert(g_pMsgBuffer == null);
		Assert(!messagename.IsStringEmpty);

		int msg_type = usermessages.LookupUserMessage(messagename);

		if (msg_type == -1)
			Error($"UserMessageBegin: Unregistered message '{messagename}'\n");

		g_pMsgBuffer = engine.UserMessageBegin(in filter, msg_type);
	}

	public static void MessageEnd() {
		Assert(g_pMsgBuffer != null);

		engine.MessageEnd();

		g_pMsgBuffer = null;
	}

	public static void MessageWriteByte(int iValue) {
		if (g_pMsgBuffer == null)
			Error("WRITE_BYTE called with no active message\n");

		g_pMsgBuffer.WriteByte(iValue);
	}

	public static void MessageWriteChar(int iValue) {
		if (g_pMsgBuffer == null)
			Error("WRITE_CHAR called with no active message\n");

		g_pMsgBuffer.WriteChar(iValue);
	}

	public static void MessageWriteShort(int iValue) {
		if (g_pMsgBuffer == null)
			Error("WRITE_SHORT called with no active message\n");

		g_pMsgBuffer.WriteShort(iValue);
	}

	public static void MessageWriteWord(int iValue) {
		if (g_pMsgBuffer == null)
			Error("WRITE_WORD called with no active message\n");

		g_pMsgBuffer.WriteWord(iValue);
	}

	public static void MessageWriteLong(int iValue) {
		if (g_pMsgBuffer == null)
			Error("WriteLong called with no active message\n");

		g_pMsgBuffer.WriteLong(iValue);
	}

	public static void MessageWriteFloat(float flValue) {
		if (g_pMsgBuffer == null)
			Error("WriteFloat called with no active message\n");

		g_pMsgBuffer.WriteFloat(flValue);
	}

	public static void MessageWriteAngle(float flValue) {
		if (g_pMsgBuffer == null)
			Error("WriteAngle called with no active message\n");

		g_pMsgBuffer.WriteBitAngle(flValue, 8);
	}

	public static void MessageWriteCoord(float flValue) {
		if (g_pMsgBuffer == null)
			Error("WriteCoord called with no active message\n");

		g_pMsgBuffer.WriteBitCoord(flValue);
	}

	public static void MessageWriteVec3Coord(in Vector3 rgflValue) {
		if (g_pMsgBuffer == null)
			Error("WriteVec3Coord called with no active message\n");

		g_pMsgBuffer.WriteBitVec3Coord(rgflValue);
	}

	public static void MessageWriteVec3Normal(in Vector3 rgflValue) {
		if (g_pMsgBuffer == null)
			Error("WriteVec3Normal called with no active message\n");

		g_pMsgBuffer.WriteBitVec3Normal(rgflValue);
	}

	public static void MessageWriteAngles(in QAngle rgflValue) {
		if (g_pMsgBuffer == null)
			Error("WriteVec3Normal called with no active message\n");

		g_pMsgBuffer.WriteBitAngles(rgflValue);
	}

	public static void MessageWriteString(ReadOnlySpan<char> sz) {
		if (g_pMsgBuffer == null)
			Error("WriteString called with no active message\n");

		g_pMsgBuffer.WriteString(sz);
	}

	public static void MessageWriteEntity(int iValue) {
		if (g_pMsgBuffer == null)
			Error("WriteEntity called with no active message\n");

		g_pMsgBuffer.WriteShort(iValue);
	}

	static EHANDLE hEnt = new();
	public static void MessageWriteEHandle(BaseEntity? entity) {
		if (g_pMsgBuffer == null)
			Error("WriteEHandle called with no active message\n");

		uint iEncodedEHandle;

		if (entity != null) {
			hEnt.Set(entity);

			int iSerialNum = hEnt.GetSerialNumber() & ((1 << Constants.NUM_NETWORKED_EHANDLE_SERIAL_NUMBER_BITS) - 1);
			iEncodedEHandle = (uint)hEnt.GetEntryIndex() | (uint)(iSerialNum << Constants.MAX_EDICT_BITS);

			hEnt.Set(null);
		}
		else {
			iEncodedEHandle = Constants.INVALID_NETWORKED_EHANDLE_VALUE;
		}

		g_pMsgBuffer.WriteLong((int)iEncodedEHandle);
	}

	// bitwise
	public static void MessageWriteBool(bool bValue) {
		if (g_pMsgBuffer == null)
			Error("WriteBool called with no active message\n");

		g_pMsgBuffer.WriteOneBit(bValue ? 1 : 0);
	}

	public static void MessageWriteUBitLong(uint data, int numbits) {
		if (g_pMsgBuffer == null)
			Error("WriteUBitLong called with no active message\n");

		g_pMsgBuffer.WriteUBitLong(data, numbits);
	}

	public static void MessageWriteSBitLong(int data, int numbits) {
		if (g_pMsgBuffer == null)
			Error("WriteSBitLong called with no active message\n");

		g_pMsgBuffer.WriteSBitLong(data, numbits);
	}

	public static void MessageWriteBits(ReadOnlySpan<byte> pIn, int nBits) {
		if (g_pMsgBuffer == null)
			Error("WriteBits called with no active message\n");

		g_pMsgBuffer.WriteBits(pIn, nBits);
	}
}

public class ServerGameDLL(IFileSystem filesystem, ICommandLine CommandLine) : IServerGameDLL
{
	public static void DLLInit(IServiceCollection services) {
		services.AddSingleton<IServerGameEnts, ServerGameEnts>();
		services.AddSingleton<IServerGameClients, ServerGameClients>();
	}

	public void BuildAdjacentMapList() {
		throw new NotImplementedException();
	}

	public void CreateNetworkStringTables() {
		// throw new NotImplementedException();

		GameRulesRegister.CreateNetworkStringTables_GameRules();
	}

	public bool DLLInit(IServiceProvider services) {
		StaticClassIndicesHelpers.DumpDatatablesCompleted();
		BaseEdict.GetChangeAccessor += x => engine.GetChangeAccessor((Edict)x); // Kind of a hack, but this is defined in gameinterface.cpp like this...
		g_SharedChangeInfo = engine.GetSharedEdictChangeInfo();

		gameeventmanager.LoadEventsFromFile("resource/gameevents.res");

		IGameSystem.Add(PhysicsGameSystem());

		if (!IGameSystem.InitAllSystems())
			return false;

		NavMesh.NavMesh.Instance = new();

		return true;
	}

	public void DLLShutdown() {
		throw new NotImplementedException();
	}

	public void GameFrame(bool simulating) {
		if (BaseEntity.IsSimulatingOnAlternateTicks()) {
			if ((gpGlobals.TickCount & 1) != 0) {
				// UpdateAllClientData();
				// return;
			}

			gpGlobals.FrameTime *= 2.0f;
		}

		TimeUnit_t oldFrameTime = gpGlobals.FrameTime;

		gEntList.CleanupDeleteList();

		IGameSystem.FrameUpdatePreEntityThinkAllSystems();

		GameStartFrame();

		NavMesh.NavMesh.Instance?.Update();

		// nextbots todo

		// UpdateQueryCache();

		Physics.RunThinkFunctions(simulating);

		IGameSystem.FrameUpdatePostEntityThinkAllSystems();

		// ServiceEventQueue();

		// UpdateAllClientData();

		// g_pGameRules?.EndGameFrame();

		// g_NetworkPropertyEventMgr.FireEvents();

		gpGlobals.FrameTime = oldFrameTime;
	}

	public bool GameInit() {
		ResetGlobalState();
		engine.ServerCommand("exec game.cfg\n");
		engine.ServerExecute();
		BaseEntity.AccurateTriggerBboxChecks = true;

		IGameEvent? ev = gameeventmanager.CreateEvent("game_init");
		if (ev != null)
			gameeventmanager.FireEvent(ev);

		return true;
	}

	public void GameServerSteamAPIActivated() {

	}

	public void GameServerSteamAPIShutdown() {

	}

	public void GameShutdown() {
		ResetGlobalState();
	}

	public ServerClass? GetAllServerClasses() {
		return ServerClass.Head;
	}

	public ReadOnlySpan<char> GetGameDescription() {
#if GMOD_DLL
		return "Garry's Mod";
#else
		return "Half-Life 2";
#endif
	}

	public ReadOnlySpan<char> GetServerBrowserGameData() {
		throw new NotImplementedException();
	}

	public ReadOnlySpan<char> GetServerBrowserMapOverride() {
		throw new NotImplementedException();
	}

	public TimeUnit_t GetTickInterval() {
		TimeUnit_t tickinterval = Constants.DEFAULT_TICK_INTERVAL;

		if (CommandLine.CheckParm("-tickrate")) {
			double tickrate = CommandLine.ParmValue("-tickrate", 0d);
			if (tickrate > 10)
				tickinterval = 1.0 / tickrate;
		}

		return tickinterval;
	}

	public bool GetUserMessageInfo(int msg_type, Span<char> name, out int size) {
		if (!usermessages.IsValidIndex(msg_type)) {
			size = 0;
			return false;
		}

		strcpy(name, usermessages.GetUserMessageName(msg_type));
		size = usermessages.GetUserMessageSize(msg_type);
		return true;
	}

	public void InvalidateMdlCache() {
		throw new NotImplementedException();
	}

	public bool LevelInit(ReadOnlySpan<char> pMapName, ReadOnlySpan<char> pMapEntities, ReadOnlySpan<char> pOldLevel, ReadOnlySpan<char> pLandmarkName, bool loadGame, bool background) {
		// ResetWindspeed();
		// UpdateChapterRestrictions(pMapName);

		//Tony; parse custom manifest if exists!
		// ParseParticleEffectsMap(pMapName, false);

		// IGameSystem::LevelInitPreEntityAllSystems() is called when the world is precached
		// That happens either in LoadGameState() or in MapEntity_ParseAllEntities()
		if (loadGame) {
			if (!pOldLevel.IsEmpty)
				gpGlobals.LoadType = MapLoadType.Transition;
			else
				gpGlobals.LoadType = MapLoadType.LoadGame;

			// BeginRestoreEntities();
			if (!engine.LoadGameState(pMapName, true)) {
				if (!pOldLevel.IsEmpty)
					ParseAllEntities(pMapEntities);
				else
					// Regular save load case
					return false;
			}

			if (!pOldLevel.IsEmpty)
				engine.LoadAdjacentEnts(pOldLevel, pLandmarkName);

			// if (g_OneWayTransition)
			// 	engine.ClearSaveDirAfterClientLoad();

			// if (pOldLevel && sv_autosave.GetBool() == true) {
			// 	// This is a single-player style level transition.
			// 	// Queue up an autosave one second into the level
			// 	BaseEntity? pAutosave = BaseEntity::Create("logic_autosave", vec3_origin, vec3_angle, NULL);
			// 	if (pAutosave != null) {
			// 		g_EventQueue.AddEvent(pAutosave, "Save", 1.0, NULL, NULL);
			// 		g_EventQueue.AddEvent(pAutosave, "Kill", 1.1, NULL, NULL);
			// 	}
			// }
		}
		else {
			if (background)
				gpGlobals.LoadType = MapLoadType.Background;

			else
				gpGlobals.LoadType = MapLoadType.NewGame;

			// Clear out entity references, and parse the entities into it.
			// g_MapEntityRefs.Purge();
			MapLoadEntityFilter filter = new();
			ParseAllEntities(pMapEntities, filter);

			// g_pServerBenchmark.StartBenchmark();

			// Now call the mod specific parse
			// LevelInit_ParseAllEntities(pMapEntities);
		}

		// Check low violence settings for this map
		// g_RagdollLVManager.SetLowViolence(pMapName);

		// Now that all of the active entities have been loaded in, precache any entities who need point_template parameters
		//  to be parsed (the above code has loaded all point_template entities)
		// PrecachePointTemplates();

		// load MOTD from file into stringtable
		// LoadMessageOfTheDay();

		// Sometimes an ent will Remove() itself during its precache, so RemoveImmediate won't happen.
		// This makes sure those ents get cleaned up.
		gEntList.CleanupDeleteList();

		// g_AIFriendliesTalkSemaphore.Release();
		// g_AIFoesTalkSemaphore.Release();
		// g_OneWayTransition = false;

		// clear any pending autosavedangerous
		// m_fAutoSaveDangerousTime = 0.0f;
		// m_fAutoSaveDangerousMinHealthToCommit = 0.0f;
		return true;
	}

	public void LevelShutdown() {
		IGameSystem.LevelShutdownPreClearSteamAPIContextAllSystems();
		// steamgameserverapicontext.Clear();

		IGameSystem.LevelShutdownPreEntityAllSystems();

		// SoundEnt.ShutdownSoundEnt()

		gEntList.Clear();

		// InvalidateQueryCache();

		IGameSystem.LevelShutdownPostEntityAllSystems();

		NavMesh.NavMesh.Instance!.Reset();
	}

	public void PostInit() {

	}

	public void PreClientUpdate(bool simulating) {
		if (!simulating)
			return;

		IGameSystem.PreClientUpdateAllSystems();
	}

	public void PreSaveGameLoaded(ReadOnlySpan<char> pSaveName, bool bCurrentlyInGame) {
		throw new NotImplementedException();
	}

	public void ServerActivate(Edict[] pEdictList, int edictCount, int clientMax) {
		// if (InRestore)
		// 	return;

		if (gEntList.ResetDeleteList() != 0)
			Msg("ERROR: Entity delete queue not empty on level start!\n");

		for (BaseEntity? ent = gEntList.FirstEnt(); ent != null; ent = gEntList.NextEnt(ent)) {
			if (ent != null && !ent.IsDormant())
				ent.Activate();
		}

		IGameSystem.LevelInitPostEntityAllSystems();
		// BaseEntity.SetAllowPrecache(false);

		NavMesh.NavMesh.Instance.Load();
		NavMesh.NavMesh.Instance.OnServerActivate();

		// todo nextbots
	}

	public void SetServerHibernation(bool bHibernating) {
		throw new NotImplementedException();
	}

	public bool ShouldHideServer() {
		throw new NotImplementedException();
	}

	public void Think(bool finalTick) {

	}
}


public class ServerGameClients : IServerGameClients
{
	const int CMD_MAXBACKUP = 64;

	public void ClientActive(Edict entity, bool loadGame) {
		GMODClient.ClientActive(entity, loadGame);

		if (gpGlobals.LoadType == MapLoadType.LoadGame) {
			// todo
		}

		BasePlayer player = (BasePlayer)BaseEntity.Instance(entity)!;
		// CSoundEnvelopeController::GetController().CheckLoopingSoundsForPlayer(pPlayer);
		// SceneManager_ClientActive(pPlayer);
	}

	public void ClientCommand(Edict entity, in TokenizedCommand args) {
		// throw new NotImplementedException();
	}

	public void ClientCommandKeyValues(Edict entity, KeyValues keyValues) {
		throw new NotImplementedException();
	}

	public bool ClientConnect(Edict entity, ReadOnlySpan<char> name, ReadOnlySpan<char> address, Span<char> reject) {
		throw new NotImplementedException();
	}

	public void ClientDisconnect(Edict entity) {
		throw new NotImplementedException();
	}

	public void ClientEarPosition(Edict entity, out Vector3 earOrigin) {
		throw new NotImplementedException();
	}

	public void ClientPutInServer(Edict entity, ReadOnlySpan<char> playerName) {
		// throw new NotImplementedException();
		GMODClient.ClientPutInServer(entity, playerName);
	}

	public void ClientSettingsChanged(Edict edict) {
		// throw new NotImplementedException();
	}

	public void ClientSetupVisibility(Edict viewEntity, Edict client, Span<byte> pvs) {
		throw new NotImplementedException();
	}

	public void ClientSpawned(Edict player) {
		throw new NotImplementedException();
	}

	public void GetBugReportInfo(Span<char> buf) {
		throw new NotImplementedException();
	}

	public void GetPlayerLimits(out int minPlayers, out int maxPlayers, out int defaultMaxPlayers) {
		minPlayers = defaultMaxPlayers = 1;
		maxPlayers = Constants.MAX_PLAYERS;
	}

	public PlayerState? GetPlayerState(Edict player) {
		if (player == null || player.GetUnknown() == null)
			return null;

		BasePlayer? pl = BaseEntity.Instance(player) as BasePlayer;
		return pl?.pl;
	}

	public void NetworkIDValidated(ReadOnlySpan<char> userName, ReadOnlySpan<char> networkID) {
		throw new NotImplementedException();
	}

	public TimeUnit_t ProcessUsercmds(Edict player, bf_read buf, int numCmds, int totalCmds, int droppedPackets, bool ignore, bool paused) {
		int i;

		UserCmd from, to;

		UserCmd[] cmds = new UserCmd[CMD_MAXBACKUP];

		UserCmd cmdNull = new();

		Assert(numCmds >= 0);
		Assert((totalCmds - numCmds) >= 0);

		BasePlayer? pl = null;
		BaseEntity? ent = BaseEntity.Instance(player);

		if (ent != null && ent.IsPlayer())
			pl = (BasePlayer)ent;

		if (totalCmds < 0 || totalCmds >= (CMD_MAXBACKUP - 1)) {
			ReadOnlySpan<char> name = "unknown";
			if (pl != null)
				name = pl.GetPlayerName();

			Msg($"CBasePlayer::ProcessUsercmds: too many cmds {totalCmds} sent for player {name}\n");
			buf.SetOverflowFlag();
			return 0.0f;
		}

		cmdNull.Reset();
		from = cmdNull;

		for (i = totalCmds - 1; i >= 0; i--) {
			to = cmds[i];
			UserCmd.ReadUsercmd(buf, ref to, ref from);
			from = to;
		}

		if (ignore || pl == null)
			return 0.0f;

		pl.ProcessUsercmds(cmds, numCmds, totalCmds, droppedPackets, paused);

		return TICK_INTERVAL;
	}

	public static int CommandClientIndex = 0;
	public void SetCommandClient(int index) => CommandClientIndex = index;
}

public class ServerGameEnts : IServerGameEnts
{
	public void FreeContainingEntity(Edict e) {
		throw new NotImplementedException();
	}

	public void MarkEntitiesAsTouching(Edict e1, Edict e2) {
		throw new NotImplementedException();
	}

	public void SetDebugEdictBase(Edict[] edict) {

	}
}

struct MapEntityRef
{
	/// <summary>Which edict slot this entity got. -1 if CreateEntityByName failed.</summary>
	public int Edict;
	/// <summary>The edict serial number.</summary>
	public int SerialNumber;
};


class MapLoadEntityFilter : IMapEntityFilter
{
	public bool ShouldCreateEntity(ReadOnlySpan<char> className) => true;

	public BaseEntity? CreateNextEntity(ReadOnlySpan<char> className) {
		BaseEntity? ret = CreateEntityByName(className);
		MapEntityRef entref = new() {
			Edict = -1,
			SerialNumber = 0
		};

		if (ret != null) {
			entref.Edict = ret.EntIndex();
			if (ret.Edict() != null)
				entref.SerialNumber = ret.Edict()!.NetworkSerialNumber;
		}

		return ret;
	}
}
