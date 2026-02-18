global using static Game.Server.EngineCallbacks;

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

	static readonly EHANDLE hEnt = new();
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
		throw new NotImplementedException();
	}

	public bool DLLInit(IServiceProvider services) {
		StaticClassIndicesHelpers.DumpDatatablesCompleted();

		NavMesh.NavMesh.Instance = new();

		return true;
	}

	public void DLLShutdown() {
		throw new NotImplementedException();
	}

	public void GameFrame(bool simulating) {

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
		throw new NotImplementedException();
	}

	public void LevelShutdown() {
		throw new NotImplementedException();
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
		throw new NotImplementedException();
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
	public void ClientActive(Edict entity, bool loadGame) {
		throw new NotImplementedException();
	}

	public void ClientCommand(Edict entity, in TokenizedCommand args) {
		throw new NotImplementedException();
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
		throw new NotImplementedException();
	}

	public void ClientSettingsChanged(Edict edict) {
		throw new NotImplementedException();
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

	public PlayerState GetPlayerState(Edict player) {
		throw new NotImplementedException();
	}

	public void NetworkIDValidated(ReadOnlySpan<char> userName, ReadOnlySpan<char> networkID) {
		throw new NotImplementedException();
	}

	public double ProcessUsercmds(Edict player, bf_read buf, int numCmds, int totalCmds, int droppedPackets, bool ignore, bool paused) {
		throw new NotImplementedException();
	}

	public void SetCommandClient(int index) {
		throw new NotImplementedException();
	}
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
