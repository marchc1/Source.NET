using Source.Common.Formats.BSP;
using Source.Common.Mathematics;

using System.Numerics;
using System.Runtime.CompilerServices;

namespace Source.Common;

public enum DispSurfFlags{
	Surface = DispTriTags.TagSurface,
	Walkable = DispTriTags.TagWalkable,
	Buildable = DispTriTags.TagBuildable,
	SurfProp1 = DispTriTags.FlagSurfProp1,
	SurfProp2 = DispTriTags.FlagSurfProp2,
}

/// <summary>
/// This is a combination of BaseTrace and GameTrace.
/// <br/>
/// Analog of trace_t
/// </summary>
public struct GameTrace
{
	public static ref GameTrace NULL => ref Unsafe.NullRef<GameTrace>();

	public Vector3 StartPos;
	public Vector3 EndPos;
	public CollisionPlane Plane;
	public float Fraction;
	public Contents Contents;
	public ushort DispFlags;

	public bool AllSolid;
	public bool StartSolid;
}

public static class GameTraceExts{
	public static bool IsNull(this ref GameTrace tr) => Unsafe.IsNullRef(ref tr);
}

public class TraceListData;
