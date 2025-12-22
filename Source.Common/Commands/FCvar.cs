// for docs purposes
using Source.Common.Client;
using Source.Common.Server;
using Source.Common.Networking;

namespace Source.Common.Commands;

public enum CmdExecutionMarker
{
	EnableServerCanExecute = 'a',
	DisableServerCanExecute = 'b',
	EnableClientCmdCanExecute = 'c',
	DisableClientCmdCanExecute = 'd'
}

[Flags]
public enum FCvar : int
{
	/// <summary>
	/// The default, no flags at all.
	/// </summary>
	None = 0,

	/// <summary>
	/// If this is set, don't add to linked list, etc.
	/// </summary>
	Unregistered = 1 << 0,

	/// <summary>
	/// Hidden in released products. Flag is removed automatically if ALLOW_DEVELOPMENT_CVARS is defined.
	/// </summary>
	DevelopmentOnly = 1 << 1,

	/// <summary>
	/// Defined by the game DLL.
	/// </summary>
	GameDLL = 1 << 2,

	/// <summary>
	/// Defined by the client DLL.
	/// </summary>
	ClientDLL = 1 << 3,

	/// <summary>
	/// Hidden. Doesn't appear in find or autocomplete. Like <see cref="FCvar.DevelopmentOnly"/>, but can't be compiled out.
	/// </summary>
	Hidden = 1 << 4,

	/// <summary>
	/// It's a server cvar, but we don't send the data since it's a password, etc.
	/// <br/>
	/// Sends 1 if it's not blank/zero, 0 otherwise as value.
	/// </summary>
	Protected = 1 << 5,

	/// <summary>
	/// This cvar cannot be changed by clients connected to a multiplayer server.
	/// </summary>
	SingleplayerOnly = 1 << 6,

	/// <summary>
	/// Set to cause it to be saved to vars.rc.
	/// </summary>
	Archive = 1 << 7,

	/// <summary>
	/// Notifies players when changed.
	/// </summary>
	Notify = 1 << 8,

	/// <summary>
	/// Changes the client's info string.
	/// </summary>
	UserInfo = 1 << 9,

	/// <summary>
	/// This cvar's string cannot contain unprintable characters (e.g., used for player name, etc).
	/// </summary>
	PrintableOnly = 1 << 10,

	/// <summary>
	/// If this is a FCVAR_SERVER, don't log changes to the log file / console if we are creating a log.
	/// </summary>
	Unlogged = 1 << 11,

	/// <summary>
	/// Never try to print that cvar.
	/// </summary>
	NeverAsString = 1 << 12,

	/// <summary>
	/// It's a ConVar that's shared between the client and the server.
	/// <br/>
	/// <br/>
	/// At signon, the values of all such ConVars are sent from the server to the client (skipped for local client).
	/// <br/>
	/// If a change is requested it must come from the console (i.e., no remote client changes).
	/// <br/>
	/// If a value is changed while a server is active, it's replicated to all connected clients.
	/// <br/>
	/// <br/>
	/// Server setting enforced on clients.
	/// </summary>
	Replicated = 1 << 13,

	/// <summary>
	/// Only usable in singleplayer / debug / multiplayer with sv_cheats enabled.
	/// </summary>
	Cheat = 1 << 14,

	/// <summary>
	/// This var isn't archived, but is exposed to players—and its use is allowed in competitive play.
	/// </summary>
	InternalUse = 1 << 15,

	/// <summary>
	/// Record this cvar when starting a demo file.
	/// </summary>
	Demo = 1 << 16,

	/// <summary>
	/// Don't record this command in demo files.
	/// </summary>
	DontRecord = 1 << 17,

#if GMOD_DLL
	/// <summary>
	/// Lua client convar (Garry's Mod).
	/// </summary>
	LuaClient = 1 << 18,

	/// <summary>
	/// Lua server convar (Garry's Mod).
	/// </summary>
	LuaServer = 1 << 19,
#endif

	/// <summary>
	/// If this cvar changes, it forces a material reload.
	/// </summary>
	ReloadMaterials = 1 << 20,

	/// <summary>
	/// If this cvar changes, it forces a texture reload.
	/// </summary>
	ReloadTextures = 1 << 21,

	/// <summary>
	/// Cvar cannot be changed by a client that is connected to a server.
	/// </summary>
	NotConnected = 1 << 22,

	/// <summary>
	/// Indicates this cvar is read from the material system thread.
	/// </summary>
	MaterialSystemThread = 1 << 23,

	/// <summary>
	/// Used as a debugging tool necessary to check material system thread convars.
	/// </summary>
	AccessibleFromThreads = 1 << 25,

	/// <summary>
	/// The server is allowed to execute this command on clients via
	/// ClientCommand / <see cref="NET_StringCmd"/> / BaseClientState.ProcessStringCmd.
	/// </summary>
	ServerCanExecute = 1 << 28,

	/// <summary>
	/// If this is set, then the server is not allowed to query this cvar's value
	/// (via <see cref="IServerPluginHelpers.StartQueryCvarValue(Engine.Edict, ReadOnlySpan{char})"/>.
	/// </summary>
	ServerCannotQuery = 1 << 29,

	/// <summary>
	/// IEngineClient.ClientCmd is allowed to execute this command.
	/// <br/>
	/// Note: <see cref="IEngineClient.ClientCmd_Unrestricted(ReadOnlySpan{char})"/> can run any client command.
	/// </summary>
	ClientCmdCanExecute = 1 << 30,

	/// <summary>
	/// "-default" causes a lot of commands to be ignored (but still be recorded as though they had run).
	/// <br/>
	/// This causes them to be executed anyway.
	/// </summary>
	ExecDespiteDefault = 1 << 31,
}
