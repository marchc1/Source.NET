using Source.Common.Mathematics;

using System.Diagnostics;
using System.Numerics;

namespace Source.Common;

/// <summary>
/// Analog of cmodel_t
/// </summary>
[DebuggerDisplay("Source BSP Collision Model @ {Origin} [{Mins} -> {Maxs}] (head-node {HeadNode})")]
public class CollisionModel
{
	public Vector3 Mins, Maxs, Origin;
	public int HeadNode;
	public readonly VCollide VCollisionData = new();
}

/// <summary>
/// Analog of csurface_t
/// </summary>
[DebuggerDisplay("Source BSP Collision Surface '{Name}' (props: {SurfaceProps}, flags: {Flags})")]
public struct CollisionSurface
{
	public string Name;
	public ushort SurfaceProps;
	public ushort Flags;
}

public struct Ray {
	public Vector3 Start;
	public Vector3 Delta;
	public Vector3 StartOffset;
	public Vector3 Extents;
	public bool IsRay;
	public bool IsSwept;

	public void Init(in Vector3 start, in Vector3 end) {
		MathLib.VectorSubtract(end, start, out Delta);

		IsSwept = (Delta.LengthSqr() != 0);

		MathLib.VectorClear(out Extents);
		IsRay = true;

		// Offset m_Start to be in the center of the box...
		MathLib.VectorClear(out StartOffset);
		MathLib.VectorCopy(start, out Start);
	}

	public void Init(in Vector3 start, in Vector3 end, in Vector3 mins, in Vector3 maxs) {
		MathLib.VectorSubtract(end, start, out Delta);

		IsSwept = (Delta.LengthSqr() != 0);

		MathLib.VectorSubtract(maxs, mins, out Extents);
		Extents *= 0.5f;
		IsRay = (Extents.LengthSqr() < 1e-6);

		// Offset m_Start to be in the center of the box...
		MathLib.VectorAdd(mins, maxs, out StartOffset);
		StartOffset *= 0.5f;
		MathLib.VectorAdd(start, StartOffset, out Start);
		StartOffset *= -1.0f;
	}
}
