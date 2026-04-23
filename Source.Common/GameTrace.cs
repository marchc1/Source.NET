using Source.Common.Formats.BSP;
using Source.Common.Mathematics;

using System.Numerics;
using System.Runtime.CompilerServices;

namespace Source.Common;

public enum DispSurfFlags : ushort
{
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
	public DispSurfFlags DispFlags;

	public bool AllSolid;
	public bool StartSolid;

	public IHandleEntity? EntHandle;
	// It is up to respective game realms to implement their Ent field as an extension method.
	// This deviates from Source, but is necessary given this architecture.

	public float FractionLeftSolid;
	public CollisionSurface Surface;

	public int HitGroup;
	public short PhysicsBone;

	public int HitBox;

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public readonly bool DidHit() => Fraction < 1 || AllSolid || StartSolid;

	public readonly bool IsDispSurface() => ((DispFlags & DispSurfFlags.Surface) != 0);
	public readonly bool IsDispSurfaceWalkable() => ((DispFlags & DispSurfFlags.Walkable) != 0);
	public readonly bool IsDispSurfaceBuildable() => ((DispFlags & DispSurfFlags.Buildable) != 0);
	public readonly bool IsDispSurfaceProp1() => ((DispFlags & DispSurfFlags.SurfProp1) != 0);
	public readonly bool IsDispSurfaceProp2() => ((DispFlags & DispSurfFlags.SurfProp2) != 0);
}

public static class GameTraceExts
{
	public static bool IsNull(this ref GameTrace tr) => Unsafe.IsNullRef(ref tr);
}

public class TraceListData;
