using Source.Common.Lua;

using System.Runtime.InteropServices;

public class CLuaInterface : ILuaInterface
{
	private	IntPtr pointer;

	[DllImport("lua-shared", CallingConvention = CallingConvention.Cdecl)]
	private static extern bool LuaInterface_Init(IntPtr pointer, bool isServer);

	public CLuaInterface(IntPtr pointer) {
		this.pointer = pointer;
	}

	public void Init(bool isServer) {
		LuaInterface_Init(pointer, isServer);
	}
}
