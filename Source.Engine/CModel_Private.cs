using CommunityToolkit.HighPerformance;

using Source.Common;
using Source.Common.Formats.BSP;
using Source.Common.Mathematics;

using System.Numerics;
using System.Runtime.CompilerServices;

using TraceCounter_t = uint;
using TraceCounterVec = System.Collections.Generic.List<uint>;

namespace Source.Engine;

internal class TraceVisits
{
	public AbsolutePlayerLimitBitVec m_Brushes;
	public VarBitVec m_Disps;
}


internal class TraceInfo : IPoolableObject
{
	public const int MAX_CHECK_COUNT_DEPTH = 2;
	public void Init() { }
	public void Reset() { }

	public Vector3 Start;
	public Vector3 End;
	public Vector3 Mins;
	public Vector3 Maxs;
	public Vector3 Extents;
	public Vector3 Delta;
	public Vector3 InvDelta;

	public Trace Trace;
	public Trace StabTrace;

	public Contents Contents;
	public bool IsPoint;
	public bool IsSwept;

	// BSP Data
	public CollisionBSPData? BSPData;

	// Displacement Data
	public Vector3 DispStabDir;       // the direction to stab in
	public int DispHit;             // hit displacement surface last

	public bool CheckPrimary;
	public int CheckDepth;
	public readonly TraceCounter_t[] Count = new TraceCounter_t[MAX_CHECK_COUNT_DEPTH];
	public readonly TraceCounterVec[] BrushCounters = new TraceCounterVec[MAX_CHECK_COUNT_DEPTH];
	public readonly TraceCounterVec[] DispCounters = new TraceCounterVec[MAX_CHECK_COUNT_DEPTH];

	TraceCounter_t GetCount() { return Count[CheckDepth]; }
	Span<TraceCounter_t> GetBrushCounters() { return BrushCounters[CheckDepth].AsSpan(); }
	Span<TraceCounter_t> GetDispCounters() { return DispCounters[CheckDepth].AsSpan(); }
}

public static partial class CM
{
	static readonly ObjectPool<TraceInfo> g_TraceInfoPool = new();
	public static int PointLeafnum(in Vector3 point) {
		CollisionBSPData bspData = GetCollisionBSPData();
		if (bspData.NumPlanes == 0)
			return 0;
		return PointLeafnum_r(bspData, in point, 0);
	}

	public static int PointLeafnum_r(CollisionBSPData bspData, in Vector3 point, int num) {
		float d;
		ref CollisionNode node = ref Unsafe.NullRef<CollisionNode>();
		ref CollisionPlane plane = ref Unsafe.NullRef<CollisionPlane>();

		while (num >= 0) {
			node = ref bspData.MapNodes.AsSpan()[bspData.MapRootNode + num];
			plane = ref bspData.MapPlanes.AsSpan()[node.CollisionPlaneIdx];

			if ((int)plane.Type < 3)
				d = point[(int)plane.Type] - plane.Dist;
			else
				d = Vector3.Dot(plane.Normal, point) - plane.Dist;

			if (d < 0)
				num = node.Children[1];
			else
				num = node.Children[0];
		}

		return -1 - num;
	}

	public static nint g_DispCollTreeCount = 0;

	internal static TraceInfo BeginTrace() {
		TraceInfo traceInfo = g_TraceInfoPool.Alloc();

		if (traceInfo.BrushCounters[0].Count != GetCollisionBSPData().NumBrushes + 1) {
			memreset(traceInfo.Count);
			traceInfo.CheckDepth = -1;

			for (int i = 0; i < TraceInfo.MAX_CHECK_COUNT_DEPTH; i++) {
				traceInfo.BrushCounters[i].SetCount(GetCollisionBSPData().NumBrushes + 1);
				traceInfo.DispCounters[i].SetCount((int)g_DispCollTreeCount);

				memreset(traceInfo.BrushCounters[i].AsSpan());
				memreset(traceInfo.DispCounters[i].AsSpan());
			}
		}


		return traceInfo;
	}

	internal static void UnsweptBoxTrace(TraceInfo traceinfo, in Ray ray, int headnode, Mask brushmask) {

	}
	internal static void RecursiveHullCheck(TraceInfo traceinfo, int num, float p1f, float p2f) {

	}
	internal static void ComputeTraceEndpoints(in Ray ray, ref Trace trace) {

	}
	internal static void BoxTrace(in Ray ray, int headnode, Mask brushmask, bool computeEndPt, ref Trace tr) {
		TraceInfo traceInfo = BeginTrace();

		// fill in a default trace
		ClearTrace(ref traceInfo.Trace);

		traceInfo.BSPData = GetCollisionBSPData();

		// check if the map is not loaded
		if (traceInfo.BSPData.NumNodes == 0) {
			tr = traceInfo.Trace;
			EndTrace(traceInfo);
			return;
		}

		traceInfo.DispHit = 0;
		traceInfo.DispStabDir.Init();
		traceInfo.Contents = (Contents)brushmask;
		MathLib.VectorCopy(ray.Start, out traceInfo.Start);
		MathLib.VectorAdd(ray.Start, ray.Delta, out traceInfo.End);
		MathLib.VectorMultiply(ray.Extents, -1.0f, out traceInfo.Mins);
		MathLib.VectorCopy(ray.Extents, out traceInfo.Maxs);
		MathLib.VectorCopy(ray.Extents, out traceInfo.Extents);
		traceInfo.Delta = ray.Delta;
		traceInfo.InvDelta = ray.InvDelta();
		traceInfo.IsPoint = ray.IsRay;
		traceInfo.IsSwept = ray.IsSwept;

		if (!ray.IsSwept) // check for position test special case
			UnsweptBoxTrace(traceInfo, ray, headnode, brushmask);
		else // general sweeping through world
			RecursiveHullCheck(traceInfo, headnode, 0, 1);

		// Compute the trace start + end points
		if (computeEndPt)
			ComputeTraceEndpoints(ray, ref traceInfo.Trace);

		// Copy off the results
		tr = traceInfo.Trace;
		EndTrace(traceInfo);
		Assert(!ray.IsRay || tr.AllSolid || (tr.Fraction >= tr.FractionLeftSolid));
	}

	private static void EndTrace(TraceInfo traceInfo) {
		g_TraceInfoPool.Free(traceInfo);
	}
}
