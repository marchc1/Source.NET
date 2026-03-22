using BepuUtilities;

using CommunityToolkit.HighPerformance;

using Source.Common;
using Source.Common.Formats.BSP;
using Source.Common.Mathematics;
using Source.Common.Physics;

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;

namespace Source.Physics;

public enum CollideType : short
{
	Poly,
	MOPP,
	Ball,
	Virtual
}

[StructLayout(LayoutKind.Explicit)]
public struct PhysCollideHeader
{
	[FieldOffset(0)] public int VPhysicsID;
	[FieldOffset(4)] public short Version;
	[FieldOffset(6)] public CollideType ModelType;
}

[StructLayout(LayoutKind.Explicit)]
public struct CompactSurfaceHeader
{
	[FieldOffset(0)] public int VPhysicsID;
	[FieldOffset(4)] public short Version;
	[FieldOffset(6)] public CollideType ModelType;
	[FieldOffset(8)] public int SurfaceSize;
	[FieldOffset(12)] public Vector3 DragAxisAreas;
	[FieldOffset(24)] public int AxisMapSize;
}

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

	Span<byte> reallocIfNeeded(ref byte[]? tmpbuf, int requiredBytes) {
		if (tmpbuf == null || tmpbuf.Length < requiredBytes)
			tmpbuf = new byte[requiredBytes];

		return tmpbuf[..requiredBytes];
	}

	public void VCollideLoad(VCollide output, int solidCount, ReadOnlySpan<byte> buffer, bool swap = false) {
		output.ClearInstantiatedReference();
		int position = 0;

		output.SolidCount = (ushort)solidCount;
		output.Solids = new PhysCollide[solidCount];

		int currentSize = 0;
		byte[]? tmpbuf = null;

		for (int i = 0; i < solidCount; i++) {
			int size = 0;
			memcpy<byte>(new Span<int>(ref size).Cast<int, byte>(), buffer[position..(position + sizeof(int))]);
			position += sizeof(int);

			Span<byte> tmp = reallocIfNeeded(ref tmpbuf, size);
			memcpy(tmp, buffer[position..(position + size)]);

			output.Solids[i] = PhysCollideParse.UnserializeFromBuffer(tmp, i, swap);
			position += size;
		}

		output.IsPacked = false;
		int keySize = buffer.Length - position;
		output.KeyValues = new byte[keySize];
		memcpy(output.KeyValues, buffer[position..(position + keySize)]);
		output.DescSize = 0;
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

public class PhysCollideCompactSurface : PhysCollide
{
	ReusableBox<IVP_Compact_Surface>? CompactSurface;
	Vector3 OrthoAreas;
	ReusableBox<CollideMap>? CollideMap;

	private void Init(ReadOnlySpan<byte> buffer, int index, bool swap) {
		CompactSurface = new();
		memcpy(new Span<IVP_Compact_Surface>(ref CompactSurface.Ref()).Cast<IVP_Compact_Surface, byte>(), buffer);
		if (swap) 
			CompactSurface.Ref().ByteSwapAll();
		
		CompactSurface.Ref().dummy[0] = index;
		OrthoAreas.Init(1, 1, 1);
		InitCollideMap();
	}

	private void InitCollideMap() {

	}

	public PhysCollideCompactSurface( ReadOnlySpan<byte> buffer, int index, bool swap = false ) {
		Init(buffer, index, swap);
	}
	public PhysCollideCompactSurface(in CompactSurfaceHeader header, int index, bool swap = false ) {
		Init(new ReadOnlySpan<CompactSurfaceHeader>(in header).Cast<CompactSurfaceHeader, byte>()[1..], index, swap);
	}
	public PhysCollideCompactSurface(ref IVP_Compact_Surface surface){
		throw new NotImplementedException();
	}
}
