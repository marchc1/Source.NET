using System.Runtime.CompilerServices;

namespace Source.Common;

public static class ThreadUtils
{
	static Thread? MainThread;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]	public static void SetMainThread() => MainThread = Thread.CurrentThread;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]	public static bool ThreadInMainThread() => Thread.CurrentThread == MainThread!;
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static int ThreadGetCurrentId() => Environment.CurrentManagedThreadId;
}
