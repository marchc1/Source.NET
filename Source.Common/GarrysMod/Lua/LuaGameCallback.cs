namespace Source.Common.GarrysMod;

public static partial class Lua
{
	public interface ILuaGameCallback {
		public struct LuaError() {
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
	}
}
