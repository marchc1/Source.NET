using System.Runtime.CompilerServices;

namespace Source.Common;

/// <summary>
/// This is a combination of BaseTrace and GameTrace.
/// <br/>
/// Analog of trace_t
/// </summary>
public struct GameTrace
{
	public static ref GameTrace NULL => ref Unsafe.NullRef<GameTrace>();

	public float Fraction;
}

public static class GameTraceExts{
	public static bool IsNull(this ref GameTrace tr) => Unsafe.IsNullRef(ref tr);
}

public class TraceListData;
