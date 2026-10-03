#if CLIENT_DLL || GAME_DLL
using Source.Common.GarrysMod.Lua;

#if CLIENT_DLL
namespace Game.Client.GarrysMod;
#else
namespace Game.Server.GarrysMod;
#endif

public static class LuaUtil
{
	static readonly LuaLibrary LL_Factory_util = new("util");

	static LuaLibraryFunction Add(string name, CFunc function) {
		LuaLibraryFunction func = new() { Name = name, Function = function };
		LL_Factory_util.Add(func);
		return func;
	}

	static readonly LuaLibraryFunction fectory__util__NetworkIDToString = Add("NetworkIDToString", NetworkIDToString);
	static readonly LuaLibraryFunction fectory__util__NetworkStringToID = Add("NetworkStringToID", NetworkStringToID);
#if GAME_DLL
	static readonly LuaLibraryFunction fectory__util__AddNetworkString = Add("AddNetworkString", AddNetworkString);
#endif

	static int NetworkIDToString(ILuaInterface lua) {
		string? str = NetworkString.Convert(LuaHelper.cvttsd2si(g_Lua!.CheckNumber(1)));
		if (str == null)
			return 0;

		g_Lua.PushString(str);
		return 1;
	}

	static int NetworkStringToID(ILuaInterface lua) {
		g_Lua!.PushNumber(NetworkString.Get(g_Lua.CheckString(1)));
		return 1;
	}

#if GAME_DLL
	static int AddNetworkString(ILuaInterface lua) {
		g_Lua!.PushNumber(NetworkString.Add(g_Lua.CheckString(1)));
		return 1;
	}
#endif
}
#endif
