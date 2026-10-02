namespace Source.Common.GarrysMod.Lua;

public struct LuaError()
{
	public struct StackEntry()
	{
		public string Source = "";
		public string Function = "";
		public int Line;
	}

	public string Message = "";
	public string Side = "";
	public List<StackEntry> Stack = [];
}

public interface ILuaGameCallback
{
	ILuaObject CreateLuaObject();
	void DestroyLuaObject(ILuaObject obj);

	void ErrorPrint(ReadOnlySpan<char> error, bool print);

	void Msg(ReadOnlySpan<char> msg, bool useless);
	void MsgColour(ReadOnlySpan<char> msg, in Color color);

	void LuaError(in LuaError error);

	void InterfaceCreated(ILuaInterface iface);
}
