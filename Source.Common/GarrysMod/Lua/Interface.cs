using System;
using System.Collections.Generic;
using System.Text;

namespace Source.Common.GarrysMod.Lua;

#pragma warning disable IDE1006 // Naming Styles
public struct lua_State : IEquatable<lua_State>
#pragma warning restore IDE1006 // Naming Styles
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
