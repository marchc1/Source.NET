using System.Runtime.InteropServices;
using Source.Common.Lua;


public class CLuaShared : ILuaShared
{
	[DllImport("lua-shared", CallingConvention = CallingConvention.Cdecl)]
	private static extern void LuaShared_Init();
	[DllImport("lua-shared", CallingConvention = CallingConvention.Cdecl)]
	private static extern IntPtr LuaShared_CreateLuaInterface(byte nRealm);

	public void Init()
	{
		LuaShared_Init();
	}

	public ILuaInterface CreateLuaInterface(byte realm)
	{
		return new CLuaInterface(LuaShared_CreateLuaInterface(realm));
	}
}
