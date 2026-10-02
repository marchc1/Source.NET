// Credit for initial bindings: https://github.com/tilkinsc/Lua.NET/blob/4fa0733656b6e1d55ebd2e3cef5b1add11b80997/src/LuaJIT.cs

using System.Runtime.InteropServices;

using size_t = System.UInt64;
using lua_Number = System.Double;
using lua_Integer = System.Int64;

using Source.Common.GarrysMod.Lua;
using System.Runtime.InteropServices.Marshalling;

namespace Game.Lua;

public static unsafe partial class Lua
{
	private const string DllName = "lua51";


	public delegate int lua_CFunction(lua_State L);
	public delegate nint lua_Reader(lua_State L, nuint ud, ref size_t sz);
	public delegate int lua_Writer(lua_State L, nuint p, size_t sz, nuint ud);
	public delegate nuint lua_Alloc(nuint ud, nuint ptr, size_t osize, size_t nsize);
	// FIX: native hooks receive a lua_Debug*, not a struct by value.
	// Use LuaDebugMarshaller.FromPointer(ar) inside the hook to get a managed lua_Debug.
	public delegate void lua_Hook(lua_State L, LuaDebugMarshaller.Native* ar);

	public static luaL_Reg AsLuaLReg(string name, delegate* unmanaged<lua_State, int> func) => new() { name = name, func = (nint)func };

	public const string LUAJIT_VERSION = "LuaJIT 2.1.0-beta3";
	public const int LUAJIT_VERSION_NUM = 20100;
	public const string LUAJIT_VERSION_SYM = "luaJIT_version_2_1_0_beta3";
	public const string LUAJIT_COPYRIGHT = "Copyright (C) 2005-2022 Mike Pall";
	public const string LUAJIT_URL = "https://luajit.org/";

	public const int LUAJIT_MODE_MASK = 0x00FF;

	public const int LUAJIT_MODE_ENGINE = 0;
	public const int LUAJIT_MODE_DEBUG = 1;
	public const int LUAJIT_MODE_FUNC = 2;
	public const int LUAJIT_MODE_ALLFUNC = 3;
	public const int LUAJIT_MODE_ALLSUBFUNC = 4;
	public const int LUAJIT_MODE_TRACE = 5;
	public const int LUAJIT_MODE_WRAPCFUNC = 0x10;
	public const int LUAJIT_MODE_MODE_MAX = 0x11;

	public const int LUAJIT_MODE_OFF = 0x0000;
	public const int LUAJIT_MODE_ON = 0x0100;
	public const int LUAJIT_MODE_FLUSH = 0x0200;

	public delegate void luaJIT_profile_callback(nuint data, lua_State L, int samples, int vmstate);

	[LibraryImport(DllName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller))]
	public static partial void luaJIT_profile_start(lua_State L, string mode, luaJIT_profile_callback cb, nuint data);

	[LibraryImport(DllName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller))]
	public static partial void luaJIT_profile_stop(lua_State L);

	[LibraryImport(DllName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller), EntryPoint = "luaJIT_profile_dumpstack")]
	private static partial nint _luaJIT_profile_dumpstack(lua_State L, string fmt, int depth, ref size_t len);
	public static string? luaJIT_profile_dumpstack(lua_State L, string fmt, int depth, ref size_t len) {
		return Marshal.PtrToStringAnsi(_luaJIT_profile_dumpstack(L, fmt, depth, ref len));
	}

	[LibraryImport(DllName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller))]
	public static partial void luaJIT_version_2_1_0_beta3();

	public const string LUA_LDIR = "!\\lua\\";
	public const string LUA_CDIR = "!\\";

	public const string LUA_PATH_DEFAULT = ".\\?.lua;" + LUA_LDIR + "?.lua;" + LUA_LDIR + "?\\init.lua;";
	public const string LUA_CPATH_DEFAULT = ".\\?.dll;" + LUA_CDIR + "?.dll;" + LUA_CDIR + "loadall.dll";

	public const string LUA_PATH = "LUA_PATH";
	public const string LUA_CPATH = "LUA_CPATH";
	public const string LUA_INIT = "LUA_INIT";

	public const string LUA_DIRSEP = "\\";
	public const string LUA_PATHSEP = ";";
	public const string LUA_PATH_MARK = "?";
	public const string LUA_EXECDIR = "!";
	public const string LUA_IGMARK = "-";
	public const string LUA_PATH_CONFIG = LUA_DIRSEP + "\n" + LUA_PATHSEP + "\n" + LUA_PATH_MARK + "\n" + LUA_EXECDIR + "\n" + LUA_IGMARK + "\n";

	public static string LUA_QL(string x) {
		return "'" + x + "'";
	}

	public const string LUA_QS = "'%s'";

	public const int LUAI_MAXSTACK = 65500;
	public const int LUAI_MAXCSTACK = 8000;
	public const int LUAI_GCPAUSE = 200;
	public const int LUAI_GCMUL = 200;
	public const int LUA_MAXCAPTURES = 32;

	public const int LUA_IDSIZE = 60;

	// FIX: was hardcoded to 512 (Windows' BUFSIZ). The real value depends on the platform
	// lua51 was compiled for, and luaL_addchar must use the same value as the native code.
	public static int LUAL_BUFFERSIZE => luaL_Buffer.NativeBufferSize;

	public const string LUA_VERSION = "Lua 5.1";
	public const string LUA_RELEASE = "Lua 5.1.4";
	public const int LUA_VERSION_NUM = 501;
	public const string LUA_COPYRIGHT = "Copyright (C) 1994-2008 Lua.org, PUC-Rio";
	public const string LUA_AUTHORS = "R. Ierusalimschy, L. H. de Figueiredo, W. Celes";

	public const string LUA_SIGNATURE = "\x1bLua";

	public const int LUA_MULTRET = -1;

	public const int LUA_REGISTRYINDEX = -10000;
	public const int LUA_ENVIRONINDEX = -10001;
	public const int LUA_GLOBALSINDEX = -10002;

	public static int lua_upvalueindex(int i) {
		return LUA_GLOBALSINDEX - i;
	}

	public const int LUA_OK = 0;
	public const int LUA_YIELD = 1;
	public const int LUA_ERRRUN = 2;
	public const int LUA_ERRSYNTAX = 3;
	public const int LUA_ERRMEM = 4;
	public const int LUA_ERRERR = 5;

	public const int LUA_TNONE = -1;
	public const int LUA_TNIL = 0;
	public const int LUA_TBOOLEAN = 1;
	public const int LUA_TLIGHTUSERDATA = 2;
	public const int LUA_TNUMBER = 3;
	public const int LUA_TSTRING = 4;
	public const int LUA_TTABLE = 5;
	public const int LUA_TFUNCTION = 6;
	public const int LUA_TUSERDATA = 7;
	public const int LUA_TTHREAD = 8;

	public const int LUA_MINSTACK = 20;

	[LibraryImport(DllName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller), EntryPoint = "lua_newstate")]
	private static partial lua_State _lua_newstate(nint f, nuint ud);
	public static lua_State lua_newstate(lua_Alloc? f, nuint ud) {
		return _lua_newstate(f == null ? 0 : Marshal.GetFunctionPointerForDelegate(f), ud);
	}

	[LibraryImport(DllName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller))]
	public static partial void lua_close(lua_State L);

	[LibraryImport(DllName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller))]
	public static partial lua_State lua_newthread(lua_State L);

	[LibraryImport(DllName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller), EntryPoint = "lua_atpanic")]
	private static partial nint _lua_atpanic(lua_State L, nint panicf);
	public static lua_CFunction? lua_atpanic(lua_State L, lua_CFunction? panicf) {
		nint panic = _lua_atpanic(L, panicf == null ? 0 : Marshal.GetFunctionPointerForDelegate(panicf));
		return panic == 0 ? null : Marshal.GetDelegateForFunctionPointer<lua_CFunction>(panic);
	}

	[LibraryImport(DllName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller))]
	public static partial int lua_gettop(lua_State L);

	[LibraryImport(DllName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller))]
	public static partial void lua_settop(lua_State L, int idx);

	[LibraryImport(DllName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller))]
	public static partial void lua_pushvalue(lua_State L, int idx);

	[LibraryImport(DllName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller))]
	public static partial void lua_remove(lua_State L, int idx);

	[LibraryImport(DllName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller))]
	public static partial void lua_insert(lua_State L, int idx);

	[LibraryImport(DllName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller))]
	public static partial void lua_replace(lua_State L, int idx);

	[LibraryImport(DllName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller))]
	public static partial int lua_checkstack(lua_State L, int sz);

	[LibraryImport(DllName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller))]
	public static partial void lua_xmove(lua_State from, lua_State to, int n);

	[LibraryImport(DllName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller))]
	public static partial int lua_isnumber(lua_State L, int idx);

	[LibraryImport(DllName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller))]
	public static partial int lua_isstring(lua_State L, int idx);

	[LibraryImport(DllName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller))]
	public static partial int lua_iscfunction(lua_State L, int idx);

	[LibraryImport(DllName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller))]
	public static partial int lua_isuserdata(lua_State L, int idx);

	[LibraryImport(DllName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller))]
	public static partial int lua_type(lua_State L, int idx);

	[LibraryImport(DllName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller), EntryPoint = "lua_typename")]
	private static partial nint _lua_typename(lua_State L, int tp);
	public static string? lua_typename(lua_State L, int tp) {
		return Marshal.PtrToStringAnsi(_lua_typename(L, tp));
	}

	[LibraryImport(DllName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller))]
	public static partial int lua_equal(lua_State L, int idx1, int idx2);

	[LibraryImport(DllName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller))]
	public static partial int lua_rawequal(lua_State L, int idx1, int idx2);

	[LibraryImport(DllName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller))]
	public static partial int lua_lessthan(lua_State L, int idx1, int idx2);

	[LibraryImport(DllName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller))]
	public static partial lua_Number lua_tonumber(lua_State L, int idx);

	[LibraryImport(DllName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller))]
	public static partial lua_Integer lua_tointeger(lua_State L, int idx);

	[LibraryImport(DllName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller))]
	public static partial int lua_toboolean(lua_State L, int idx);

	[LibraryImport(DllName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller), EntryPoint = "lua_tolstring")]
	private static partial nint _lua_tolstring(lua_State L, int idx, ref ulong len);
	public static string? lua_tolstring(lua_State L, int idx, ref ulong len) {
		return Marshal.PtrToStringAnsi(_lua_tolstring(L, idx, ref len));
	}

	[LibraryImport(DllName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller))]
	public static partial ulong lua_objlen(lua_State L, int idx);

	[LibraryImport(DllName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller), EntryPoint = "lua_tocfunction")]
	private static partial nint _lua_tocfunction(lua_State L, int idx);
	public static lua_CFunction? lua_tocfunction(lua_State L, int idx) {
		nint ret = _lua_tocfunction(L, idx);
		return ret == 0 ? null : Marshal.GetDelegateForFunctionPointer<lua_CFunction>(ret);
	}

	[LibraryImport(DllName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller))]
	public static partial nuint lua_touserdata(lua_State L, int idx);

	[LibraryImport(DllName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller))]
	public static partial lua_State lua_tothread(lua_State L, int idx);

	[LibraryImport(DllName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller))]
	public static partial nuint lua_topointer(lua_State L, int idx);

	[LibraryImport(DllName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller))]
	public static partial void lua_pushnil(lua_State L);

	[LibraryImport(DllName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller))]
	public static partial void lua_pushnumber(lua_State L, lua_Number n);

	[LibraryImport(DllName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller))]
	public static partial void lua_pushinteger(lua_State L, lua_Integer n);

	[LibraryImport(DllName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller))]
	public static partial void lua_pushlstring(lua_State L, string s, size_t len);

	[LibraryImport(DllName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller))]
	public static partial void lua_pushstring(lua_State L, string s);

	// TODO:
	// [LibraryImport(DllName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller))]
	// public static partial nint lua_pushvfstring(lua_State L, string fmt, va_list argp);

	// TODO:
	// [LibraryImport(DllName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller), EntryPoint = "lua_pushfstring")]
	// private static partial nint _lua_pushfstring(lua_State L, string fmt, params string[] args);
	// public static string? lua_pushfstring(lua_State L, string fmt, params string[] args)
	// {
	// 	return Marshal.PtrToStringAnsi(_lua_pushfstring(L, fmt, args));
	// }

	[LibraryImport(DllName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller), EntryPoint = "lua_pushcclosure")]
	private static partial void _lua_pushcclosure(lua_State L, nint fn, int n);
	public static void lua_pushcclosure(lua_State L, lua_CFunction? fn, int n) {
		_lua_pushcclosure(L, fn == null ? 0 : Marshal.GetFunctionPointerForDelegate(fn), n);
	}

	[LibraryImport(DllName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller))]
	public static partial void lua_pushboolean(lua_State L, int b);

	[LibraryImport(DllName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller))]
	public static partial void lua_pushlightuserdata(lua_State L, nuint p);

	[LibraryImport(DllName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller))]
	public static partial int lua_pushthread(lua_State L);

	[LibraryImport(DllName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller))]
	public static partial void lua_gettable(lua_State L, int idx);

	[LibraryImport(DllName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller))]
	public static partial void lua_getfield(lua_State L, int idx, string k);

	[LibraryImport(DllName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller))]
	public static partial void lua_rawget(lua_State L, int idx);

	[LibraryImport(DllName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller))]
	public static partial void lua_rawgeti(lua_State L, int idx, int n);

	[LibraryImport(DllName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller))]
	public static partial void lua_createtable(lua_State L, int narr, int nrec);

	[LibraryImport(DllName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller))]
	public static partial nuint lua_newuserdata(lua_State L, ulong sz);

	[LibraryImport(DllName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller))]
	public static partial int lua_getmetatable(lua_State L, int objindex);

	[LibraryImport(DllName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller))]
	public static partial void lua_getfenv(lua_State L, int idx);

	[LibraryImport(DllName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller))]
	public static partial void lua_settable(lua_State L, int idx);

	[LibraryImport(DllName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller))]
	public static partial void lua_setfield(lua_State L, int idx, string k);

	[LibraryImport(DllName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller))]
	public static partial void lua_rawset(lua_State L, int idx);

	[LibraryImport(DllName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller))]
	public static partial void lua_rawseti(lua_State L, int idx, int n);

	[LibraryImport(DllName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller))]
	public static partial int lua_setmetatable(lua_State L, int objindex);

	[LibraryImport(DllName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller))]
	public static partial int lua_setfenv(lua_State L, int idx);

	[LibraryImport(DllName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller))]
	public static partial void lua_call(lua_State L, int nargs, int nresults);

	[LibraryImport(DllName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller))]
	public static partial int lua_pcall(lua_State L, int nargs, int nresults, int errfunc);

	[LibraryImport(DllName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller), EntryPoint = "lua_cpcall")]
	private static partial int _lua_cpcall(lua_State L, nint func, nuint ud);
	public static int lua_cpcall(lua_State L, lua_CFunction? func, nuint ud) {
		return _lua_cpcall(L, func == null ? 0 : Marshal.GetFunctionPointerForDelegate(func), ud);
	}

	[LibraryImport(DllName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller), EntryPoint = "lua_load")]
	private static partial int _lua_load(lua_State L, nint reader, nuint dt, string chunkname);
	public static int lua_load(lua_State L, lua_Reader? reader, nuint dt, string chunkname) {
		return _lua_load(L, reader == null ? 0 : Marshal.GetFunctionPointerForDelegate(reader), dt, chunkname);
	}

	[LibraryImport(DllName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller), EntryPoint = "lua_dump")]
	private static partial int _lua_dump(lua_State L, nint writer, nuint data);
	public static int lua_dump(lua_State L, lua_Writer? writer, nuint data) {
		return _lua_dump(L, writer == null ? 0 : Marshal.GetFunctionPointerForDelegate(writer), data);
	}

	[LibraryImport(DllName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller))]
	public static partial int lua_yield(lua_State L, int nresults);

	[LibraryImport(DllName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller))]
	public static partial int lua_resume(lua_State L, int narg);

	[LibraryImport(DllName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller))]
	public static partial int lua_status(lua_State L);

	public const int LUA_GCSTOP = 0;
	public const int LUA_GCRESTART = 1;
	public const int LUA_GCCOLLECT = 2;
	public const int LUA_GCCOUNT = 3;
	public const int LUA_GCCOUNTB = 4;
	public const int LUA_GCSTEP = 5;
	public const int LUA_GCSETPAUSE = 6;
	public const int LUA_GCSETSTEPMUL = 7;
	public const int LUA_GCISRUNNING = 9;

	[LibraryImport(DllName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller))]
	public static partial int lua_gc(lua_State L, int what, int data);

	[LibraryImport(DllName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller))]
	public static partial int lua_error(lua_State L);

	[LibraryImport(DllName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller))]
	public static partial int lua_next(lua_State L, int idx);

	[LibraryImport(DllName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller))]
	public static partial void lua_concat(lua_State L, int n);

	[LibraryImport(DllName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller))]
	public static partial lua_Alloc lua_getallocf(lua_State L, out nuint ud);

	[LibraryImport(DllName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller), EntryPoint = "lua_setallocf")]
	private static partial void _lua_setallocf(lua_State L, nint f, nuint ud);
	public static void lua_setallocf(lua_State L, lua_Alloc? f, nuint ud) {
		_lua_setallocf(L, f == null ? 0 : Marshal.GetFunctionPointerForDelegate(f), ud);
	}

	public static void lua_pop(lua_State L, int n) {
		lua_settop(L, -n - 1);
	}

	public static void lua_newtable(lua_State L) {
		lua_createtable(L, 0, 0);
	}

	public static void lua_register(lua_State L, string n, lua_CFunction? f) {
		lua_pushcfunction(L, f);
		lua_setglobal(L, n);
	}

	public static void lua_pushcfunction(lua_State L, lua_CFunction? f) {
		lua_pushcclosure(L, f, 0);
	}

	public static ulong lua_strlen(lua_State L, int i) {
		return lua_objlen(L, i);
	}

	public static int lua_isfunction(lua_State L, int n) {
		return (lua_type(L, n) == LUA_TFUNCTION) ? 1 : 0;
	}

	public static int lua_istable(lua_State L, int n) {
		return (lua_type(L, n) == LUA_TTABLE) ? 1 : 0;
	}

	public static int lua_islightuserdata(lua_State L, int n) {
		return (lua_type(L, n) == LUA_TLIGHTUSERDATA) ? 1 : 0;
	}

	public static int lua_isnil(lua_State L, int n) {
		return (lua_type(L, n) == LUA_TNIL) ? 1 : 0;
	}

	public static int lua_isboolean(lua_State L, int n) {
		return (lua_type(L, n) == LUA_TBOOLEAN) ? 1 : 0;
	}

	public static int lua_isthread(lua_State L, int n) {
		return (lua_type(L, n) == LUA_TTHREAD) ? 1 : 0;
	}

	public static int lua_isnone(lua_State L, int n) {
		return (lua_type(L, n) == LUA_TNONE) ? 1 : 0;
	}

	public static int lua_isnoneornil(lua_State L, int n) {
		return (lua_type(L, n) <= 0) ? 1 : 0;
	}

	public static void lua_pushliteral(lua_State L, string s) {
		lua_pushlstring(L, s, (size_t)s.Length);
	}

	public static void lua_setglobal(lua_State L, string s) {
		lua_setfield(L, LUA_GLOBALSINDEX, s);
	}

	public static void lua_getglobal(lua_State L, string s) {
		lua_getfield(L, LUA_GLOBALSINDEX, s);
	}

	public static string? lua_tostring(lua_State L, int i) {
		ulong temp = 0; // NOP
		return lua_tolstring(L, i, ref temp);
	}

	public static lua_State lua_open() {
		return luaL_newstate();
	}

	public static void lua_getregistry(lua_State L) {
		lua_pushvalue(L, LUA_REGISTRYINDEX);
	}

	public static int lua_getgccount(lua_State L) {
		return lua_gc(L, LUA_GCCOUNT, 0);
	}

	[LibraryImport(DllName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller))]
	public static partial void lua_setlevel(lua_State from, lua_State to);

	public const int LUA_HOOKCALL = 0;
	public const int LUA_HOOKRET = 1;
	public const int LUA_HOOKLINE = 2;
	public const int LUA_HOOKCOUNT = 3;
	public const int LUA_HOOKTAILRET = 4;

	public const int LUA_MASKCALL = 1 << LUA_HOOKCALL;
	public const int LUA_MASKRET = 1 << LUA_HOOKRET;
	public const int LUA_MASKLINE = 1 << LUA_HOOKLINE;
	public const int LUA_MASKCOUNT = 1 << LUA_HOOKCOUNT;

	// FIX (SYSLIB1051): lua_Debug lives in another assembly, so the source generator
	// refuses to marshal it. The imports take the native struct by pointer (always supported)
	// and the public wrappers convert. lua_getlocal/lua_setlocal take lua_Debug* in C,
	// so passing the struct by value was also an ABI bug.

	[LibraryImport(DllName, EntryPoint = "lua_getstack")]
	private static partial int _lua_getstack(lua_State L, int level, LuaDebugMarshaller.Native* ar);
	public static int lua_getstack(lua_State L, int level, ref lua_Debug ar) {
		LuaDebugMarshaller.Native native = LuaDebugMarshaller.ConvertToUnmanaged(ar);
		int ret = _lua_getstack(L, level, &native);
		ar = LuaDebugMarshaller.ConvertToManaged(native);
		return ret;
	}

	[LibraryImport(DllName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller), EntryPoint = "lua_getinfo")]
	private static partial int _lua_getinfo(lua_State L, string what, LuaDebugMarshaller.Native* ar);
	public static int lua_getinfo(lua_State L, string what, ref lua_Debug ar) {
		LuaDebugMarshaller.Native native = LuaDebugMarshaller.ConvertToUnmanaged(ar);
		int ret = _lua_getinfo(L, what, &native);
		ar = LuaDebugMarshaller.ConvertToManaged(native);
		return ret;
	}

	[LibraryImport(DllName, EntryPoint = "lua_getlocal")]
	private static partial nint _lua_getlocal(lua_State L, LuaDebugMarshaller.Native* ar, int n);
	public static string? lua_getlocal(lua_State L, in lua_Debug ar, int n) {
		LuaDebugMarshaller.Native native = LuaDebugMarshaller.ConvertToUnmanaged(ar);
		return Marshal.PtrToStringUTF8(_lua_getlocal(L, &native, n));
	}

	[LibraryImport(DllName, EntryPoint = "lua_setlocal")]
	private static partial nint _lua_setlocal(lua_State L, LuaDebugMarshaller.Native* ar, int n);
	public static string? lua_setlocal(lua_State L, in lua_Debug ar, int n) {
		LuaDebugMarshaller.Native native = LuaDebugMarshaller.ConvertToUnmanaged(ar);
		return Marshal.PtrToStringUTF8(_lua_setlocal(L, &native, n));
	}

	[LibraryImport(DllName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller), EntryPoint = "lua_getupvalue")]
	private static partial nint _lua_getupvalue(lua_State L, int funcindex, int n);
	public static string? lua_getupvalue(lua_State L, int funcindex, int n) {
		return Marshal.PtrToStringAnsi(_lua_getupvalue(L, funcindex, n));
	}

	[LibraryImport(DllName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller), EntryPoint = "lua_setupvalue")]
	private static partial nint _lua_setupvalue(lua_State L, int funcindex, int n);
	public static string? lua_setupvalue(lua_State L, int funcindex, int n) {
		return Marshal.PtrToStringAnsi(_lua_setupvalue(L, funcindex, n));
	}

	[LibraryImport(DllName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller), EntryPoint = "lua_sethook")]
	private static partial int _lua_sethook(lua_State L, nint func, int mask, int count);
	public static int lua_sethook(lua_State L, lua_Hook? func, int mask, int count) {
		return _lua_sethook(L, func == null ? 0 : Marshal.GetFunctionPointerForDelegate(func), mask, count);
	}

	[LibraryImport(DllName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller), EntryPoint = "lua_gethook")]
	private static partial nint _lua_gethook(lua_State L);
	public static lua_Hook? lua_gethook(lua_State L) {
		nint ret = _lua_gethook(L);
		return ret == 0 ? null : Marshal.GetDelegateForFunctionPointer<lua_Hook>(ret);
	}

	[LibraryImport(DllName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller))]
	public static partial int lua_gethookmask(lua_State L);

	[LibraryImport(DllName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller))]
	public static partial int lua_gethookcount(lua_State L);

	[LibraryImport(DllName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller))]
	public static partial nuint lua_upvalueid(lua_State L, int idx, int n);

	[LibraryImport(DllName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller))]
	public static partial void lua_upvaluejoin(lua_State L, int idx1, int n1, int idx2, int n2);

	[LibraryImport(DllName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller), EntryPoint = "lua_loadx")]
	private static partial int _lua_loadx(lua_State L, nint reader, nuint dt, string chunkname, string? mode);
	public static int lua_loadx(lua_State L, lua_Reader? reader, nuint dt, string chunkname, string? mode) {
		return _lua_loadx(L, reader == null ? 0 : Marshal.GetFunctionPointerForDelegate(reader), dt, chunkname, mode);
	}

	[LibraryImport(DllName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller), EntryPoint = "lua_version")]
	private static partial nint _lua_version(lua_State L);
	public static double lua_version(lua_State L) {
		nint mem = _lua_version(L);
		if (mem == 0)
			return 0;
		byte[] arr = new byte[8];
		for (int i = 0; i < arr.Length; i++)
			arr[i] = Marshal.ReadByte(mem, i);
		return BitConverter.ToDouble(arr, 0);
	}

	[LibraryImport(DllName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller))]
	public static partial void lua_copy(lua_State L, int fromidx, int toidx);

	[LibraryImport(DllName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller))]
	public static partial lua_Number lua_tonumberx(lua_State L, int idx, ref int isnum);

	[LibraryImport(DllName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller))]
	public static partial lua_Integer lua_tointegerx(lua_State L, int idx, ref int isnum);

	[LibraryImport(DllName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller))]
	public static partial int lua_isyieldable(lua_State L);

	public const int LUA_ERRFILE = LUA_ERRERR + 1;

	// FIX (SYSLIB1051): luaL_Reg[] is converted by hand into a native array.
	// A NULL sentinel is always appended, so callers no longer need to add one
	// (a trailing default(luaL_Reg) is still accepted and stops the copy).

	private static LuaLRegMarshaller.Native* AllocRegs(ReadOnlySpan<luaL_Reg> l) {
		var native = (LuaLRegMarshaller.Native*)NativeMemory.AllocZeroed((nuint)(l.Length + 1), (nuint)sizeof(LuaLRegMarshaller.Native));
		for (int i = 0; i < l.Length; i++) {
			if (l[i].name is null)
				break;
			native[i] = LuaLRegMarshaller.ConvertToUnmanaged(l[i]);
		}
		return native;
	}

	private static void FreeRegs(LuaLRegMarshaller.Native* native, int count) {
		for (int i = 0; i < count; i++)
			LuaLRegMarshaller.Free(native[i]); // NULL names are a no-op
		NativeMemory.Free(native);
	}

	[LibraryImport(DllName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller), EntryPoint = "luaL_openlib")]
	private static partial void _luaL_openlib(lua_State L, string? libname, LuaLRegMarshaller.Native* l, int nup);
	public static void luaL_openlib(lua_State L, string? libname, luaL_Reg[] l, int nup) {
		LuaLRegMarshaller.Native* native = AllocRegs(l);
		try { _luaL_openlib(L, libname, native, nup); }
		finally { FreeRegs(native, l.Length); }
	}

	[LibraryImport(DllName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller), EntryPoint = "luaL_register")]
	private static partial void _luaL_register(lua_State L, string? libname, LuaLRegMarshaller.Native* l);
	public static void luaL_register(lua_State L, string? libname, luaL_Reg[] l) {
		LuaLRegMarshaller.Native* native = AllocRegs(l);
		try { _luaL_register(L, libname, native); }
		finally { FreeRegs(native, l.Length); }
	}

	[LibraryImport(DllName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller))]
	public static partial int luaL_getmetafield(lua_State L, int obj, string e);

	[LibraryImport(DllName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller))]
	public static partial int luaL_callmeta(lua_State L, int obj, string e);

	[LibraryImport(DllName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller))]
	public static partial int luaL_typerror(lua_State L, int narg, string tname);

	[LibraryImport(DllName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller))]
	public static partial int luaL_argerror(lua_State L, int numarg, string extramsg);

	[LibraryImport(DllName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller), EntryPoint = "luaL_checklstring")]
	private static partial nint _luaL_checklstring(lua_State L, int numArg, ref size_t l);
	public static string? luaL_checklstring(lua_State L, int numArg, ref size_t l) {
		return Marshal.PtrToStringAnsi(_luaL_checklstring(L, numArg, ref l));
	}

	[LibraryImport(DllName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller), EntryPoint = "luaL_optlstring")]
	private static partial nint _luaL_optlstring(lua_State L, int numArg, string def, ref size_t l);
	public static string? luaL_optlstring(lua_State L, int numArg, string def, ref size_t l) {
		return Marshal.PtrToStringAnsi(_luaL_optlstring(L, numArg, def, ref l));
	}

	[LibraryImport(DllName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller))]
	public static partial lua_Number luaL_checknumber(lua_State L, int numArg);

	[LibraryImport(DllName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller))]
	public static partial lua_Number luaL_optnumber(lua_State L, int nArg, lua_Number def);

	[LibraryImport(DllName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller))]
	public static partial lua_Integer luaL_checkinteger(lua_State L, int numArg);

	[LibraryImport(DllName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller))]
	public static partial lua_Integer luaL_optinteger(lua_State L, int nArg, lua_Integer def);

	[LibraryImport(DllName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller))]
	public static partial void luaL_checkstack(lua_State L, int sz, string msg);

	[LibraryImport(DllName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller))]
	public static partial void luaL_checktype(lua_State L, int narg, int t);

	[LibraryImport(DllName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller))]
	public static partial void luaL_checkany(lua_State L, int narg);

	[LibraryImport(DllName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller))]
	public static partial int luaL_newmetatable(lua_State L, string tname);

	[LibraryImport(DllName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller))]
	public static partial nuint luaL_checkudata(lua_State L, int ud, string tname);

	[LibraryImport(DllName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller))]
	public static partial void luaL_where(lua_State L, int lvl);

	// FIX (CS0758): luaL_error is variadic in C, and LibraryImport can't forward `params`.
	// This does exactly what LuaJIT's luaL_error does internally, without varargs.
	// Format the message in C# first (e.g. with an interpolated string).
	public static int luaL_error(lua_State L, string message) {
		luaL_where(L, 1);
		lua_pushstring(L, message);
		lua_concat(L, 2);
		return lua_error(L);
	}

	[LibraryImport(DllName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller))]
	public static partial int luaL_checkoption(lua_State L, int narg, string def, string[] lst);

	public const int LUA_NOREF = -2;
	public const int LUA_REFNIL = -1;

	[LibraryImport(DllName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller))]
	public static partial int luaL_ref(lua_State L, int t);

	[LibraryImport(DllName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller))]
	public static partial void luaL_unref(lua_State L, int t, int _ref);

	[LibraryImport(DllName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller))]
	public static partial int luaL_loadfile(lua_State L, string filename);

	[LibraryImport(DllName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller))]
	public static partial int luaL_loadbuffer(lua_State L, string buff, size_t sz, string name);

	[LibraryImport(DllName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller))]
	public static partial int luaL_loadstring(lua_State L, string s);

	[LibraryImport(DllName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller))]
	public static partial lua_State luaL_newstate();

	[LibraryImport(DllName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller), EntryPoint = "luaL_gsub")]
	private static partial nint _luaL_gsub(lua_State L, string s, string p, string r);
	public static string? luaL_gsub(lua_State L, string s, string p, string r) {
		return Marshal.PtrToStringAnsi(_luaL_gsub(L, s, p, r));
	}

	[LibraryImport(DllName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller), EntryPoint = "luaL_findtable")]
	private static partial nint _luaL_findtable(lua_State L, int idx, string fname, int szhint);
	public static string? luaL_findtable(lua_State L, int idx, string fname, int szhint) {
		return Marshal.PtrToStringAnsi(_luaL_findtable(L, idx, fname, szhint));
	}

	[LibraryImport(DllName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller))]
	public static partial int luaL_fileresult(lua_State L, int stat, string fname);

	[LibraryImport(DllName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller))]
	public static partial int luaL_execresult(lua_State L, int stat);

	[LibraryImport(DllName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller))]
	public static partial int luaL_loadfilex(lua_State L, string filename, string? mode);

	[LibraryImport(DllName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller))]
	public static partial int luaL_loadbufferx(lua_State L, string buff, size_t sz, string name, string? mode);

	[LibraryImport(DllName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller))]
	public static partial void luaL_traceback(lua_State L, lua_State L1, string msg, int level);

	[LibraryImport(DllName, EntryPoint = "luaL_setfuncs")]
	private static partial void _luaL_setfuncs(lua_State L, LuaLRegMarshaller.Native* l, int nup);
	public static void luaL_setfuncs(lua_State L, luaL_Reg[] l, int nup) {
		LuaLRegMarshaller.Native* native = AllocRegs(l);
		try { _luaL_setfuncs(L, native, nup); }
		finally { FreeRegs(native, l.Length); }
	}

	[LibraryImport(DllName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller))]
	public static partial void luaL_pushmodule(lua_State L, string modename, int sizehint);

	[LibraryImport(DllName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller))]
	public static partial nuint luaL_testudata(lua_State L, int ud, string tname);

	[LibraryImport(DllName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller))]
	public static partial void luaL_setmetatable(lua_State L, string tname);

	public static void luaL_argcheck(lua_State L, bool cond, int numarg, string extramsg) {
		if (cond == false)
			luaL_argerror(L, numarg, extramsg);
	}

	public static string? luaL_checkstring(lua_State L, int n) {
		size_t temp = 0; // NOP
		return luaL_checklstring(L, n, ref temp);
	}

	public static string? luaL_optstring(lua_State L, int n, string d) {
		size_t temp = 0; // NOP
		return luaL_optlstring(L, n, d, ref temp);
	}

	public static int luaL_checkint(lua_State L, int n) {
		return (int)luaL_checkinteger(L, n);
	}

	public static int luaL_optint(lua_State L, int n, lua_Integer d) {
		return (int)luaL_optinteger(L, n, d);
	}

	public static long luaL_checklong(lua_State L, int n) {
		return luaL_checkinteger(L, n);
	}

	public static long luaL_optlong(lua_State L, int n, lua_Integer d) {
		return luaL_optinteger(L, n, d);
	}

	public static string? luaL_typename(lua_State L, int i) {
		return lua_typename(L, lua_type(L, i));
	}

	public static int luaL_dofile(lua_State L, string fn) {
		int status = luaL_loadfile(L, fn);
		if (status > 0)
			return status;
		return lua_pcall(L, 0, LUA_MULTRET, 0);
	}

	public static int luaL_dostring(lua_State L, string s) {
		int status = luaL_loadstring(L, s);
		if (status > 0)
			return status;
		return lua_pcall(L, 0, LUA_MULTRET, 0);
	}

	public static void luaL_getmetatable(lua_State L, string n) {
		lua_getfield(L, LUA_REGISTRYINDEX, n);
	}

	public delegate T luaL_Function<T>(lua_State L, int n);

	public static T luaL_opt<T>(lua_State L, luaL_Function<T> f, int n, T d) {
		return lua_isnoneornil(L, n) > 0 ? d : f(L, n);
	}

	public static void luaL_newlibtable(lua_State L, luaL_Reg[] l) {
		lua_createtable(L, 0, l.Length - 1);
	}

	public static void luaL_newlib(lua_State L, luaL_Reg[] l) {
		luaL_newlibtable(L, l);
		luaL_setfuncs(L, l, 0);
	}

	public static void luaL_addchar(luaL_Buffer* B, byte c) {
		if (B->p >= B->buffer + luaL_Buffer.NativeBufferSize)
			luaL_prepbuffer(B);
		*B->p++ = c;
	}

	public static void luaL_putchar(luaL_Buffer* B, byte c) {
		luaL_addchar(B, c);
	}

	public static void luaL_addsize(luaL_Buffer* B, int n) {
		B->p += n;
	}

	[LibraryImport(DllName)]
	public static partial void luaL_buffinit(lua_State L, luaL_Buffer* B);

	[LibraryImport(DllName)]
	public static partial byte* luaL_prepbuffer(luaL_Buffer* B);

	[LibraryImport(DllName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller))]
	public static partial void luaL_addlstring(luaL_Buffer* B, string s, size_t l);

	[LibraryImport(DllName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller))]
	public static partial void luaL_addstring(luaL_Buffer* B, string s);

	[LibraryImport(DllName)]
	public static partial void luaL_addvalue(luaL_Buffer* B);

	[LibraryImport(DllName)]
	public static partial void luaL_pushresult(luaL_Buffer* B);

	public const string LUA_FILEHANDLE = "FILE*";

	public const string LUA_COLIBNAME = "coroutine";
	public const string LUA_MATHLIBNAME = "math";
	public const string LUA_STRLIBNAME = "string";
	public const string LUA_TABLIBNAME = "table";
	public const string IOLIBNAME = "io";
	public const string OSLIBNAME = "os";
	public const string LOADLIBNAME = "package";
	public const string DBLIBNAME = "debug";
	public const string BITLIBNAME = "bit";
	public const string JITLIBNAME = "jit";
	public const string FFILIBNAME = "ffi";

	[LibraryImport(DllName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller))]
	public static partial int luaopen_base(lua_State L);

	[LibraryImport(DllName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller))]
	public static partial int luaopen_math(lua_State L);

	[LibraryImport(DllName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller))]
	public static partial int luaopen_string(lua_State L);

	[LibraryImport(DllName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller))]
	public static partial int luaopen_table(lua_State L);

	[LibraryImport(DllName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller))]
	public static partial int luaopen_io(lua_State L);

	[LibraryImport(DllName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller))]
	public static partial int luaopen_os(lua_State L);

	[LibraryImport(DllName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller))]
	public static partial int luaopen_package(lua_State L);

	[LibraryImport(DllName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller))]
	public static partial int luaopen_debug(lua_State L);

	[LibraryImport(DllName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller))]
	public static partial int luaopen_bit(lua_State L);

	[LibraryImport(DllName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller))]
	public static partial int luaopen_jit(lua_State L);

	[LibraryImport(DllName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller))]
	public static partial int luaopen_ffi(lua_State L);

	[LibraryImport(DllName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller))]
	public static partial int luaopen_string_buffer(lua_State L);

	[LibraryImport(DllName, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller))]
	public static partial void luaL_openlibs(lua_State L);

}
