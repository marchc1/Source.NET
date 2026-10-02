global using static Game.MenuSystem.LuaGlobals;

using Source;
using Source.Common.GarrysMod.Lua;

namespace Game.MenuSystem;

public static class LuaGlobals
{
	public static ILuaInterface? g_Lua;
}

public static class LuaHelper
{
	public static bool IsInErrorCB;

	static ILuaObject NewTable(ILuaInterface lua) {
		ILuaObject table = lua.CreateObject();
		lua.PreCreateTable(0, 0);
		table.SetReference(-1);
		lua.Pop(1);
		return table;
	}

	public static void CallOnLuaErrorHook(in LuaError error, string? addonTitle, ulong workshopID) {
		if (g_Lua == null || g_Lua.Global() == null)
			return;

		if (IsInErrorCB) {
			Warning($"Error during OnLuaError on {error.Side}!\n");
			return;
		}

		ILuaObject hook = g_Lua.CreateObject();
		g_Lua.Global().GetMember("hook", hook);
		if (!hook.isTable()) {
			hook.UnReference();
			return;
		}

		ILuaObject run = g_Lua.CreateObject();
		hook.GetMember("Run", run);
		if (!run.isFunction()) {
			run.UnReference();
			hook.UnReference();
			return;
		}

		run.Push();
		g_Lua.PushString("OnLuaError");
		g_Lua.PushString(error.Message);
		g_Lua.PushString(error.Side);

		ILuaObject stack = NewTable(g_Lua);
		for (int i = 0; i < error.Stack.Count;) {
			ILuaObject entry = NewTable(g_Lua);
			entry.SetMember("File", error.Stack[i].Source);
			entry.SetMember("Line", (float)error.Stack[i].Line);
			entry.SetMember("Function", error.Stack[i].Function);
			i++;
			stack.SetMember((float)i, entry);
			entry.UnReference();
		}

		stack.Push();
		IsInErrorCB = true;

		if (addonTitle != null) {
			g_Lua.PushString(addonTitle);
			g_Lua.PushString(workshopID.ToString());
			g_Lua.CallInternalNoReturns(6);
		}
		else
			g_Lua.CallInternalNoReturns(4);

		IsInErrorCB = false;

		stack.UnReference();
		run.UnReference();
		hook.UnReference();
	}
}
