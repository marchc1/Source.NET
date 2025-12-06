using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential)]
public struct CSharpInterface
{
	public IntPtr Msg;
	public IntPtr MsgColour;
};

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate void MsgDelegate([MarshalAs(UnmanagedType.LPUTF8Str)] string msg);

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate void MsgColourDelegate([MarshalAs(UnmanagedType.LPUTF8Str)] string msg, int[] color);

	
public class CSharpBridge
{
	[DllImport("lua-shared", CallingConvention = CallingConvention.Cdecl)]
	private static extern void SetupCSharpCallback(ref CSharpInterface iface);

	private static MsgDelegate msgDel;
	private static MsgColourDelegate msgColourDel;

	private static void MsgImpl(string msg)
	{
		Console.WriteLine("[CPP] " + msg);
	}

	private static void MsgColourImpl(string msg, int[] color)
	{
		Console.WriteLine($"[CPP] {msg} (R:{color[0]}, G:{color[1]}, B:{color[2]}, A:{color[3]})");
	}

	public static void InitializeCppCallbacks()
	{
		msgDel = new MsgDelegate(MsgImpl);
		msgColourDel = new MsgColourDelegate(MsgColourImpl);

		CSharpInterface iface = new CSharpInterface {
			Msg = Marshal.GetFunctionPointerForDelegate(msgDel),
			MsgColour = Marshal.GetFunctionPointerForDelegate(msgColourDel)
		};

		SetupCSharpCallback(ref iface);
	}
}
