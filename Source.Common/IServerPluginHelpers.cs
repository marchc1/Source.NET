using Microsoft.Extensions.DependencyInjection;

using Source.Common.Commands;
using Source.Common.Engine;
using Source.Common.Formats.Keyvalues;

namespace Source.Common;

public enum PluginResult
{
	Continue,
	Override,
	Stop
}

public enum QueryCvarValueStatus
{
	ValueIntact,
	CvarNotFound,
	NotACvar,
	CvarProtected
}

public interface IServerPluginCallbacks
{
	bool Load(IServiceCollection interfaceFactory);
	// Called when the plugin should be shutdown
	void Unload();
	// called when a plugins execution is stopped but the plugin is not unloaded
	void Pause();
	// called when a plugin should start executing again (sometime after a Pause() call)
	void UnPause();
	// Returns string describing current plugin.  e.g., Admin-Mod.  
	ReadOnlySpan<char> GetPluginDescription();
	// Called any time a new level is started (after GameInit() also on level transitions within a game)
	void LevelInit(ReadOnlySpan<char> mapName);
	// The server is about to activate
	void ServerActivate(Span<Edict> edictList, int clientMax);
	// The server should run physics/think on all edicts
	void GameFrame(bool simulating);
	// Called when a level is shutdown (including changing levels)
	void LevelShutdown();
	// Client is going active
	void ClientActive(Edict entity);
	// Client is disconnecting from server
	void ClientDisconnect(Edict entity);
	// Client is connected and should be put in the game
	void ClientPutInServer(Edict entity, ReadOnlySpan<char> playername);
	// Sets the client index for the client who typed the command into their console
	void SetCommandClient(int index);
	// A player changed one/several replicated cvars (name etc)
	void ClientSettingsChanged(Edict pEdict);
	// Client is connecting to server ( set retVal to false to reject the connection )
	//	You can specify a rejection message by writing it into reject
	PluginResult ClientConnect(ref bool allowConnect, Edict entity, ReadOnlySpan<char> name, ReadOnlySpan<char> address, Span<char> reject);
	// The client has typed a command at the console
	PluginResult ClientCommand(Edict entity, in TokenizedCommand args);
	// A user has had their network id setup and validated 
	PluginResult NetworkIDValidated(ReadOnlySpan<char> pszUserName, ReadOnlySpan<char> networkID);
	// This is called when a query from IServerPluginHelpers::StartQueryCvarValue is finished.
	// iCookie is the value returned by IServerPluginHelpers::StartQueryCvarValue.
	// Added with version 2 of the interface.
	void OnQueryCvarValueFinished(QueryCvarCookie_t iCookie, Edict playerEntity, QueryCvarValueStatus status, ReadOnlySpan<char> cvarName, ReadOnlySpan<char> cvarValue);
	// added with version 3 of the interface.
	void OnEdictAllocated(Edict edict);
	void OnEdictFreed(Edict edict);
}

public enum DialogType {
	Msg,
	Menu,
	Text,
	Entry,
	AskConnect
}

public interface IServerPluginHelpers
{
	void CreateMessage(Edict entity, DialogType type, KeyValues data, IServerPluginCallbacks plugin) ;
	void ClientCommand(Edict entity, ReadOnlySpan<char> cmd ) ;
	QueryCvarCookie_t StartQueryCvarValue(Edict entity, ReadOnlySpan<char> pName ) ;
}
