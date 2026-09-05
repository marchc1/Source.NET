namespace Source.Common.GarrysMod;

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

public static partial class Lua
{
	public interface ILuaGameCallback
	{

	}
}
