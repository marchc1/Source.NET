#if CLIENT_DLL || GAME_DLL
using Source;
using Source.Common.GarrysMod.Lua;

#if CLIENT_DLL
namespace Game.Client.GarrysMod;
#else
namespace Game.Server.GarrysMod;
#endif

public static class LuaHelper
{
	public static bool IsInErrorCB;

	public static int cvttsd2si(double value) => double.IsNaN(value) || value >= 2147483648.0 || value <= -2147483649.0 ? int.MinValue : (int)value;

	public static long cvttsd2si64(double value) => double.IsNaN(value) || value >= 9223372036854775808.0 || value < -9223372036854775808.0 ? long.MinValue : (long)value;

	public static void CallOnLuaErrorHook(in LuaError error, string? addonTitle, ulong workshopID) {
		if (g_Lua == null || g_Lua.Global() == null)
			return;

		if (IsInErrorCB) {
			Warning($"Error during OnLuaError on {error.Side}!\n");
			return;
		}

		LuaObject hook = new();
		g_Lua.Global().GetMember("hook", hook);
		if (!hook.isTable()) {
			hook.UnReference();
			return;
		}

		LuaObject run = new();
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

		LuaTable stack = new();
		for (int i = 0; i < error.Stack.Count;) {
			LuaTable entry = new();
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
#endif
