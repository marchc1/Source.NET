using Source.Common.Mathematics;

using System.Numerics;
using System.Runtime.CompilerServices;

namespace Source.Common;

[InlineArray(SurfInfo.MAX_SURFINFO_VERTS)] public struct InlineArrayMaxSurfInfoVerts<T> { T first; }

public struct SurfInfo {
	public const int MAX_SURFINFO_VERTS = 16;
	
	public InlineArrayMaxSurfInfoVerts<Vector3> Verts;
	public uint Count;
	public VPlane Plane;
	public object? EngineData;
}
