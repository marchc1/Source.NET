#if CLIENT_DLL || GAME_DLL
using Source.Common.Commands;
using Source.Common.GarrysMod.Lua;

#if CLIENT_DLL
namespace Game.Client.GarrysMod;
#else
namespace Game.Server.GarrysMod;
#endif

public static class LuaConVar
{
	public static readonly LuaClass LC_ConVar = new("ConVar", LuaType.ConVar, null, null);

	static readonly LuaClassFunction ConVar___tostring__Factory = LC_ConVar.Add("__tostring", ConVar____tostring);
	static readonly LuaClassFunction ConVar_GetName__Factory = LC_ConVar.Add("GetName", ConVar__GetName);
	static readonly LuaClassFunction ConVar_GetDefault__Factory = LC_ConVar.Add("GetDefault", ConVar__GetDefault);
	static readonly LuaClassFunction ConVar_GetHelpText__Factory = LC_ConVar.Add("GetHelpText", ConVar__GetHelpText);
	static readonly LuaClassFunction ConVar_GetString__Factory = LC_ConVar.Add("GetString", ConVar__GetString);
	static readonly LuaClassFunction ConVar_GetFloat__Factory = LC_ConVar.Add("GetFloat", ConVar__GetFloat);
	static readonly LuaClassFunction ConVar_GetInt__Factory = LC_ConVar.Add("GetInt", ConVar__GetInt);
	static readonly LuaClassFunction ConVar_GetBool__Factory = LC_ConVar.Add("GetBool", ConVar__GetBool);
	static readonly LuaClassFunction ConVar_SetString__Factory = LC_ConVar.Add("SetString", ConVar__SetString);
	static readonly LuaClassFunction ConVar_SetFloat__Factory = LC_ConVar.Add("SetFloat", ConVar__SetFloat);
	static readonly LuaClassFunction ConVar_SetInt__Factory = LC_ConVar.Add("SetInt", ConVar__SetInt);
	static readonly LuaClassFunction ConVar_SetBool__Factory = LC_ConVar.Add("SetBool", ConVar__SetBool);
	static readonly LuaClassFunction ConVar_GetFlags__Factory = LC_ConVar.Add("GetFlags", ConVar__GetFlags);
	static readonly LuaClassFunction ConVar_IsFlagSet__Factory = LC_ConVar.Add("IsFlagSet", ConVar__IsFlagSet);
	static readonly LuaClassFunction ConVar_Revert__Factory = LC_ConVar.Add("Revert", ConVar__Revert);
	static readonly LuaClassFunction ConVar_GetMax__Factory = LC_ConVar.Add("GetMax", ConVar__GetMax);
	static readonly LuaClassFunction ConVar_GetMin__Factory = LC_ConVar.Add("GetMin", ConVar__GetMin);

	static readonly LuaLibraryFunction worker__GLobal__GetConVar_Internal = LuaGlobalLibrary.Add("GetConVar_Internal", GetConVar_Internal);
	static readonly LuaLibraryFunction worker__GLobal__CreateConVar = LuaGlobalLibrary.Add("CreateConVar", CreateConVar);
	static readonly LuaLibraryFunction worker__GLobal__ConVarExists = LuaGlobalLibrary.Add("ConVarExists", ConVarExists);

	static readonly string[] s_BannedInfo = [
		"rcon_password",
		"sv_password",
		"password",
		"tv_password",
		"tv_relaypassword",
		"rcon_address",
		"lua_error_url",
	];

	public static void Push_ConVar(ConVar? convar) => LC_ConVar.Push(convar);

	static ConVar? Get_ConVar(int stackPos) => (ConVar?)LC_ConVar.Get(stackPos);

	static void CheckLuaConVar(ConVar convar) {
#if CLIENT_DLL
		if (!convar.IsFlagSet(FCvar.LuaClient))
#else
		if (!convar.IsFlagSet(FCvar.LuaServer))
#endif
			g_Lua!.ArgError(1, "attempted to modify ConVar not created by Lua");
	}

	public static bool IsAllowedToGetConvarInfo(ReadOnlySpan<char> name) {
		foreach (string banned in s_BannedInfo) {
			if (stricmp(banned, name) == 0)
				return false;
		}
		return true;
	}

	static bool IsValidConsoleName(ReadOnlySpan<char> name) {
		foreach (char c in name) {
			if (((c & ~0x20) - 'A') is >= 0 and <= 25)
				continue;
			if (c is >= '0' and <= '9')
				continue;
			if (c is '+' or '-' or '.' or '^' or '_' or '!' or '~')
				continue;
			return false;
		}
		return true;
	}

	public static bool ShouldPushConVar(ConVar? convar) {
		if (convar == null)
			return false;

		bool lua = convar.IsFlagSet(FCvar.LuaClient) || convar.IsFlagSet(FCvar.LuaServer);
		if (convar.IsFlagSet(FCvar.Hidden) || convar.IsFlagSet(FCvar.DevelopmentOnly) || convar.IsFlagSet(FCvar.Unregistered))
			return lua;

		return true;
	}

	static int ConVar____tostring(ILuaInterface lua) {
		ConVar? convar = Get_ConVar(1);
		if (convar == null) {
			g_Lua!.PushString("ConVar [NULL]");
			return 1;
		}

		string str = $"ConVar [{convar.GetName()}]";
		g_Lua!.PushString(str.Length > 0x1FF ? str[..0x1FF] : str);
		return 1;
	}

	static int ConVar__GetName(ILuaInterface lua) {
		ConVar? convar = Get_ConVar(1);
		if (convar == null)
			g_Lua!.Error("Tried to use a NULL ConVar!");

		g_Lua!.PushString(convar.GetName());
		return 1;
	}

	static int ConVar__GetDefault(ILuaInterface lua) {
		ConVar? convar = Get_ConVar(1);
		if (convar == null)
			g_Lua!.Error("Tried to use a NULL ConVar!");

		g_Lua!.PushString(convar.GetDefault());
		return 1;
	}

	static int ConVar__GetHelpText(ILuaInterface lua) {
		ConVar? convar = Get_ConVar(1);
		if (convar == null)
			g_Lua!.Error("Tried to use a NULL ConVar!");

		g_Lua!.PushString(convar.GetHelpText());
		return 1;
	}

	static int ConVar__GetString(ILuaInterface lua) {
		ConVar? convar = Get_ConVar(1);
		if (convar == null)
			g_Lua!.Error("Tried to use a NULL ConVar!");

		if ((convar.GetFlags() & FCvar.NeverAsString) != 0)
			g_Lua!.PushString("FCVAR_NEVER_AS_STRING");
		else
			g_Lua!.PushString(convar.GetString());
		return 1;
	}

	static int ConVar__GetFloat(ILuaInterface lua) {
		ConVar? convar = Get_ConVar(1);
		if (convar == null)
			g_Lua!.Error("Tried to use a NULL ConVar!");

		g_Lua!.PushNumber(convar.GetFloat());
		return 1;
	}

	static int ConVar__GetInt(ILuaInterface lua) {
		ConVar? convar = Get_ConVar(1);
		if (convar == null)
			g_Lua!.Error("Tried to use a NULL ConVar!");

		g_Lua!.PushNumber(convar.GetInt());
		return 1;
	}

	static int ConVar__GetBool(ILuaInterface lua) {
		ConVar? convar = Get_ConVar(1);
		if (convar == null)
			g_Lua!.Error("Tried to use a NULL ConVar!");

		g_Lua!.PushBool(convar.GetInt() != 0);
		return 1;
	}

	static int ConVar__SetString(ILuaInterface lua) {
		ConVar? convar = Get_ConVar(1);
		if (convar == null)
			g_Lua!.Error("Tried to use a NULL ConVar!");

		CheckLuaConVar(convar);
		convar.SetValue(g_Lua!.CheckString(2));
		return 0;
	}

	static int ConVar__SetFloat(ILuaInterface lua) {
		ConVar? convar = Get_ConVar(1);
		if (convar == null)
			g_Lua!.Error("Tried to use a NULL ConVar!");

		CheckLuaConVar(convar);
		convar.SetValue((float)g_Lua!.CheckNumber(2));
		return 0;
	}

	static int ConVar__SetInt(ILuaInterface lua) {
		ConVar? convar = Get_ConVar(1);
		if (convar == null)
			g_Lua!.Error("Tried to use a NULL ConVar!");

		CheckLuaConVar(convar);
		convar.SetValue(LuaHelper.cvttsd2si(g_Lua!.CheckNumber(2)));
		return 0;
	}

	static int ConVar__SetBool(ILuaInterface lua) {
		ConVar? convar = Get_ConVar(1);
		if (convar == null)
			g_Lua!.Error("Tried to use a NULL ConVar!");

		CheckLuaConVar(convar);
		convar.SetValue(g_Lua!.GetBool(2) ? 1 : 0);
		return 0;
	}

	static int ConVar__GetFlags(ILuaInterface lua) {
		ConVar? convar = Get_ConVar(1);
		if (convar == null)
			g_Lua!.Error("Tried to use a NULL ConVar!");

		g_Lua!.PushNumber((int)convar.GetFlags());
		return 1;
	}

	static int ConVar__IsFlagSet(ILuaInterface lua) {
		ConVar? convar = Get_ConVar(1);
		if (convar == null)
			g_Lua!.Error("Tried to use a NULL ConVar!");

		int flag = LuaHelper.cvttsd2si(g_Lua!.CheckNumber(2));
		g_Lua.PushBool(((int)convar.GetFlags() & flag) != 0);
		return 1;
	}

	static int ConVar__Revert(ILuaInterface lua) {
		ConVar? convar = Get_ConVar(1);
		if (convar == null)
			g_Lua!.Error("Tried to use a NULL ConVar!");

		CheckLuaConVar(convar);
		convar.Revert();
		return 0;
	}

	static int ConVar__GetMax(ILuaInterface lua) {
		ConVar? convar = Get_ConVar(1);
		if (convar == null)
			g_Lua!.Error("Tried to use a NULL ConVar!");

		if (!convar.GetMax(out double max))
			return 0;

		g_Lua!.PushNumber((float)max);
		return 1;
	}

	static int ConVar__GetMin(ILuaInterface lua) {
		ConVar? convar = Get_ConVar(1);
		if (convar == null)
			g_Lua!.Error("Tried to use a NULL ConVar!");

		if (!convar.GetMin(out double min))
			return 0;

		g_Lua!.PushNumber((float)min);
		return 1;
	}

	static int ConVarExists(ILuaInterface lua) {
		if (stricmp(g_Lua!.CheckString(1), "maxplayers") == 0) {
			g_Lua.PushBool(true);
			return 1;
		}

		ConVar? convar = cvar.FindVar(g_Lua.CheckString(1));
		if (ShouldPushConVar(convar))
			g_Lua.PushBool(convar != null);
		else
			g_Lua.PushBool(false);
		return 1;
	}

	static int CreateConVar(ILuaInterface lua) {
		string name = g_Lua!.CheckString(1);
		if (!IsAllowedToGetConvarInfo(name) || LuaConCommands.ConCommand_IsBlocked(name) != null) {
			g_Lua.ErrorFromLua($"CreateConVar: ConVar name is blocked! ({name})");
			return 0;
		}

		if (cvar.FindCommand(name) != null) {
			g_Lua.ErrorFromLua($"CreateConVar: Cannot override an existing console command! ({name})");
			return 0;
		}

		if (strlen(name) <= 1) {
			g_Lua.ErrorFromLua($"CreateConVar: ConVar name is too short! ({name})");
			return 0;
		}

		if (!IsValidConsoleName(name)) {
			g_Lua.ErrorFromLua($"CreateConVar: Invalid ConVar name! ({name})");
			return 0;
		}

		string defaultValue = g_Lua.CheckString(2);

		int flags = g_Lua.GetFlags(3);
		if ((flags & (int)FCvar.DontRecord) == 0)
			flags |= (int)FCvar.Demo;

		string? helpString = null;
		if (g_Lua.GetType(4) == LuaType.String)
			helpString = g_Lua.CheckString(4);
		else if (g_Lua.GetType(4) != LuaType.Nil)
			lua.ErrorFromLua($"bad argument #4 to CreateConVar (string expected, got {lua.GetTypeName(lua.GetType(4))})");

		bool hasMin = false;
		float min = 0.0f;
		if (g_Lua.GetType(5) == LuaType.Number) {
			min = (float)g_Lua.CheckNumber(5);
			hasMin = true;
		}
		else if (g_Lua.GetType(5) != LuaType.Nil)
			lua.ErrorFromLua($"bad argument #5 to CreateConVar (number expected, got {lua.GetTypeName(lua.GetType(5))})");

		bool hasMax = false;
		float max = 0.0f;
		if (g_Lua.GetType(6) == LuaType.Number) {
			max = (float)g_Lua.CheckNumber(6);
			hasMax = true;
		}
		else if (g_Lua.GetType(6) != LuaType.Nil)
			lua.ErrorFromLua($"bad argument #6 to CreateConVar (number expected, got {lua.GetTypeName(lua.GetType(6))})");

		ConVar? existing = cvar.FindVar(name);
		if (existing != null) {
			if (!ShouldPushConVar(existing))
				return 0;
			Push_ConVar(existing);
			return 1;
		}

		ConVar convar = g_Lua.CreateConVar(name, defaultValue, helpString, flags);
		convar.SetMin(hasMin, min);
		convar.SetMax(hasMax, max);
		if ((flags & (int)FCvar.UserInfo) != 0) {
			// todo: cvar.CallGlobalChangeCallbacks(convar, "", 0.0f);
		}

		Push_ConVar(convar);
		return 1;
	}

	static int GetConVar_Internal(ILuaInterface lua) {
		string name = g_Lua!.CheckString(1);
		if (!IsAllowedToGetConvarInfo(name) || stricmp("con_logfile", name) == 0)
			return 0;

		ConVar? convar = cvar.FindVar(name);
		if (convar == null || !ShouldPushConVar(convar))
			return 0;

		Push_ConVar(convar);
		return 1;
	}
}
#endif
