using Source.Common.Engine;
using Source.Common.Mathematics;

using System.Numerics;
using System.Reflection.Metadata;

namespace Source.Common.GarrysMod;

public static partial class Lua
{
	public unsafe delegate int CFunc(lua_State* L);

	public enum Special
	{
		Glob,
		Env,
		Reg
	}

	public enum Index
	{
		Global = -10002,
		Environment,
		Registry
	}

	public interface ILuaBase
	{
		// TODO
	}
}
