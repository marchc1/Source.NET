using Source.Common.Networking.DataTable;

namespace Source.Common.Server;

/// <summary>
/// Interface the game DLL exposes to the engine
/// </summary>
public interface IServerGameDLL
{
	void GameShutdown() { }
	public void PostInit();
	public ServerClass? GetAllServerClasses();
	// public StandardSendProxies? GetStandardSendProxies();
}
