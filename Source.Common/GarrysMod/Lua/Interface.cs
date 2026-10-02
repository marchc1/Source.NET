#pragma warning disable IDE1006 // Naming Styles

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Text;

namespace Source.Common.GarrysMod.Lua;

[CustomMarshaller(typeof(lua_State), MarshalMode.Default, typeof(LuaStateMarshaller))]
public static unsafe class LuaStateMarshaller
{
	public static void* ConvertToUnmanaged(lua_State managed) => (void*)managed.Handle;
	public static lua_State ConvertToManaged(void* unmanaged) => new lua_State { Handle = (nuint)unmanaged };
}

[NativeMarshalling(typeof(LuaStateMarshaller))]
public struct lua_State : IEquatable<lua_State>
{
	public nuint Handle;

	public readonly bool IsNull => Handle == 0;
	public readonly bool IsNotNull => Handle != 0;

	public static bool operator !(lua_State state) => state.Handle == 0;
	public static bool operator ==(lua_State state1, lua_State state2) => state1.Handle == state2.Handle;
	public static bool operator ==(lua_State state1, int handle) => state1.Handle == (nuint)handle;
	public static bool operator !=(lua_State state1, lua_State state2) => state1.Handle != state2.Handle;
	public static bool operator !=(lua_State state1, int handle) => state1.Handle != (nuint)handle;

	public readonly bool Equals(lua_State other) => Handle == other.Handle;
	public override readonly bool Equals(object? other) => other is lua_State state && Equals(state);
	public override readonly int GetHashCode() => Handle.GetHashCode();
}

[NativeMarshalling(typeof(LuaDebugMarshaller))]
public struct lua_Debug
{
	public int _event;
	public string? name;
	public string? namewhat;
	public string? what;
	public string? source;
	public int currentline;
	public int nups;
	public int linedefined;
	public int lastlinedefined;
	public string? short_src;
	public int i_ci;
}

[CustomMarshaller(typeof(lua_Debug), MarshalMode.Default, typeof(LuaDebugMarshaller))]
public static unsafe class LuaDebugMarshaller
{
	public const int LUA_IDSIZE = 60;

	[StructLayout(LayoutKind.Sequential)]
	public struct Native
	{
		public int _event;
		public byte* name;
		public byte* namewhat;
		public byte* what;
		public byte* source;
		public int currentline;
		public int nups;
		public int linedefined;
		public int lastlinedefined;
		public fixed byte short_src[LUA_IDSIZE];
		public int i_ci;
	}

	public static Native ConvertToUnmanaged(lua_Debug managed) {
		Native native = default;
		native._event = managed._event;
		native.currentline = managed.currentline;
		native.nups = managed.nups;
		native.linedefined = managed.linedefined;
		native.lastlinedefined = managed.lastlinedefined;
		native.i_ci = managed.i_ci;

		if (managed.short_src is not null) {
			Span<byte> dest = new(native.short_src, LUA_IDSIZE);
			int written = Encoding.UTF8.GetBytes(
				managed.short_src.AsSpan(0, Math.Min(managed.short_src.Length, LUA_IDSIZE - 1)),
				dest[..(LUA_IDSIZE - 1)]);
			dest[written] = 0;
		}

		return native;
	}

	public static lua_Debug ConvertToManaged(Native unmanaged) {
		byte* src = unmanaged.short_src;
		int len = new ReadOnlySpan<byte>(src, LUA_IDSIZE).IndexOf((byte)0);
		if (len < 0)
			len = LUA_IDSIZE;

		return new lua_Debug {
			_event = unmanaged._event,
			name = Utf8StringMarshaller.ConvertToManaged(unmanaged.name),
			namewhat = Utf8StringMarshaller.ConvertToManaged(unmanaged.namewhat),
			what = Utf8StringMarshaller.ConvertToManaged(unmanaged.what),
			source = Utf8StringMarshaller.ConvertToManaged(unmanaged.source),
			currentline = unmanaged.currentline,
			nups = unmanaged.nups,
			linedefined = unmanaged.linedefined,
			lastlinedefined = unmanaged.lastlinedefined,
			short_src = Encoding.UTF8.GetString(src, len),
			i_ci = unmanaged.i_ci,
		};
	}
	
	public static lua_Debug FromPointer(Native* ar) => ConvertToManaged(*ar);
}


[NativeMarshalling(typeof(LuaLRegMarshaller))]
public struct luaL_Reg
{
	public string? name;
	public nint func;

	public luaL_Reg(string? name, nint func) {
		this.name = name;
		this.func = func;
	}

	public static unsafe luaL_Reg Create(string name, delegate* unmanaged[Cdecl]<nint, int> func) => new(name, (nint)func);
}

[CustomMarshaller(typeof(luaL_Reg), MarshalMode.Default, typeof(LuaLRegMarshaller))]
public static unsafe class LuaLRegMarshaller
{
	[StructLayout(LayoutKind.Sequential)]
	public struct Native
	{
		public byte* name;
		public nint func;
	}

	public static Native ConvertToUnmanaged(luaL_Reg managed) => new() {
		name = Utf8StringMarshaller.ConvertToUnmanaged(managed.name), // null -> NULL
		func = managed.func,
	};

	public static luaL_Reg ConvertToManaged(Native unmanaged) => new() {
		name = Utf8StringMarshaller.ConvertToManaged(unmanaged.name),
		func = unmanaged.func,
	};

	public static void Free(Native unmanaged) => Utf8StringMarshaller.Free(unmanaged.name);
}


[StructLayout(LayoutKind.Sequential)]
public unsafe struct luaL_Buffer
{
	public const int MaxBufferSize = 8192;
	
	public static int NativeBufferSize { get; } =
		OperatingSystem.IsWindows() ? 512 :
		OperatingSystem.IsMacOS() ? 1024 :
		8192;

	public byte* p;
	public int lvl;
	public nint L;
	public fixed byte buffer[MaxBufferSize];

	public readonly lua_State State => new() { Handle = (nuint)L };
}

#pragma warning restore IDE1006 // Naming Styles
