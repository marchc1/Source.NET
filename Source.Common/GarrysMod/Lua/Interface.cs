#pragma warning disable IDE1006 // Naming Styles

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Source.Common.GarrysMod.Lua;

[StructLayout(LayoutKind.Sequential)]
public readonly struct lua_State : IEquatable<lua_State>
{
	public readonly nint Handle;

	public lua_State(nint handle) => Handle = handle;

	public bool IsNull => Handle == 0;

	public static bool operator ==(lua_State a, lua_State b) => a.Handle == b.Handle;
	public static bool operator !=(lua_State a, lua_State b) => a.Handle != b.Handle;

	public bool Equals(lua_State other) => Handle == other.Handle;
	public override bool Equals(object? other) => other is lua_State state && Equals(state);
	public override int GetHashCode() => Handle.GetHashCode();
}

[InlineArray(LUA_IDSIZE)]
public struct LuaShortSource
{
	public const int LUA_IDSIZE = 128;
	byte element;
}

[StructLayout(LayoutKind.Sequential)]
public struct lua_Debug
{
	public int Event;
	public nint NamePtr;
	public nint NameWhatPtr;
	public nint WhatPtr;
	public nint SourcePtr;
	public int CurrentLine;
	public int NUps;
	public int LineDefined;
	public int LastLineDefined;
	public LuaShortSource ShortSourceBytes;
	public int i_ci;

	public readonly string? Name => Marshal.PtrToStringUTF8(NamePtr);
	public readonly string? NameWhat => Marshal.PtrToStringUTF8(NameWhatPtr);
	public readonly string? What => Marshal.PtrToStringUTF8(WhatPtr);
	public readonly string? Source => Marshal.PtrToStringUTF8(SourcePtr);

	public readonly string ShortSource {
		get {
			ReadOnlySpan<byte> bytes = ShortSourceBytes;
			int len = bytes.IndexOf((byte)0);
			return Encoding.UTF8.GetString(len < 0 ? bytes : bytes[..len]);
		}
	}
}

#pragma warning restore IDE1006 // Naming Styles
