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

}
