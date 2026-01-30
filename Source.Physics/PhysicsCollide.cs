using Source.Common;
using Source.Common.Formats.BSP;
using Source.Common.Mathematics;
using Source.Common.Physics;

using System;
using System.Collections.Generic;
using System.Text;

namespace Source.Physics;

public class PhysicsCollide : IPhysicsCollision
{
	public PhysCollide BBoxToCollide(in AngularImpulse mins, in AngularImpulse maxs) {
		throw new NotImplementedException();
	}

	public PhysConvex BBoxToConvex(in AngularImpulse mins, in AngularImpulse maxs) {
		throw new NotImplementedException();
	}

	public void CollideGetAABB(out AngularImpulse mins, out AngularImpulse maxs, PhysCollide collide, in AngularImpulse collideOrigin, in QAngle collideAngles) {
		throw new NotImplementedException();
	}

	public AngularImpulse CollideGetExtent(PhysCollide collide, in AngularImpulse collideOrigin, in QAngle collideAngles, in AngularImpulse direction) {
		throw new NotImplementedException();
	}

	public void CollideGetMassCenter(PhysCollide collide, out AngularImpulse outMassCenter) {
		throw new NotImplementedException();
	}

	public AngularImpulse CollideGetOrthographicAreas(PhysCollide collide) {
		throw new NotImplementedException();
	}

	public int CollideIndex(PhysCollide collide) {
		throw new NotImplementedException();
	}

	public void CollideSetMassCenter(PhysCollide collide, in AngularImpulse massCenter) {
		throw new NotImplementedException();
	}

	public void CollideSetOrthographicAreas(PhysCollide collide, in AngularImpulse areas) {
		throw new NotImplementedException();
	}

	public int CollideSize(PhysCollide collide) {
		throw new NotImplementedException();
	}

	public float CollideSurfaceArea(PhysCollide collide) {
		throw new NotImplementedException();
	}

	public float CollideVolume(PhysCollide collide) {
		throw new NotImplementedException();
	}

	public int CollideWrite(Span<byte> dest, PhysCollide collide, bool swap = false) {
		throw new NotImplementedException();
	}

	public PhysCollide ConvertConvexToCollide(Span<PhysConvex> convex) {
		throw new NotImplementedException();
	}

	public PhysCollide ConvertConvexToCollideParams(Span<PhysConvex> convex, in ConvertConvexParams convertParams) {
		throw new NotImplementedException();
	}

	public PhysCollide ConvertPolysoupToCollide(PhysPolysoup soup, bool useMOPP) {
		throw new NotImplementedException();
	}

	public void ConvexesFromConvexPolygon(in AngularImpulse polyNormal, ReadOnlySpan<AngularImpulse> points, int pointCount, Span<PhysConvex> output) {
		throw new NotImplementedException();
	}

	public void ConvexFree(PhysConvex convex) {
		throw new NotImplementedException();
	}

	public PhysConvex ConvexFromConvexPolyhedron<T>(in T convexPolyhedron) where T : IPolyhedron {
		throw new NotImplementedException();
	}

	public PhysConvex ConvexFromPlanes(Span<float> planes, float mergeDistance) {
		throw new NotImplementedException();
	}

	public PhysConvex ConvexFromVerts(Span<AngularImpulse> verts) {
		throw new NotImplementedException();
	}

	public float ConvexSurfaceArea(PhysConvex convex) {
		throw new NotImplementedException();
	}

	public float ConvexVolume(PhysConvex convex) {
		throw new NotImplementedException();
	}

	public int CreateDebugMesh(PhysCollide collisionModel, Span<AngularImpulse> outVerts) {
		throw new NotImplementedException();
	}

	public ICollisionQuery CreateQueryModel(PhysCollide collide) {
		throw new NotImplementedException();
	}

	public PhysCollide CreateVirtualMesh(in VirtualMeshParams meshParams) {
		throw new NotImplementedException();
	}

	public void DestroyCollide(PhysCollide collide) {
		throw new NotImplementedException();
	}

	public void DestroyDebugMesh(int vertCount, Span<AngularImpulse> outVerts) {
		throw new NotImplementedException();
	}

	public void DestroyQueryModel(ICollisionQuery query) {
		throw new NotImplementedException();
	}

	public bool GetBBoxCacheSize(out uint cachedSize, out nint cachedCount) {
		throw new NotImplementedException();
	}

	public int GetConvexesUsedInCollideable(PhysCollide collideable, Span<PhysConvex> outputArray) {
		throw new NotImplementedException();
	}

	public bool IsBoxIntersectingCone(in AngularImpulse boxAbsMins, in AngularImpulse boxAbsMaxs, in TruncatedCone cone) {
		throw new NotImplementedException();
	}

	public void OutputDebugInfo(PhysCollide collide) {
		throw new NotImplementedException();
	}

	public Polyhedron PolyhedronFromConvex(PhysConvex convex, bool useTempPolyhedron) {
		throw new NotImplementedException();
	}

	public void PolysoupAddTriangle(PhysPolysoup soup, in AngularImpulse a, in AngularImpulse b, in AngularImpulse c, int materialIndex7bits) {
		throw new NotImplementedException();
	}

	public PhysPolysoup PolysoupCreate() {
		throw new NotImplementedException();
	}

	public void PolysoupDestroy(PhysPolysoup soup) {
		throw new NotImplementedException();
	}

	public uint ReadStat(int statID) {
		throw new NotImplementedException();
	}

	public void SetConvexGameData(PhysConvex convex, uint gameData) {
		throw new NotImplementedException();
	}

	public bool SupportsVirtualMesh() {
		throw new NotImplementedException();
	}

	public IPhysicsCollision ThreadContextCreate() {
		throw new NotImplementedException();
	}

	public void ThreadContextDestroy(IPhysicsCollision threadContext) {
		throw new NotImplementedException();
	}

	public void TraceBox(in AngularImpulse start, in AngularImpulse end, in AngularImpulse mins, in AngularImpulse maxs, PhysCollide collide, in AngularImpulse collideOrigin, in QAngle collideAngles, out Trace trace) {
		throw new NotImplementedException();
	}

	public void TraceBox(in Ray ray, PhysCollide collide, in AngularImpulse collideOrigin, in QAngle collideAngles, out Trace trace) {
		throw new NotImplementedException();
	}

	public void TraceBox(in Ray ray, Contents contentsMask, IConvexInfo? convexInfo, PhysCollide collide, in AngularImpulse collideOrigin, in QAngle collideAngles, out Trace trace) {
		throw new NotImplementedException();
	}

	public void TraceCollide(in AngularImpulse start, in AngularImpulse end, PhysCollide pSweepCollide, in QAngle sweepAngles, PhysCollide collide, in AngularImpulse collideOrigin, in QAngle collideAngles, out Trace trace) {
		throw new NotImplementedException();
	}

	public PhysCollide UnserializeCollide(ReadOnlySpan<byte> buffer, int size, int index) {
		throw new NotImplementedException();
	}

	public void VCollideLoad(VCollide output, int solidCount, ReadOnlySpan<byte> buffer, bool swap = false) {
		throw new NotImplementedException();
	}

	public void VCollideUnload(VCollide vCollide) {
		throw new NotImplementedException();
	}

	public IVPhysicsKeyParser VPhysicsKeyParserCreate(ReadOnlySpan<byte> keyData) {
		throw new NotImplementedException();
	}

	public void VPhysicsKeyParserDestroy(IVPhysicsKeyParser parser) {
		throw new NotImplementedException();
	}
}
