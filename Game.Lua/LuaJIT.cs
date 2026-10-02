// Credit for initial bindings: https://github.com/tilkinsc/Lua.NET/blob/4fa0733656b6e1d55ebd2e3cef5b1add11b80997/src/LuaJIT.cs

using System.Runtime.InteropServices;

using size_t = System.UInt64;
using lua_Number = System.Double;
using lua_Integer = System.Int64;

using Source.Common.GarrysMod.Lua;

namespace Game.Lua;

public static class Lua
{
	private const string DllName = "lua51";
	private const CallingConvention Convention = CallingConvention.Cdecl;

	[StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
	public struct lua_Debug
	{
		public int _event;
		public string name;
		public string namewhat;
		public string what;
		public string source;
		public int currentline;
		public int nups;
		public int linedefined;
		public int lastlinedefined;
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = LUA_IDSIZE)]
		public sbyte[] short_src;
		public int i_ci;
	};

	[StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
	public struct luaL_Reg
	{
		public string? name;
		public nint func;
	};

	[StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
	public struct luaL_Buffer
	{
		public nint p;
		public int lvl;
		public lua_State L;
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = LUAL_BUFFERSIZE)]
		public sbyte[] buffer;
	};

	public delegate int lua_CFunction(lua_State L);
	public delegate nint lua_Reader(lua_State L, nuint ud, ref size_t sz);
	public delegate int lua_Writer(lua_State L, nuint p, size_t sz, nuint ud);
	public delegate nuint lua_Alloc(nuint ud, nuint ptr, size_t osize, size_t nsize);
	public delegate void lua_Hook(lua_State L, lua_Debug ar);

	public static unsafe luaL_Reg AsLuaLReg(string name, delegate* unmanaged<lua_State, int> func) => new() { name = name, func = (nint)func };

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

	[DllImport(DllName, CallingConvention = Convention)]
	public static extern void luaJIT_profile_start(lua_State L, string mode, luaJIT_profile_callback cb, nuint data);

	[DllImport(DllName, CallingConvention = Convention)]
	public static extern void luaJIT_profile_stop(lua_State L);

	[DllImport(DllName, CallingConvention = Convention, EntryPoint = "luaJIT_profile_dumpstack")]
	private static extern nint _luaJIT_profile_dumpstack(lua_State L, string fmt, int depth, ref size_t len);
	public static string? luaJIT_profile_dumpstack(lua_State L, string fmt, int depth, ref size_t len) {
		return Marshal.PtrToStringAnsi(_luaJIT_profile_dumpstack(L, fmt, depth, ref len));
	}

	[DllImport(DllName, CallingConvention = Convention)]
	public static extern void luaJIT_version_2_1_0_beta3();

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

	public const int LUAL_BUFFERSIZE = 512 > 16384 ? 8182 : 512;

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

	[DllImport(DllName, CallingConvention = Convention, EntryPoint = "lua_newstate")]
	private static extern lua_State _lua_newstate(nint f, nuint ud);
	public static lua_State lua_newstate(lua_Alloc? f, nuint ud) {
		return _lua_newstate(f == null ? 0 : Marshal.GetFunctionPointerForDelegate(f), ud);
	}

	[DllImport(DllName, CallingConvention = Convention)]
	public static extern void lua_close(lua_State L);

	[DllImport(DllName, CallingConvention = Convention)]
	public static extern lua_State lua_newthread(lua_State L);

	[DllImport(DllName, CallingConvention = Convention, EntryPoint = "lua_atpanic")]
	private static extern nint _lua_atpanic(lua_State L, nint panicf);
	public static lua_CFunction? lua_atpanic(lua_State L, lua_CFunction? panicf) {
		nint panic = _lua_atpanic(L, panicf == null ? 0 : Marshal.GetFunctionPointerForDelegate(panicf));
		return panic == 0 ? null : Marshal.GetDelegateForFunctionPointer<lua_CFunction>(panic);
	}

	[DllImport(DllName, CallingConvention = Convention)]
	public static extern int lua_gettop(lua_State L);

	[DllImport(DllName, CallingConvention = Convention)]
	public static extern void lua_settop(lua_State L, int idx);

	[DllImport(DllName, CallingConvention = Convention)]
	public static extern void lua_pushvalue(lua_State L, int idx);

	[DllImport(DllName, CallingConvention = Convention)]
	public static extern void lua_remove(lua_State L, int idx);

	[DllImport(DllName, CallingConvention = Convention)]
	public static extern void lua_insert(lua_State L, int idx);

	[DllImport(DllName, CallingConvention = Convention)]
	public static extern void lua_replace(lua_State L, int idx);

	[DllImport(DllName, CallingConvention = Convention)]
	public static extern int lua_checkstack(lua_State L, int sz);

	[DllImport(DllName, CallingConvention = Convention)]
	public static extern void lua_xmove(lua_State from, lua_State to, int n);

	[DllImport(DllName, CallingConvention = Convention)]
	public static extern int lua_isnumber(lua_State L, int idx);

	[DllImport(DllName, CallingConvention = Convention)]
	public static extern int lua_isstring(lua_State L, int idx);

	[DllImport(DllName, CallingConvention = Convention)]
	public static extern int lua_iscfunction(lua_State L, int idx);

	[DllImport(DllName, CallingConvention = Convention)]
	public static extern int lua_isuserdata(lua_State L, int idx);

	[DllImport(DllName, CallingConvention = Convention)]
	public static extern int lua_type(lua_State L, int idx);

	[DllImport(DllName, CallingConvention = Convention, EntryPoint = "lua_typename")]
	private static extern nint _lua_typename(lua_State L, int tp);
	public static string? lua_typename(lua_State L, int tp) {
		return Marshal.PtrToStringAnsi(_lua_typename(L, tp));
	}

	[DllImport(DllName, CallingConvention = Convention)]
	public static extern int lua_equal(lua_State L, int idx1, int idx2);

	[DllImport(DllName, CallingConvention = Convention)]
	public static extern int lua_rawequal(lua_State L, int idx1, int idx2);

	[DllImport(DllName, CallingConvention = Convention)]
	public static extern int lua_lessthan(lua_State L, int idx1, int idx2);

	[DllImport(DllName, CallingConvention = Convention)]
	public static extern lua_Number lua_tonumber(lua_State L, int idx);

	[DllImport(DllName, CallingConvention = Convention)]
	public static extern lua_Integer lua_tointeger(lua_State L, int idx);

	[DllImport(DllName, CallingConvention = Convention)]
	public static extern int lua_toboolean(lua_State L, int idx);

	[DllImport(DllName, CallingConvention = Convention, EntryPoint = "lua_tolstring")]
	private static extern nint _lua_tolstring(lua_State L, int idx, ref ulong len);
	public static string? lua_tolstring(lua_State L, int idx, ref ulong len) {
		return Marshal.PtrToStringAnsi(_lua_tolstring(L, idx, ref len));
	}

	[DllImport(DllName, CallingConvention = Convention)]
	public static extern ulong lua_objlen(lua_State L, int idx);

	[DllImport(DllName, CallingConvention = Convention, EntryPoint = "lua_tocfunction")]
	private static extern nint _lua_tocfunction(lua_State L, int idx);
	public static lua_CFunction? lua_tocfunction(lua_State L, int idx) {
		nint ret = _lua_tocfunction(L, idx);
		return ret == 0 ? null : Marshal.GetDelegateForFunctionPointer<lua_CFunction>(ret);
	}

	[DllImport(DllName, CallingConvention = Convention)]
	public static extern nuint lua_touserdata(lua_State L, int idx);

	[DllImport(DllName, CallingConvention = Convention)]
	public static extern lua_State lua_tothread(lua_State L, int idx);

	[DllImport(DllName, CallingConvention = Convention)]
	public static extern nuint lua_topointer(lua_State L, int idx);

	[DllImport(DllName, CallingConvention = Convention)]
	public static extern void lua_pushnil(lua_State L);

	[DllImport(DllName, CallingConvention = Convention)]
	public static extern void lua_pushnumber(lua_State L, lua_Number n);

	[DllImport(DllName, CallingConvention = Convention)]
	public static extern void lua_pushinteger(lua_State L, lua_Integer n);

	[DllImport(DllName, CallingConvention = Convention)]
	public static extern void lua_pushlstring(lua_State L, string s, size_t len);

	[DllImport(DllName, CallingConvention = Convention)]
	public static extern void lua_pushstring(lua_State L, string s);

	// TODO:
	// [DllImport(DllName, CallingConvention = Convention)]
	// public static extern nint lua_pushvfstring(lua_State L, string fmt, va_list argp);

	// TODO:
	// [DllImport(DllName, CallingConvention = Convention, EntryPoint = "lua_pushfstring")]
	// private static extern nint _lua_pushfstring(lua_State L, string fmt, params string[] args);
	// public static string? lua_pushfstring(lua_State L, string fmt, params string[] args)
	// {
	// 	return Marshal.PtrToStringAnsi(_lua_pushfstring(L, fmt, args));
	// }

	[DllImport(DllName, CallingConvention = Convention, EntryPoint = "lua_pushcclosure")]
	private static extern void _lua_pushcclosure(lua_State L, nint fn, int n);
	public static void lua_pushcclosure(lua_State L, lua_CFunction? fn, int n) {
		_lua_pushcclosure(L, fn == null ? 0 : Marshal.GetFunctionPointerForDelegate(fn), n);
	}

	[DllImport(DllName, CallingConvention = Convention)]
	public static extern void lua_pushboolean(lua_State L, int b);

	[DllImport(DllName, CallingConvention = Convention)]
	public static extern void lua_pushlightuserdata(lua_State L, nuint p);

	[DllImport(DllName, CallingConvention = Convention)]
	public static extern int lua_pushthread(lua_State L);

	[DllImport(DllName, CallingConvention = Convention)]
	public static extern void lua_gettable(lua_State L, int idx);

	[DllImport(DllName, CallingConvention = Convention)]
	public static extern void lua_getfield(lua_State L, int idx, string k);

	[DllImport(DllName, CallingConvention = Convention)]
	public static extern void lua_rawget(lua_State L, int idx);

	[DllImport(DllName, CallingConvention = Convention)]
	public static extern void lua_rawgeti(lua_State L, int idx, int n);

	[DllImport(DllName, CallingConvention = Convention)]
	public static extern void lua_createtable(lua_State L, int narr, int nrec);

	[DllImport(DllName, CallingConvention = Convention)]
	public static extern nuint lua_newuserdata(lua_State L, ulong sz);

	[DllImport(DllName, CallingConvention = Convention)]
	public static extern int lua_getmetatable(lua_State L, int objindex);

	[DllImport(DllName, CallingConvention = Convention)]
	public static extern void lua_getfenv(lua_State L, int idx);

	[DllImport(DllName, CallingConvention = Convention)]
	public static extern void lua_settable(lua_State L, int idx);

	[DllImport(DllName, CallingConvention = Convention)]
	public static extern void lua_setfield(lua_State L, int idx, string k);

	[DllImport(DllName, CallingConvention = Convention)]
	public static extern void lua_rawset(lua_State L, int idx);

	[DllImport(DllName, CallingConvention = Convention)]
	public static extern void lua_rawseti(lua_State L, int idx, int n);

	[DllImport(DllName, CallingConvention = Convention)]
	public static extern int lua_setmetatable(lua_State L, int objindex);

	[DllImport(DllName, CallingConvention = Convention)]
	public static extern int lua_setfenv(lua_State L, int idx);

	[DllImport(DllName, CallingConvention = Convention)]
	public static extern void lua_call(lua_State L, int nargs, int nresults);

	[DllImport(DllName, CallingConvention = Convention)]
	public static extern int lua_pcall(lua_State L, int nargs, int nresults, int errfunc);

	[DllImport(DllName, CallingConvention = Convention, EntryPoint = "lua_cpcall")]
	private static extern int _lua_cpcall(lua_State L, nint func, nuint ud);
	public static int lua_cpcall(lua_State L, lua_CFunction? func, nuint ud) {
		return _lua_cpcall(L, func == null ? 0 : Marshal.GetFunctionPointerForDelegate(func), ud);
	}

	[DllImport(DllName, CallingConvention = Convention, EntryPoint = "lua_load")]
	private static extern int _lua_load(lua_State L, nint reader, nuint dt, string chunkname);
	public static int lua_load(lua_State L, lua_Reader? reader, nuint dt, string chunkname) {
		return _lua_load(L, reader == null ? 0 : Marshal.GetFunctionPointerForDelegate(reader), dt, chunkname);
	}

	[DllImport(DllName, CallingConvention = Convention, EntryPoint = "lua_dump")]
	private static extern int _lua_dump(lua_State L, nint writer, nuint data);
	public static int lua_dump(lua_State L, lua_Writer? writer, nuint data) {
		return _lua_dump(L, writer == null ? 0 : Marshal.GetFunctionPointerForDelegate(writer), data);
	}

	[DllImport(DllName, CallingConvention = Convention)]
	public static extern int lua_yield(lua_State L, int nresults);

	[DllImport(DllName, CallingConvention = Convention)]
	public static extern int lua_resume(lua_State L, int narg);

	[DllImport(DllName, CallingConvention = Convention)]
	public static extern int lua_status(lua_State L);

	public const int LUA_GCSTOP = 0;
	public const int LUA_GCRESTART = 1;
	public const int LUA_GCCOLLECT = 2;
	public const int LUA_GCCOUNT = 3;
	public const int LUA_GCCOUNTB = 4;
	public const int LUA_GCSTEP = 5;
	public const int LUA_GCSETPAUSE = 6;
	public const int LUA_GCSETSTEPMUL = 7;
	public const int LUA_GCISRUNNING = 9;

	[DllImport(DllName, CallingConvention = Convention)]
	public static extern int lua_gc(lua_State L, int what, int data);

	[DllImport(DllName, CallingConvention = Convention)]
	public static extern int lua_error(lua_State L);

	[DllImport(DllName, CallingConvention = Convention)]
	public static extern int lua_next(lua_State L, int idx);

	[DllImport(DllName, CallingConvention = Convention)]
	public static extern void lua_concat(lua_State L, int n);

	[DllImport(DllName, CallingConvention = Convention)]
	public static extern lua_Alloc lua_getallocf(lua_State L, out nuint ud);

	[DllImport(DllName, CallingConvention = Convention, EntryPoint = "lua_setallocf")]
	private static extern void _lua_setallocf(lua_State L, nint f, nuint ud);
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

	[DllImport(DllName, CallingConvention = Convention)]
	public static extern void lua_setlevel(lua_State from, lua_State to);

	public const int LUA_HOOKCALL = 0;
	public const int LUA_HOOKRET = 1;
	public const int LUA_HOOKLINE = 2;
	public const int LUA_HOOKCOUNT = 3;
	public const int LUA_HOOKTAILRET = 4;

	public const int LUA_MASKCALL = 1 << LUA_HOOKCALL;
	public const int LUA_MASKRET = 1 << LUA_HOOKRET;
	public const int LUA_MASKLINE = 1 << LUA_HOOKLINE;
	public const int LUA_MASKCOUNT = 1 << LUA_HOOKCOUNT;

	[DllImport(DllName, CallingConvention = Convention)]
	public static extern int lua_getstack(lua_State L, int level, ref lua_Debug ar);

	[DllImport(DllName, CallingConvention = Convention)]
	public static extern int lua_getinfo(lua_State L, string what, ref lua_Debug ar);

	[DllImport(DllName, CallingConvention = Convention, EntryPoint = "lua_getlocal")]
	private static extern nint _lua_getlocal(lua_State L, lua_Debug ar, int n);
	public static string? lua_getlocal(lua_State L, lua_Debug ar, int n) {
		return Marshal.PtrToStringAnsi(_lua_getlocal(L, ar, n));
	}

	[DllImport(DllName, CallingConvention = Convention, EntryPoint = "lua_setlocal")]
	private static extern nint _lua_setlocal(lua_State L, lua_Debug ar, int n);
	public static string? lua_setlocal(lua_State L, lua_Debug ar, int n) {
		return Marshal.PtrToStringAnsi(_lua_setlocal(L, ar, n));
	}

	[DllImport(DllName, CallingConvention = Convention, EntryPoint = "lua_getupvalue")]
	private static extern nint _lua_getupvalue(lua_State L, int funcindex, int n);
	public static string? lua_getupvalue(lua_State L, int funcindex, int n) {
		return Marshal.PtrToStringAnsi(_lua_getupvalue(L, funcindex, n));
	}

	[DllImport(DllName, CallingConvention = Convention, EntryPoint = "lua_setupvalue")]
	private static extern nint _lua_setupvalue(lua_State L, int funcindex, int n);
	public static string? lua_setupvalue(lua_State L, int funcindex, int n) {
		return Marshal.PtrToStringAnsi(_lua_setupvalue(L, funcindex, n));
	}

	[DllImport(DllName, CallingConvention = Convention, EntryPoint = "lua_sethook")]
	private static extern int _lua_sethook(lua_State L, nint func, int mask, int count);
	public static int lua_sethook(lua_State L, lua_Hook? func, int mask, int count) {
		return _lua_sethook(L, func == null ? 0 : Marshal.GetFunctionPointerForDelegate(func), mask, count);
	}

	[DllImport(DllName, CallingConvention = Convention, EntryPoint = "lua_gethook")]
	private static extern nint _lua_gethook(lua_State L);
	public static lua_Hook? lua_gethook(lua_State L) {
		nint ret = _lua_gethook(L);
		return ret == 0 ? null : Marshal.GetDelegateForFunctionPointer<lua_Hook>(ret);
	}

	[DllImport(DllName, CallingConvention = Convention)]
	public static extern int lua_gethookmask(lua_State L);

	[DllImport(DllName, CallingConvention = Convention)]
	public static extern int lua_gethookcount(lua_State L);

	[DllImport(DllName, CallingConvention = Convention)]
	public static extern nuint lua_upvalueid(lua_State L, int idx, int n);

	[DllImport(DllName, CallingConvention = Convention)]
	public static extern void lua_upvaluejoin(lua_State L, int idx1, int n1, int idx2, int n2);

	[DllImport(DllName, CallingConvention = Convention, EntryPoint = "lua_loadx")]
	private static extern int _lua_loadx(lua_State L, nint reader, nuint dt, string chunkname, string? mode);
	public static int lua_loadx(lua_State L, lua_Reader? reader, nuint dt, string chunkname, string? mode) {
		return _lua_loadx(L, reader == null ? 0 : Marshal.GetFunctionPointerForDelegate(reader), dt, chunkname, mode);
	}

	[DllImport(DllName, CallingConvention = Convention, EntryPoint = "lua_version")]
	private static extern nint _lua_version(lua_State L);
	public static double lua_version(lua_State L) {
		nint mem = _lua_version(L);
		if (mem == 0)
			return 0;
		byte[] arr = new byte[8];
		for (int i = 0; i < arr.Length; i++)
			arr[i] = Marshal.ReadByte(mem, i);
		return BitConverter.ToDouble(arr, 0);
	}

	[DllImport(DllName, CallingConvention = Convention)]
	public static extern void lua_copy(lua_State L, int fromidx, int toidx);

	[DllImport(DllName, CallingConvention = Convention)]
	public static extern lua_Number lua_tonumberx(lua_State L, int idx, ref int isnum);

	[DllImport(DllName, CallingConvention = Convention)]
	public static extern lua_Integer lua_tointegerx(lua_State L, int idx, ref int isnum);

	[DllImport(DllName, CallingConvention = Convention)]
	public static extern int lua_isyieldable(lua_State L);

	public const int LUA_ERRFILE = LUA_ERRERR + 1;

	[DllImport(DllName, CallingConvention = Convention)]
	public static extern void luaL_openlib(lua_State L, string libname, luaL_Reg[] l, int nup);

	[DllImport(DllName, CallingConvention = Convention)]
	public static extern void luaL_register(lua_State L, string libname, luaL_Reg[] l);

	[DllImport(DllName, CallingConvention = Convention)]
	public static extern int luaL_getmetafield(lua_State L, int obj, string e);

	[DllImport(DllName, CallingConvention = Convention)]
	public static extern int luaL_callmeta(lua_State L, int obj, string e);

	[DllImport(DllName, CallingConvention = Convention)]
	public static extern int luaL_typerror(lua_State L, int narg, string tname);

	[DllImport(DllName, CallingConvention = Convention)]
	public static extern int luaL_argerror(lua_State L, int numarg, string extramsg);

	[DllImport(DllName, CallingConvention = Convention, EntryPoint = "luaL_checklstring")]
	private static extern nint _luaL_checklstring(lua_State L, int numArg, ref size_t l);
	public static string? luaL_checklstring(lua_State L, int numArg, ref size_t l) {
		return Marshal.PtrToStringAnsi(_luaL_checklstring(L, numArg, ref l));
	}

	[DllImport(DllName, CallingConvention = Convention, EntryPoint = "luaL_optlstring")]
	private static extern nint _luaL_optlstring(lua_State L, int numArg, string def, ref size_t l);
	public static string? luaL_optlstring(lua_State L, int numArg, string def, ref size_t l) {
		return Marshal.PtrToStringAnsi(_luaL_optlstring(L, numArg, def, ref l));
	}

	[DllImport(DllName, CallingConvention = Convention)]
	public static extern lua_Number luaL_checknumber(lua_State L, int numArg);

	[DllImport(DllName, CallingConvention = Convention)]
	public static extern lua_Number luaL_optnumber(lua_State L, int nArg, lua_Number def);

	[DllImport(DllName, CallingConvention = Convention)]
	public static extern lua_Integer luaL_checkinteger(lua_State L, int numArg);

	[DllImport(DllName, CallingConvention = Convention)]
	public static extern lua_Integer luaL_optinteger(lua_State L, int nArg, lua_Integer def);

	[DllImport(DllName, CallingConvention = Convention)]
	public static extern void luaL_checkstack(lua_State L, int sz, string msg);

	[DllImport(DllName, CallingConvention = Convention)]
	public static extern void luaL_checktype(lua_State L, int narg, int t);

	[DllImport(DllName, CallingConvention = Convention)]
	public static extern void luaL_checkany(lua_State L, int narg);

	[DllImport(DllName, CallingConvention = Convention)]
	public static extern int luaL_newmetatable(lua_State L, string tname);

	[DllImport(DllName, CallingConvention = Convention)]
	public static extern nuint luaL_checkudata(lua_State L, int ud, string tname);

	[DllImport(DllName, CallingConvention = Convention)]
	public static extern void luaL_where(lua_State L, int lvl);

	// TODO: I dont think params works
	[DllImport(DllName, CallingConvention = Convention)]
	public static extern int luaL_error(lua_State L, string fmt, params string[] args);

	[DllImport(DllName, CallingConvention = Convention)]
	public static extern int luaL_checkoption(lua_State L, int narg, string def, string[] lst);

	public const int LUA_NOREF = -2;
	public const int LUA_REFNIL = -1;

	[DllImport(DllName, CallingConvention = Convention)]
	public static extern int luaL_ref(lua_State L, int t);

	[DllImport(DllName, CallingConvention = Convention)]
	public static extern void luaL_unref(lua_State L, int t, int _ref);

	[DllImport(DllName, CallingConvention = Convention)]
	public static extern int luaL_loadfile(lua_State L, string filename);

	[DllImport(DllName, CallingConvention = Convention)]
	public static extern int luaL_loadbuffer(lua_State L, string buff, size_t sz, string name);

	[DllImport(DllName, CallingConvention = Convention)]
	public static extern int luaL_loadstring(lua_State L, string s);

	[DllImport(DllName, CallingConvention = Convention)]
	public static extern lua_State luaL_newstate();

	[DllImport(DllName, CallingConvention = Convention, EntryPoint = "luaL_gsub")]
	private static extern nint _luaL_gsub(lua_State L, string s, string p, string r);
	public static string? luaL_gsub(lua_State L, string s, string p, string r) {
		return Marshal.PtrToStringAnsi(_luaL_gsub(L, s, p, r));
	}

	[DllImport(DllName, CallingConvention = Convention, EntryPoint = "luaL_findtable")]
	private static extern nint _luaL_findtable(lua_State L, int idx, string fname, int szhint);
	public static string? luaL_findtable(lua_State L, int idx, string fname, int szhint) {
		return Marshal.PtrToStringAnsi(_luaL_findtable(L, idx, fname, szhint));
	}

	[DllImport(DllName, CallingConvention = Convention)]
	public static extern int luaL_fileresult(lua_State L, int stat, string fname);

	[DllImport(DllName, CallingConvention = Convention)]
	public static extern int luaL_execresult(lua_State L, int stat);

	[DllImport(DllName, CallingConvention = Convention)]
	public static extern int luaL_loadfilex(lua_State L, string filename, string? mode);

	[DllImport(DllName, CallingConvention = Convention)]
	public static extern int luaL_loadbufferx(lua_State L, string buff, size_t sz, string name, string? mode);

	[DllImport(DllName, CallingConvention = Convention)]
	public static extern void luaL_traceback(lua_State L, lua_State L1, string msg, int level);

	[DllImport(DllName, CallingConvention = Convention)]
	public static extern void luaL_setfuncs(lua_State L, luaL_Reg[] l, int nup);

	[DllImport(DllName, CallingConvention = Convention)]
	public static extern void luaL_pushmodule(lua_State L, string modename, int sizehint);

	[DllImport(DllName, CallingConvention = Convention)]
	public static extern nuint luaL_testudata(lua_State L, int ud, string tname);

	[DllImport(DllName, CallingConvention = Convention)]
	public static extern void luaL_setmetatable(lua_State L, string tname);

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

	public static void luaL_addchar(luaL_Buffer B, byte c) {
		if (B.p >= B.buffer.Length + LUAL_BUFFERSIZE)
			luaL_prepbuffer(B);
		Marshal.WriteByte(B.p, c);
		B.p += 1;
	}

	public static void luaL_putchar(luaL_Buffer B, byte c) {
		luaL_addchar(B, c);
	}

	public static void luaL_addsize(luaL_Buffer B, int n) {
		B.p += n;
	}

	[DllImport(DllName, CallingConvention = Convention)]
	public static extern void luaL_buffinit(lua_State L, luaL_Buffer B);

	[DllImport(DllName, CallingConvention = Convention)]
	public static extern nint luaL_prepbuffer(luaL_Buffer B);

	[DllImport(DllName, CallingConvention = Convention)]
	public static extern void luaL_addlstring(luaL_Buffer B, string s, size_t l);

	[DllImport(DllName, CallingConvention = Convention)]
	public static extern void luaL_addstring(luaL_Buffer B, string s);

	[DllImport(DllName, CallingConvention = Convention)]
	public static extern void luaL_addvalue(luaL_Buffer B);

	[DllImport(DllName, CallingConvention = Convention)]
	public static extern void luaL_pushresult(luaL_Buffer B);

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
	public const string FFILIBNAME = "fii";

	[DllImport(DllName, CallingConvention = Convention)]
	public static extern int luaopen_base(lua_State L);

	[DllImport(DllName, CallingConvention = Convention)]
	public static extern int luaopen_math(lua_State L);

	[DllImport(DllName, CallingConvention = Convention)]
	public static extern int luaopen_string(lua_State L);

	[DllImport(DllName, CallingConvention = Convention)]
	public static extern int luaopen_table(lua_State L);

	[DllImport(DllName, CallingConvention = Convention)]
	public static extern int luaopen_io(lua_State L);

	[DllImport(DllName, CallingConvention = Convention)]
	public static extern int luaopen_os(lua_State L);

	[DllImport(DllName, CallingConvention = Convention)]
	public static extern int luaopen_package(lua_State L);

	[DllImport(DllName, CallingConvention = Convention)]
	public static extern int luaopen_debug(lua_State L);

	[DllImport(DllName, CallingConvention = Convention)]
	public static extern int luaopen_bit(lua_State L);

	[DllImport(DllName, CallingConvention = Convention)]
	public static extern int luaopen_jit(lua_State L);

	[DllImport(DllName, CallingConvention = Convention)]
	public static extern int luaopen_ffi(lua_State L);

	[DllImport(DllName, CallingConvention = Convention)]
	public static extern int luaopen_string_buffer(lua_State L);

	[DllImport(DllName, CallingConvention = Convention)]
	public static extern void luaL_openlibs(lua_State L);

}

