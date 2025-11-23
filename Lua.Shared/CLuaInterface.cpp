#include "CLuaInterface.h"

// CLuaInterface things

ILuaGameCallback::CLuaError* ReadStackIntoError(lua_State* L)
{
	// VPROF ReadStackIntoError GLua

	int level = 0;
	lua_Debug ar;
	ILuaGameCallback::CLuaError* lua_error = new ILuaGameCallback::CLuaError;
	while (lua_getstack(L, level, &ar)) {
		lua_getinfo(L, "nSl", &ar);

		lua_error->stack.emplace_back();
		ILuaGameCallback::CLuaError::StackEntry& entry = lua_error->stack.back();
		entry.source = ar.source ? ar.source : "unknown";
		entry.function = ar.name ? ar.name : "unknown";

		entry.line = ar.currentline;

		++level;
	}

	const char* str = lua_tolstring(L, -1, NULL);
	if (str != NULL) // Setting a std::string to NULL causes a crash. Don't care.
		lua_error->message = str;

	CLuaInterface* LUA = (CLuaInterface*)L->luabase;
	lua_error->side = LUA->IsClient() ? "client" : (LUA->IsMenu() ? "menu" : "server");

	return lua_error;
}

int AdvancedLuaErrorReporter(lua_State *L) 
{
	// VPROF AdvancedLuaErrorReporter GLua

	if (lua_isstring(L, 1)) {
		// const char* str = lua_tostring(L, 1);

		// g_LastError.assign(str);

		CLuaInterface* LUA = (CLuaInterface*)L->luabase;
		lua_pushvalue(L, 1);
		LUA->GetLuaGameCallback()->LuaError(ReadStackIntoError(L));
		lua_pop(L, 1);

		// lua_pushstring(L, g_LastError.c_str());
	}

	return 0;
}