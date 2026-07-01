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

	public static bool IsBoxIntersectingBox(in Vector3 boxMin1, in Vector3 boxMax1, in Vector3 boxMin2, in Vector3 boxMax2) {
		if ((boxMin1[0] > boxMax2[0]) || (boxMax1[0] < boxMin2[0]))
			return false;
		if ((boxMin1[1] > boxMax2[1]) || (boxMax1[1] < boxMin2[1]))
			return false;
		if ((boxMin1[2] > boxMax2[2]) || (boxMax1[2] < boxMin2[2]))
			return false;
		return true;
	}

	public static bool IsBoxIntersectingSphereExtents(in Vector3 boxCenter, in Vector3 boxHalfDiag, in Vector3 center, float radius) {
		float dmin = 0.0f;
		float delta, diff;

		diff = MathF.Abs(center.X - boxCenter.X);
		if (diff > boxHalfDiag.X) {
			delta = diff - boxHalfDiag.X;
			dmin += delta * delta;
		}

		diff = MathF.Abs(center.Y - boxCenter.Y);
		if (diff > boxHalfDiag.Y) {
			delta = diff - boxHalfDiag.Y;
			dmin += delta * delta;
		}

		diff = MathF.Abs(center.Z - boxCenter.Z);
		if (diff > boxHalfDiag.Z) {
			delta = diff - boxHalfDiag.Z;
			dmin += delta * delta;
		}

		return dmin < radius * radius;
	}

	public static bool IsPointInBox(in Vector3 pt, in Vector3 boxMin, in Vector3 boxMax) {
		Assert(boxMin.X <= boxMax.X && boxMin.Y <= boxMax.Y && boxMin.Z <= boxMax.Z);
		return pt.X >= boxMin.X && pt.X <= boxMax.X && pt.Y >= boxMin.Y && pt.Y <= boxMax.Y && pt.Z >= boxMin.Z && pt.Z <= boxMax.Z;
	}

	private static void FindMinMax(float v1, float v2, float v3, out float min, out float max) {
		min = max = v1;
		if (v2 < min) min = v2; else if (v2 > max) max = v2;
		if (v3 < min) min = v3; else if (v3 > max) max = v3;
	}

	// Separating-axis edge tests (triangle edge crossed with an axial axis). Returns false on a separating axis.
	private static bool AxisTestEdgeCrossX(float edgeZ, float edgeY, float absEdgeZ, float absEdgeY, in Vector3 pA, in Vector3 pB, in Vector3 ext, float tol) {
		float distA = edgeZ * pA.Y - edgeY * pA.Z;
		float distB = edgeZ * pB.Y - edgeY * pB.Z;
		float distBox = absEdgeZ * ext.Y + absEdgeY * ext.Z;
		if (distA < distB) {
			if ((distA > (distBox + tol)) || (distB < -(distBox + tol))) return false;
		}
		else {
			if ((distB > (distBox + tol)) || (distA < -(distBox + tol))) return false;
		}
		return true;
	}

	private static bool AxisTestEdgeCrossY(float edgeZ, float edgeX, float absEdgeZ, float absEdgeX, in Vector3 pA, in Vector3 pB, in Vector3 ext, float tol) {
		float distA = -edgeZ * pA.X + edgeX * pA.Z;
		float distB = -edgeZ * pB.X + edgeX * pB.Z;
		float distBox = absEdgeZ * ext.X + absEdgeX * ext.Z;
		if (distA < distB) {
			if ((distA > (distBox + tol)) || (distB < -(distBox + tol))) return false;
		}
		else {
			if ((distB > (distBox + tol)) || (distA < -(distBox + tol))) return false;
		}
		return true;
	}

	private static bool AxisTestEdgeCrossZ(float edgeY, float edgeX, float absEdgeY, float absEdgeX, in Vector3 pA, in Vector3 pB, in Vector3 ext, float tol) {
		float distA = edgeY * pA.X - edgeX * pA.Y;
		float distB = edgeY * pB.X - edgeX * pB.Y;
		float distBox = absEdgeY * ext.X + absEdgeX * ext.Y;
		if (distB < distA) {
			if ((distB > (distBox + tol)) || (distA < -(distBox + tol))) return false;
		}
		else {
			if ((distA > (distBox + tol)) || (distB < -(distBox + tol))) return false;
		}
		return true;
	}

	// Separating-Axis Theorem test for an AABB (center/extents) vs. a triangle (with its precomputed plane).
	public static bool IsBoxIntersectingTriangle(in Vector3 boxCenter, in Vector3 boxExtents, in Vector3 v1, in Vector3 v2, in Vector3 v3, in CollisionPlane plane, float tolerance) {
		// Test the axial planes (x,y,z) against the min/max of the triangle.
		Vector3 p1, p2, p3;

		p1.X = v1.X - boxCenter.X; p2.X = v2.X - boxCenter.X; p3.X = v3.X - boxCenter.X;
		FindMinMax(p1.X, p2.X, p3.X, out float min, out float max);
		if ((min > (boxExtents.X + tolerance)) || (max < -(boxExtents.X + tolerance))) return false;

		p1.Y = v1.Y - boxCenter.Y; p2.Y = v2.Y - boxCenter.Y; p3.Y = v3.Y - boxCenter.Y;
		FindMinMax(p1.Y, p2.Y, p3.Y, out min, out max);
		if ((min > (boxExtents.Y + tolerance)) || (max < -(boxExtents.Y + tolerance))) return false;

		p1.Z = v1.Z - boxCenter.Z; p2.Z = v2.Z - boxCenter.Z; p3.Z = v3.Z - boxCenter.Z;
		FindMinMax(p1.Z, p2.Z, p3.Z, out min, out max);
		if ((min > (boxExtents.Z + tolerance)) || (max < -(boxExtents.Z + tolerance))) return false;

		// Test the 9 edge cases.
		Vector3 edge, absEdge;

		// edge 0 (p2 - p1)
		edge = p2 - p1;
		absEdge.Y = MathF.Abs(edge.Y); absEdge.Z = MathF.Abs(edge.Z);
		if (!AxisTestEdgeCrossX(edge.Z, edge.Y, absEdge.Z, absEdge.Y, p1, p3, boxExtents, tolerance)) return false;
		absEdge.X = MathF.Abs(edge.X);
		if (!AxisTestEdgeCrossY(edge.Z, edge.X, absEdge.Z, absEdge.X, p1, p3, boxExtents, tolerance)) return false;
		if (!AxisTestEdgeCrossZ(edge.Y, edge.X, absEdge.Y, absEdge.X, p2, p3, boxExtents, tolerance)) return false;

		// edge 1 (p3 - p2)
		edge = p3 - p2;
		absEdge.Y = MathF.Abs(edge.Y); absEdge.Z = MathF.Abs(edge.Z);
		if (!AxisTestEdgeCrossX(edge.Z, edge.Y, absEdge.Z, absEdge.Y, p1, p2, boxExtents, tolerance)) return false;
		absEdge.X = MathF.Abs(edge.X);
		if (!AxisTestEdgeCrossY(edge.Z, edge.X, absEdge.Z, absEdge.X, p1, p2, boxExtents, tolerance)) return false;
		if (!AxisTestEdgeCrossZ(edge.Y, edge.X, absEdge.Y, absEdge.X, p1, p3, boxExtents, tolerance)) return false;

		// edge 2 (p1 - p3)
		edge = p1 - p3;
		absEdge.Y = MathF.Abs(edge.Y); absEdge.Z = MathF.Abs(edge.Z);
		if (!AxisTestEdgeCrossX(edge.Z, edge.Y, absEdge.Z, absEdge.Y, p1, p2, boxExtents, tolerance)) return false;
		absEdge.X = MathF.Abs(edge.X);
		if (!AxisTestEdgeCrossY(edge.Z, edge.X, absEdge.Z, absEdge.X, p1, p2, boxExtents, tolerance)) return false;
		if (!AxisTestEdgeCrossZ(edge.Y, edge.X, absEdge.Y, absEdge.X, p2, p3, boxExtents, tolerance)) return false;

		// Test against the triangle face plane.
		Vector3 vecMin = boxCenter - boxExtents;
		Vector3 vecMax = boxCenter + boxExtents;
		if (MathLib.BoxOnPlaneSide(vecMin, vecMax, plane) != 3) return false;

		return true;
	}
}
