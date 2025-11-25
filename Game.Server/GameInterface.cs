using Microsoft.Extensions.DependencyInjection;

using Source;
using Source.Common;
using Source.Common.Commands;
using Source.Common.Engine;
using Source.Common.Filesystem;
using Source.Common.Server;

namespace Game.Server;

[EngineComponent]
public static class GameInterface
{

}

public class ServerGameDLL(IEngineServer engine, IFileSystem filesystem, ICommandLine CommandLine) : IServerGameDLL
{
	public static void DLLInit(IServiceCollection services) {
		
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
		throw new NotImplementedException();
	}

	public bool GameInit() {
		throw new NotImplementedException();
	}

	public void GameServerSteamAPIActivated() {
		throw new NotImplementedException();
	}

	public void GameServerSteamAPIShutdown() {
		throw new NotImplementedException();
	}

	public void GameShutdown() {
		throw new NotImplementedException();
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
		throw new NotImplementedException();
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
		throw new NotImplementedException();
	}
}
