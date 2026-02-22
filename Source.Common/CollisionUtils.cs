using System.Numerics;
using System.Runtime.CompilerServices;

namespace Source.Common;

public static class CollisionUtils
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float IntersectRayWithTriangle(in Ray ray, in Vector3 v1, in Vector3 v2, in Vector3 v3, bool oneSided) {
		Vector3 edge1 = v2 - v1;
		Vector3 edge2 = v3 - v1;

		if (oneSided) {
			Vector3 normal = Vector3.Cross(edge1, edge2);
			if (Vector3.Dot(normal, ray.Delta) >= 0.0f)
				return -1.0f;
		}

		Vector3 dirCrossEdge2 = Vector3.Cross(ray.Delta, edge2);

		float denom = Vector3.Dot(dirCrossEdge2, edge1);
		if (MathF.Abs(denom) < 1e-6f)
			return -1.0f;
		float invDenom = 1.0f / denom;

		Vector3 org = ray.Start - v1;
		float u = Vector3.Dot(dirCrossEdge2, org) * invDenom;
		if (u < 0.0f || u > 1.0f)
			return -1.0f;

		Vector3 orgCrossEdge1 = Vector3.Cross(org, edge1);
		float v = Vector3.Dot(orgCrossEdge1, ray.Delta) * invDenom;
		if (v < 0.0f || u + v > 1.0f)
			return -1.0f;

		float boxt = ComputeBoxOffset(ray);
		float t = Vector3.Dot(orgCrossEdge1, edge2) * invDenom;
		if (t < -boxt || t > 1.0f + boxt)
			return -1.0f;

		return Math.Clamp(t, 0.0f, 1.0f);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float ComputeBoxOffset(in Ray ray) {
		if (ray.IsRay)
			return 1e-3f;

		float offset =
			Math.Abs(ray.Extents.X * ray.Delta.X) +
			Math.Abs(ray.Extents.Y * ray.Delta.Y) +
			Math.Abs(ray.Extents.Z * ray.Delta.Z);

		float invRSquared = 1.0f / ray.Delta.LengthSquared();
		offset *= invRSquared;

		return offset + 1e-3f;
	}
}