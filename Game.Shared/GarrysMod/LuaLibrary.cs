#if CLIENT_DLL || GAME_DLL
using Source.Common.GarrysMod.Lua;

#if CLIENT_DLL
namespace Game.Client.GarrysMod;
#else
namespace Game.Server.GarrysMod;
#endif

public class LuaLibraryFunction
{
	public string? Name;
	// public string? Unknown1;
	public CFunc? Function;
}

public class LuaLibrary(string name) : LuaUser
{
	public string Name = name;
	public List<LuaLibraryFunction> Functions = [];

	public void Add(LuaLibraryFunction func) {
		Functions.Add(func);
		SetUsingLua(true);
	}

	public override void InitLibraries(ILuaInterface lua) {
		LuaTable table = new(Name, 0);
		for (int i = 0; i < Functions.Count; i++)
			table.SetMember(Functions[i].Name, Functions[i].Function!);
		table.UnReference();
	}
}

public class LuaGlobalLibrary() : LuaLibrary("GLOBAL")
{
	static LuaGlobalLibrary? Factory;

	public static LuaGlobalLibrary GetGlobalLuaLibraryFactory() => Factory ??= new();

	public static LuaLibraryFunction Add(string name, CFunc function) {
		LuaLibraryFunction func = new() { Name = name, Function = function };
		GetGlobalLuaLibraryFactory().Add(func);
		return func;
	}

	public override void InitLibraries(ILuaInterface lua) {
		for (int i = 0; i < Functions.Count; i++)
			lua.Global().SetMember(Functions[i].Name, Functions[i].Function!);
	}
}
#endif
