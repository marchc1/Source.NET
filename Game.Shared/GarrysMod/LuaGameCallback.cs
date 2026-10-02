#if CLIENT_DLL || GAME_DLL
using Source;
using Source.Common;
using Source.Common.GarrysMod.Lua;

#if CLIENT_DLL
namespace Game.Client.GarrysMod;
#else
namespace Game.Server.GarrysMod;
#endif

public class LuaGameCallback : ILuaGameCallback
{
	static readonly Color cMsgColor = new(156, 241, 255, 255);

	public ILuaObject CreateLuaObject() => new LuaObject();

	public void DestroyLuaObject(ILuaObject obj) => obj?.UnReference();

	public void ErrorPrint(ReadOnlySpan<char> error, bool print) => throw new NotImplementedException();

	public void Msg(ReadOnlySpan<char> msg, bool useless) => MsgColour(msg, in cMsgColor);

	public void MsgColour(ReadOnlySpan<char> msg, in Color color) => Dbg._ColorSpewMessage(SpewType.Message, in color, "%s", msg.ToString());

	public void LuaError(in LuaError error) => throw new NotImplementedException();

	public void InterfaceCreated(ILuaInterface iface) { }
}
#endif
