using Source.Common;
using Source.Common.Formats.BSP;
using Source.Common.Mathematics;
using Source.Common.Physics;

using System.Diagnostics;
using System.Numerics;

namespace Source.Physics;

public interface ITraceObject {
	ushort SupportMap(in Vector3 dir, out Vector3 outVec);
	Vector3 GetVertByIndex(int index);
	float Radius();
}

public class PhysicsTrace {
	public void SweepBox( in Vector3 start, in Vector3 end, in Vector3 mins, in Vector3 maxs, PhysCollide surface, in Vector3 surfaceOrigin, in QAngle surfaceAngles, out Trace ptr ){
		throw new NotImplementedException();
	}

	public void SweepBox( in Ray raySrc, Contents contentsMask, IConvexInfo convexInfo, PhysCollide surface, in Vector3 surfaceOrigin, in QAngle surfaceAngles, out Trace ptr ){
		throw new NotImplementedException();
	}

	public void Sweep( in Vector3 start, in Vector3 end, PhysCollide sweptSurface, in QAngle sweptAngles, PhysCollide surface, in Vector3 surfaceOrigin, in QAngle surfaceAngles,  out Trace ptr ){
		throw new NotImplementedException();
	}
	
	public void GetAABB(out Vector3 mins, out Vector3 maxs, PhysCollide collide, in Vector3 collideOrigin, in QAngle collideAngles ){
		mins = default;
		maxs = default;
	}

	public Vector3 GetExtent( PhysCollide collide, in Vector3 collideOrigin, in QAngle collideAngles, in Vector3 direction ){
		throw new NotImplementedException();
	}

	public bool IsBoxIntersectingCone( in Vector3 boxAbsMins, in Vector3 boxAbsMaxs, in TruncatedCone cone ){
		throw new NotImplementedException();
	}
}
