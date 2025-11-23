using System.Runtime.InteropServices;
using Source.Common.Lua;


public class CLuaShared : ILuaShared
{
	[DllImport("lua-shared", CallingConvention = CallingConvention.Cdecl)]
	private static extern void LuaShared_Init();

	public void Init()
	{
		LuaShared_Init();
	}
}
