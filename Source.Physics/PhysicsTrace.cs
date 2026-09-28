using Source.Common;
using Source.Common.Formats.BSP;
using Source.Common.Mathematics;
using Source.Common.Physics;

using Jitter2.Collision;
using Jitter2.LinearMath;

using System.Diagnostics;
using System.Numerics;

namespace Source.Physics;

public interface ITraceObject {
	ushort SupportMap(in Vector3 dir, out Vector3 outVec);
	Vector3 GetVertByIndex(int index);
	float Radius();
}

public sealed class TraceHull : ISupportMappable
{
	public readonly JVector[] Verts;
	public readonly JVector Center;
	public readonly Vector3 Mins;
	public readonly Vector3 Maxs;
	public readonly int GameData;

	public TraceHull(Vector3[] simVerts, int gameData) {
		GameData = gameData;
		Verts = new JVector[simVerts.Length];
		Mins = new(float.MaxValue);
		Maxs = new(float.MinValue);
		JVector sum = JVector.Zero;
		for (int i = 0; i < simVerts.Length; i++) {
			Vector3 hl = IVPConvert.PositionToHL(simVerts[i]);
			Verts[i] = new JVector(hl.X, hl.Y, hl.Z);
			sum += Verts[i];
			Mins = Vector3.Min(Mins, hl);
			Maxs = Vector3.Max(Maxs, hl);
		}
		Center = simVerts.Length > 0 ? sum * (1.0 / simVerts.Length) : JVector.Zero;
	}

	public void SupportMap(in JVector direction, out JVector result) {
		double best = double.MinValue;
		result = Center;
		for (int i = 0; i < Verts.Length; i++) {
			double d = JVector.Dot(Verts[i], direction);
			if (d > best) {
				best = d;
				result = Verts[i];
			}
		}
	}

	public void GetCenter(out JVector point) => point = Center;
}

public readonly struct TraceBoxSupport(in Vector3 extents) : ISupportMappable
{
	readonly JVector Extents = new(extents.X, extents.Y, extents.Z);

	public void SupportMap(in JVector direction, out JVector result) {
		result = new JVector(
			direction.X >= 0 ? Extents.X : -Extents.X,
			direction.Y >= 0 ? Extents.Y : -Extents.Y,
			direction.Z >= 0 ? Extents.Z : -Extents.Z);
	}

	public void GetCenter(out JVector point) => point = JVector.Zero;
}

public class PhysicsTrace {
	public void SweepBox( in Vector3 start, in Vector3 end, in Vector3 mins, in Vector3 maxs, PhysCollide surface, in Vector3 surfaceOrigin, in QAngle surfaceAngles, out Trace ptr ){
		Ray ray = default;
		ray.Init(start, end, mins, maxs);
		SweepBox(ray, unchecked((Contents)Mask.All), null, surface, surfaceOrigin, surfaceAngles, out ptr);
	}

	public void SweepBox( in Ray raySrc, Contents contentsMask, IConvexInfo? convexInfo, PhysCollide surface, in Vector3 surfaceOrigin, in QAngle surfaceAngles, out Trace ptr ){
		ptr = default;
		ptr.Fraction = 1.0f;
		ptr.FractionLeftSolid = 0;
		MathLib.VectorAdd(raySrc.Start, raySrc.StartOffset, out ptr.StartPos);
		MathLib.VectorAdd(ptr.StartPos, raySrc.Delta, out ptr.EndPos);

		if (surface is not PhysCollideCompactSurface compact)
			return;

		TraceHull[] hulls = compact.GetTraceHulls();
		if (hulls.Length == 0)
			return;

		MathLib.AngleMatrix(surfaceAngles, surfaceOrigin, out Matrix3x4 surfaceToWorld);
		MathLib.VectorITransform(raySrc.Start, surfaceToWorld, out Vector3 centerLocal);
		MathLib.VectorIRotate(raySrc.Delta, surfaceToWorld, out Vector3 deltaLocal);

		// world -> surface rotation, used as the orientation of the (world aligned) box in surface space
		Matrix4x4 worldToLocal = new(
			surfaceToWorld[0][0], surfaceToWorld[0][1], surfaceToWorld[0][2], 0,
			surfaceToWorld[1][0], surfaceToWorld[1][1], surfaceToWorld[1][2], 0,
			surfaceToWorld[2][0], surfaceToWorld[2][1], surfaceToWorld[2][2], 0,
			0, 0, 0, 1);
		Quaternion q = Quaternion.Normalize(Quaternion.CreateFromRotationMatrix(worldToLocal));
		JQuaternion boxOrientation = new(q.X, q.Y, q.Z, q.W);

		// Bounds of the box in surface space
		Vector3 ext = raySrc.Extents;
		Vector3 extLocal = new(
			MathF.Abs(surfaceToWorld[0][0]) * ext.X + MathF.Abs(surfaceToWorld[1][0]) * ext.Y + MathF.Abs(surfaceToWorld[2][0]) * ext.Z,
			MathF.Abs(surfaceToWorld[0][1]) * ext.X + MathF.Abs(surfaceToWorld[1][1]) * ext.Y + MathF.Abs(surfaceToWorld[2][1]) * ext.Z,
			MathF.Abs(surfaceToWorld[0][2]) * ext.X + MathF.Abs(surfaceToWorld[1][2]) * ext.Y + MathF.Abs(surfaceToWorld[2][2]) * ext.Z);
		Vector3 endLocal = centerLocal + deltaLocal;
		Vector3 sweepMins = Vector3.Min(centerLocal, endLocal) - extLocal - new Vector3(DIST_EPSILON);
		Vector3 sweepMaxs = Vector3.Max(centerLocal, endLocal) + extLocal + new Vector3(DIST_EPSILON);

		TraceBoxSupport box = new(ext);
		JVector startPos = new(centerLocal.X, centerLocal.Y, centerLocal.Z);
		JVector endPos = new(endLocal.X, endLocal.Y, endLocal.Z);
		JVector sweep = new(deltaLocal.X, deltaLocal.Y, deltaLocal.Z);
		JVector backSweep = -sweep;
		bool swept = deltaLocal.LengthSquared() > 0;

		bool startSolid = false;
		bool allSolid = false;
		float fractionLeftSolid = 0;
		float bestFraction = 1.0f;
		Vector3 bestNormal = default;
		Vector3 bestPoint = default;
		Contents startContents = 0;
		Contents bestContents = 0;

		for (int i = 0; i < hulls.Length; i++) {
			TraceHull hull = hulls[i];
			if (hull.Verts.Length == 0)
				continue;

			if (sweepMaxs.X < hull.Mins.X || sweepMins.X > hull.Maxs.X ||
				sweepMaxs.Y < hull.Mins.Y || sweepMins.Y > hull.Maxs.Y ||
				sweepMaxs.Z < hull.Mins.Z || sweepMins.Z > hull.Maxs.Z)
				continue;

			Contents contents = convexInfo != null ? (Contents)convexInfo.GetContents(hull.GameData) : Contents.Solid;
			if ((contents & contentsMask) == 0)
				continue;

			if (NarrowPhase.Overlap(hull, box, boxOrientation, startPos)) {
				startSolid = true;
				startContents = contents;
				if (!swept || NarrowPhase.Overlap(hull, box, boxOrientation, endPos))
					allSolid = true;
				else if (NarrowPhase.Sweep(hull, box, boxOrientation, endPos, backSweep, out _, out _, out _, out double backLambda) && backLambda <= 1.0) {
					float leftSolid = (float)(1.0 - backLambda);
					if (leftSolid > fractionLeftSolid)
						fractionLeftSolid = leftSolid;
				}
				continue;
			}

			if (!swept)
				continue;

			if (!NarrowPhase.Sweep(hull, box, boxOrientation, startPos, sweep, out JVector pointA, out _, out JVector normal, out double lambda))
				continue;

			if (lambda < 0 || lambda > 1.0)
				continue;

			Vector3 n = new((float)normal.X, (float)normal.Y, (float)normal.Z);
			float approach = -Vector3.Dot(deltaLocal, n);
			float fraction = (float)lambda;
			if (approach > 0)
				fraction -= DIST_EPSILON / approach;
			if (fraction < 0)
				fraction = 0;

			if (fraction < bestFraction) {
				bestFraction = fraction;
				bestNormal = n;
				bestPoint = new((float)pointA.X, (float)pointA.Y, (float)pointA.Z);
				bestContents = contents;
			}
		}

		if (startSolid) {
			ptr.StartSolid = true;
			ptr.Contents = startContents;
			ptr.Fraction = 0.0f;
			ptr.EndPos = ptr.StartPos;
			if (allSolid) {
				ptr.AllSolid = true;
				ptr.FractionLeftSolid = 1.0f;
			}
			else {
				ptr.FractionLeftSolid = fractionLeftSolid;
				ptr.StartPos += raySrc.Delta * fractionLeftSolid;
			}
			return;
		}

		if (bestFraction < 1.0f) {
			ptr.Fraction = bestFraction;
			ptr.Contents = bestContents;
			MathLib.VectorRotate(bestNormal, surfaceToWorld, out ptr.Plane.Normal);
			MathLib.VectorTransform(bestPoint, surfaceToWorld, out Vector3 worldPoint);
			ptr.Plane.Dist = Vector3.Dot(worldPoint, ptr.Plane.Normal);
			MathLib.VectorMA(ptr.StartPos, ptr.Fraction, raySrc.Delta, out ptr.EndPos);
		}
	}

	public void Sweep( in Vector3 start, in Vector3 end, PhysCollide sweptSurface, in QAngle sweptAngles, PhysCollide surface, in Vector3 surfaceOrigin, in QAngle surfaceAngles,  out Trace ptr ){
		throw new NotImplementedException();
	}
	
	public void GetAABB(out Vector3 mins, out Vector3 maxs, PhysCollide collide, in Vector3 collideOrigin, in QAngle collideAngles ){
		mins = default;
		maxs = default;
		if (collide is not PhysCollideCompactSurface compact)
			return;

		TraceHull[] hulls = compact.GetTraceHulls();
		MathLib.AngleMatrix(collideAngles, collideOrigin, out Matrix3x4 collideToWorld);
		mins = new(float.MaxValue);
		maxs = new(float.MinValue);
		bool any = false;
		for (int i = 0; i < hulls.Length; i++) {
			JVector[] verts = hulls[i].Verts;
			for (int v = 0; v < verts.Length; v++) {
				MathLib.VectorTransform(new Vector3((float)verts[v].X, (float)verts[v].Y, (float)verts[v].Z), collideToWorld, out Vector3 world);
				mins = Vector3.Min(mins, world);
				maxs = Vector3.Max(maxs, world);
				any = true;
			}
		}

		if (!any) {
			mins = collideOrigin;
			maxs = collideOrigin;
		}
	}

	public Vector3 GetExtent( PhysCollide collide, in Vector3 collideOrigin, in QAngle collideAngles, in Vector3 direction ){
		throw new NotImplementedException();
	}

	public bool IsBoxIntersectingCone( in Vector3 boxAbsMins, in Vector3 boxAbsMaxs, in TruncatedCone cone ){
		throw new NotImplementedException();
	}
}
