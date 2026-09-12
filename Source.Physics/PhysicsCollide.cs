using BepuUtilities;

using CommunityToolkit.HighPerformance;

using Source.Common;
using Source.Common.Formats.BSP;
using Source.Common.Formats.Keyvalues;
using Source.Common.Mathematics;
using Source.Common.Physics;

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Source.Physics;

public struct BBoxCache
{
	public Vector3 Mins;
	public Vector3 Maxs;
	public PhysCollideCompactSurface? Collide;
}

public class PhysicsCollide : IPhysicsCollision
{
	public PhysCollide BBoxToCollide(in Vector3 mins, in Vector3 maxs) {
		Vector3 mn = mins * IVPConvert.HL2IVP_FACTOR;
		Vector3 mx = maxs * IVPConvert.HL2IVP_FACTOR;
		Vector3[] hull = [
			new(mn.X, mn.Y, mn.Z), new(mx.X, mn.Y, mn.Z), new(mn.X, mx.Y, mn.Z), new(mx.X, mx.Y, mn.Z),
			new(mn.X, mn.Y, mx.Z), new(mx.X, mn.Y, mx.Z), new(mn.X, mx.Y, mx.Z), new(mx.X, mx.Y, mx.Z),
		];
		return new PhysCollideCompactSurface(hull);
	}

	public PhysConvex BBoxToConvex(in Vector3 mins, in Vector3 maxs) {
		throw new NotImplementedException();
	}

	public void CollideGetAABB(out Vector3 mins, out Vector3 maxs, PhysCollide collide, in Vector3 collideOrigin, in QAngle collideAngles) {
		TraceAPI.GetAABB(out mins, out maxs, collide, in collideOrigin, in collideAngles);
	}

	public Vector3 CollideGetExtent(PhysCollide collide, in Vector3 collideOrigin, in QAngle collideAngles, in Vector3 direction) {
		throw new NotImplementedException();
	}

	public void CollideGetMassCenter(PhysCollide collide, out Vector3 outMassCenter) {
		throw new NotImplementedException();
	}

	public Vector3 CollideGetOrthographicAreas(PhysCollide collide) {
		throw new NotImplementedException();
	}

	public int CollideIndex(PhysCollide collide) {
		throw new NotImplementedException();
	}

	public void CollideSetMassCenter(PhysCollide collide, in Vector3 massCenter) {
		throw new NotImplementedException();
	}

	public void CollideSetOrthographicAreas(PhysCollide collide, in Vector3 areas) {
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

	public void ConvexesFromConvexPolygon(in Vector3 polyNormal, ReadOnlySpan<Vector3> points, int pointCount, Span<PhysConvex> output) {
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

	public PhysConvex ConvexFromVerts(Span<Vector3> verts) {
		throw new NotImplementedException();
	}

	public float ConvexSurfaceArea(PhysConvex convex) {
		throw new NotImplementedException();
	}

	public float ConvexVolume(PhysConvex convex) {
		throw new NotImplementedException();
	}

	private readonly List<Vector3[]> DebugMeshRentals = [];

	public int CreateDebugMesh(PhysCollide collisionModel, out Span<Vector3> outVerts) {
		if (collisionModel is not PhysCollideCompactSurface surface) {
			outVerts = default;
			return 0;
		}

		int vertCount = surface.Triangles.Count;
		if (vertCount == 0) {
			outVerts = default;
			return 0;
		}

		Vector3[] verts = ArrayPool<Vector3>.Shared.Rent(vertCount);
		for (int i = 0; i < vertCount; i++)
			verts[i] = IVPConvert.PositionToHL(surface.Triangles[i]);

		DebugMeshRentals.Add(verts);
		outVerts = verts.AsSpan(0, vertCount);
		return vertCount;
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

	public void DestroyDebugMesh(int vertCount, Span<Vector3> outVerts) {
		if (outVerts.IsEmpty)
			return;

		ref Vector3 first = ref MemoryMarshal.GetReference(outVerts);
		for (int i = 0; i < DebugMeshRentals.Count; i++) {
			if (Unsafe.AreSame(ref DebugMeshRentals[i][0], ref first)) {
				ArrayPool<Vector3>.Shared.Return(DebugMeshRentals[i]);
				DebugMeshRentals.RemoveAt(i);
				return;
			}
		}
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

	public bool IsBoxIntersectingCone(in Vector3 boxAbsMins, in Vector3 boxAbsMaxs, in TruncatedCone cone) {
		throw new NotImplementedException();
	}

	public void OutputDebugInfo(PhysCollide collide) {
		throw new NotImplementedException();
	}

	public Polyhedron PolyhedronFromConvex(PhysConvex convex, bool useTempPolyhedron) {
		throw new NotImplementedException();
	}

	public void PolysoupAddTriangle(PhysPolysoup soup, in Vector3 a, in Vector3 b, in Vector3 c, int materialIndex7bits) {
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

	public void TraceBox(in Vector3 start, in Vector3 end, in Vector3 mins, in Vector3 maxs, PhysCollide collide, in Vector3 collideOrigin, in QAngle collideAngles, out Trace trace) {
		throw new NotImplementedException();
	}

	public void TraceBox(in Ray ray, PhysCollide collide, in Vector3 collideOrigin, in QAngle collideAngles, out Trace trace) {
		throw new NotImplementedException();
	}

	public void TraceBox(in Ray ray, Contents contentsMask, IConvexInfo? convexInfo, PhysCollide collide, in Vector3 collideOrigin, in QAngle collideAngles, out Trace trace) {
		throw new NotImplementedException();
	}

	public void TraceCollide(in Vector3 start, in Vector3 end, PhysCollide pSweepCollide, in QAngle sweepAngles, PhysCollide collide, in Vector3 collideOrigin, in QAngle collideAngles, out Trace trace) {
		throw new NotImplementedException();
	}

	public PhysCollide UnserializeCollide(ReadOnlySpan<byte> buffer, int size, int index) {
		throw new NotImplementedException();
	}

	public void VCollideLoad(VCollide output, int solidCount, ReadOnlySpan<byte> buffer, bool swap = false) {
		output.ClearInstantiatedReference();
		int position = 0;

		output.SolidCount = (ushort)solidCount;
		output.Solids = new PhysCollide[solidCount];

		for (int i = 0; i < solidCount; i++) {
			output.Solids[i] = PhysCollideParse.UnserializeFromBuffer(buffer[position..], i, swap, out int size);
			position += size;
		}

		output.IsPacked = false;
		int keySize = buffer.Length - position;
		output.KeyValues = new byte[keySize];
		memcpy(output.KeyValues, buffer[position..(position + keySize)]);
		output.DescSize = 0;
	}

	public void VCollideUnload(VCollide vCollide) {
		// throw new NotImplementedException();
		// TODO!
	}

	public IVPhysicsKeyParser VPhysicsKeyParserCreate(ReadOnlySpan<byte> keyData) {
		return new VPhysicsKeyParser(keyData);
	}

	public void VPhysicsKeyParserDestroy(IVPhysicsKeyParser parser) {
		// Managed parser; nothing to free.
	}

	private readonly PhysicsTrace TraceAPI = new();
	private readonly List<BBoxCache> BBoxCache = [];
	private readonly byte[] BBoxVertMap = new byte[8];
}

public class PhysCollideCompactSurface : PhysCollide
{
	public readonly List<Vector3[]> ConvexHulls = [];
	public readonly List<Vector3> Triangles = [];
	private unsafe void Init(PhyParser parser, int index, bool swap) {
		parser.ParseSurfaces(ConvexHulls, Triangles);
	}

	public PhysCollideCompactSurface(PhyParser parser, int index, bool swap = false) {
		Init(parser, index, swap);
	}

	public PhysCollideCompactSurface(Vector3[] hull) {
		ConvexHulls.Add(hull);
	}
}
