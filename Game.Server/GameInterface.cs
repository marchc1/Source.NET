using Game.Shared;

using Microsoft.Extensions.DependencyInjection;

using Source;
using Source.Common;
using Source.Common.Bitbuffers;
using Source.Common.Commands;
using Source.Common.Engine;
using Source.Common.Filesystem;
using Source.Common.Formats.Keyvalues;
using Source.Common.Server;

using System.Numerics;

namespace Game.Server;

[EngineComponent]
public static class GameInterface
{

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
		throw new NotImplementedException();
	}

	public void GameServerSteamAPIShutdown() {
		throw new NotImplementedException();
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

	public bool GetUserMessageInfo(int msg_type, Span<char> name, out ReadOnlySpan<char> sized) {
		throw new NotImplementedException();
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
