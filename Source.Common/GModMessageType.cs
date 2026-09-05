#if GMOD_DLL
namespace Source.Common;
public enum GModMessageType
{
	/// <summary> <b>[Server <- -> Client]</b> A Lua net message </summary>
	NetMessage,
	/// <summary> <b>[Server -> Client]</b> Lua file changed on the server, client is given this to auto-refresh it </summary>
	LuaAutoRefresh,
	/// <summary> <b>[Client -> Server]</b> A lua error occured, and is sent to the server </summary>
	LuaError,
	/// <summary> <b>[Server -> Client]</b> Server asks the client which files are needed by the client </summary>
	RequestLuaFiles,
	/// <summary> <b>[Client <- -> Server]</b> The client tells the server which files it needs, and the server sends them back. </summary>
	LuaFile
}
#endif
