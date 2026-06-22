using Source.Common.Formats.BSP;
using Source.Common.Mathematics;

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;

namespace Source.Common;

public class DispVector<T> : List<T>;

public static class DispCollCommon
{
	public const int MAX_DISP_AABB_NODES = 341;
	public const int MAX_AABB_LIST = 344;
	public const int DISPCOLL_TREETRI_SIZE = BSPFileCommon.MAX_DISPTRIS;
	public const float DISPCOLL_DIST_EPSILON = 0.03125f;
	public const int DISPCOLL_ROOTNODE_INDEX = 0;
	public const int DISPCOLL_INVALID_TRI = -1;
	public const float DISPCOLL_INVALID_FRAC = -99999.9f;
	public const int DISPCOLL_NORMAL_UNDEF = 0xffff;
}

[InlineArray(MAX_DISP_AABB_NODES)] public struct InlineArrayMaxDispAABBNodes<T> { public T? first; }
[InlineArray(MAX_AABB_LIST)] public struct InlineArrayMaxAABBList<T> { public T? first; }

public class DispCollTri
{
	record struct index_t
	{
		private ushort _value;

		public ushort Vert {
			readonly get => (ushort)(_value & 0x1FF);
			set => _value = (ushort)((_value & ~0x1FF) | (value & 0x1FF));
		}

		public ushort Min {
			readonly get => (ushort)((_value >> 9) & 0x3);
			set => _value = (ushort)((_value & ~(0x3 << 9)) | ((value & 0x3) << 9));
		}

		public ushort Max {
			readonly get => (ushort)((_value >> 11) & 0x3);
			set => _value = (ushort)((_value & ~(0x3 << 11)) | ((value & 0x3) << 11));
		}
	}
	InlineArray3<index_t> TriData;
	private ushort _packeddata;
	public ushort SignBits {
		get => (ushort)(_packeddata & 0x7);
		set => _packeddata = (ushort)((_packeddata & ~0x7) | (value & 0x7));
	}

	public ushort PlaneType {
		get => (ushort)((_packeddata >> 3) & 0x7);
		set => _packeddata = (ushort)((_packeddata & ~(0x7 << 3)) | ((value & 0x7) << 3));
	}

	public ushort Flags {
		get => (ushort)((_packeddata >> 6) & 0x1F);
		set => _packeddata = (ushort)((_packeddata & ~(0x1F << 6)) | ((value & 0x1F) << 6));
	}

	public Vector3 Normal;
	public float Dist;

	public DispCollTri() {
		throw new NotImplementedException();
	}

	void Init() {
		throw new NotImplementedException();
	}

	public void CalcPlane() {
		throw new NotImplementedException();
	}

	public void MinMinMax() {
		throw new NotImplementedException();
	}

	public void SetVert(int pos, int vert) {
		Assert((pos >= 0) && (pos < 3));
		Assert((vert >= 0) && (vert < (1 << 9)));
		TriData[pos].Vert = (ushort)vert;
	}

	public int GetVert(int pos) {
		Assert((pos >= 0) && (pos < 3));
		return TriData[pos].Vert;
	}

	public void SetMin(int axis, int min) {
		Assert((axis >= 0) && (axis < 3));
		Assert((min >= 0) && (min < 3));
		TriData[axis].Min = (ushort)min;
	}

	public int GetMin(int axis) {
		Assert((axis >= 0) && (axis < 3));
		return TriData[axis].Min;
	}

	public void SetMax(int axis, int max) {
		Assert((axis >= 0) && (axis < 3));
		Assert((max >= 0) && (max < 3));
		TriData[axis].Max = (ushort)max;
	}

	public int GetMax(int axis) {
		Assert((axis >= 0) && (axis < 3));
		return TriData[axis].Max;
	}
}

public class DispCollHelper
{
	public float StartFrac;
	public float EndFrac;
	public Vector3 ImpactNormal;
	public float ImpactDist;
}

public class DispCollTriCache
{
	public InlineArray3<ushort> CrossX;
	public InlineArray3<ushort> CrossY;
	public InlineArray3<ushort> CrossZ;
}

public class DispCollNode
{
	public FourVectors Mins;
	public FourVectors Maxs;
}

public class DispCollLeaf
{
	public InlineArray2<short> Tris;
}


public struct RayLeafList
{
	public FourVectors RayStart;
	public FourVectors RayExtents;
	public FourVectors InvDelta;
	public InlineArrayMaxAABBList<int> NodeList;
	public int MaxIndex;
}

public class DispCollTree
{
	public Vector3 Mins;
	public int Counter;
	public Vector3 Maxs;
	protected Contents Contents;
	protected int Power;
	protected int Flags;
	protected InlineArray4<Vector3> SurfPoints;
	protected Vector3 StabDir;
	protected InlineArray2<short> SurfaceProps;
	protected readonly DispVector<Vector3> Verts = [];
	protected readonly DispVector<DispCollTri> Tris = [];
	protected readonly DispVector<DispCollNode> Nodes = [];
	protected readonly DispVector<DispCollLeaf> Leaves = [];
	protected readonly DispVector<DispCollTriCache> TrisCache = [];
	protected readonly DispVector<Vector3> EdgePlanes = [];
	protected readonly DispCollHelper Helper = new();
	protected uint Size;


	public bool AABBTree_IntersectAABB(Vector3 absMins, Vector3 absMaxs) {
		return false; // todo enginetrace
	}
	public bool AABBTree_Ray(in Ray ray, in Vector3 invDelta, ref Trace trace, bool side = true) {
		return false; // todo enginetrace
	}

	static Vector3 Vec3DispCollEpsilons = new(DISPCOLL_DIST_EPSILON, DISPCOLL_DIST_EPSILON, DISPCOLL_DIST_EPSILON);
	public bool AABBTree_SweepAABB(in Ray ray, in Vector3 invDelta, ref Trace trace) {
		if (CheckFlags(CoreDispInfo.SURF_NOHULL_COLL))
			return false;

		MathLib.VectorCopy(ray.Delta, out AngularImpulse rayDir);
		MathLib.VectorNormalize(ref rayDir);

		float frac = trace.Fraction;

		RayLeafList list = new() {
			InvDelta = new(invDelta),
			RayStart = new(ray.Start),
			RayExtents = new(ray.Extents + Vec3DispCollEpsilons)
		};
		int listIndex = BuildRayLeafList(0, list);

		if (listIndex <= list.MaxIndex) {
			// todo
		}

		if (trace.Fraction < frac)
			return true;

		return false;
	}

	private int BuildRayLeafList(int v, RayLeafList list) {
		throw new NotImplementedException();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetPower(int power) => Power = power;
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public int GetPower() => Power;

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public int GetFlags() => Flags;
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetFlags(int flags) => Flags = flags;
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public bool CheckFlags(int flags) => (flags & GetFlags()) != 0;

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public int GetWidth() => ((1 << Power) + 1);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public int GetHeight() => ((1 << Power) + 1);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public int GetSize() => ((1 << Power) + 1) * ((1 << Power) + 1);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public int GetTriSize() => ((1 << Power) * (1 << Power) * 2);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public void GetStabDirection(out Vector3 dir) => dir = StabDir;

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public void GetBounds(out Vector3 boxMin, out Vector3 boxMax) { boxMin = Mins; boxMax = Maxs; }
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Contents GetContents() => Contents;
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetSurfaceProps(int prop, short surfProp) => SurfaceProps[prop] = surfProp;
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public short GetSurfaceProps(int prop) => SurfaceProps[prop];

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public uint GetMemorySize() => Size;

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public bool IsCached() => TrisCache.Count == Tris.Count;

	public bool PointInBounds(in Vector3 start, in Vector3 mins, in Vector3 maxs, bool isPoint) {
		if (isPoint)
			return CollisionUtils.IsPointInBox(start, mins, maxs);

		MathLib.VectorSubtract(maxs, mins, out Vector3 extents);
		extents *= 0.5f;

		return CollisionUtils.IsPointInBox(start, mins - extents, maxs + extents);
	}
}
