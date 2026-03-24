using Source.Common.Mathematics;

using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

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
	public static bool IsBoxIntersectingRay(in Vector3 boxMin, in Vector3 boxMax, in Vector3 origin, in Vector3 vecDelta, in Vector3 vecInvDelta, float tolerance = 0.0f) {
		Vector128<float> start = MathLib.LoadFloat3(origin);
		Vector128<float> delta = MathLib.LoadFloat3(vecDelta);
		Vector128<float> boxMins = MathLib.LoadFloat3(boxMin);
		Vector128<float> boxMaxs = MathLib.LoadFloat3(boxMax);

		boxMins = MathLib.SubSIMD(boxMins, start);
		boxMaxs = MathLib.SubSIMD(boxMaxs, start);

		// Check to see if both the origin (start point) and the end point (delta) are on the front side
		// of any of the box sides - if so there can be no intersection
		Vector128<float> startOutMins = MathLib.CmpLtSIMD(MathLib.Four_Zeros, boxMins);
		Vector128<float> endOutMins = MathLib.CmpLtSIMD(delta, boxMins);
		Vector128<float> minsMask = MathLib.AndSIMD(startOutMins, endOutMins);
		Vector128<float> startOutMaxs = MathLib.CmpGtSIMD(MathLib.Four_Zeros, boxMaxs);
		Vector128<float> endOutMaxs = MathLib.CmpGtSIMD(delta, boxMaxs);
		Vector128<float> maxsMask = MathLib.AndSIMD(startOutMaxs, endOutMaxs);
		if (MathLib.IsAnyNegative(MathLib.SetWToZeroSIMD(MathLib.OrSIMD(minsMask, maxsMask))))
			return false;

		// now build the per-axis interval of t for intersections
		Vector128<float> epsilon = MathLib.ReplicateX4(tolerance);
		Vector128<float> invDelta = MathLib.LoadFloat3(vecInvDelta);
		boxMins = MathLib.SubSIMD(boxMins, epsilon);
		boxMaxs = MathLib.AddSIMD(boxMaxs, epsilon);

		boxMins = MathLib.MulSIMD(boxMins, invDelta);
		boxMaxs = MathLib.MulSIMD(boxMaxs, invDelta);

		Vector128<float> crossPlane = MathLib.OrSIMD(MathLib.XorSIMD(startOutMins, endOutMins), MathLib.XorSIMD(startOutMaxs, endOutMaxs));
		// only consider axes where we crossed a plane
		boxMins = MathLib.MaskedAssign(crossPlane, boxMins, MathLib.Four_Negative_FLT_MAX);
		boxMaxs = MathLib.MaskedAssign(crossPlane, boxMaxs, MathLib.Four_FLT_MAX);

		// now sort the interval per axis
		Vector128<float> mint = MathLib.MinSIMD(boxMins, boxMaxs);
		Vector128<float> maxt = MathLib.MaxSIMD(boxMins, boxMaxs);

		// now find the intersection of the intervals on all axes
		Vector128<float> firstOut = MathLib.FindLowestSIMD3(maxt);
		Vector128<float> lastIn = MathLib.FindHighestSIMD3(mint);
		// NOTE: This is really a scalar quantity now [t0,t1] == [lastIn,firstOut]
		firstOut = MathLib.MinSIMD(firstOut, MathLib.Four_Ones);
		lastIn = MathLib.MaxSIMD(lastIn, MathLib.Four_Zeros);

		// If the final interval is valid lastIn<firstOut, check for separation
		Vector128<float> separation = MathLib.CmpGtSIMD(lastIn, firstOut);

		return MathLib.IsAllZeros(separation);
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
