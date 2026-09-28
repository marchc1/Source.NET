using Jitter2.LinearMath;

using Source.Common.Formats.BSP;
using Source.Common.Mathematics;
using Source.Common.Physics;

using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Source.Physics;

public struct TraceRay
{
	public Vector3 Start;
	public Vector3 End;
	public Vector3 Delta;
	public Vector3 Dir;

	public float Length;
	public float BaseLength;
	public float OOBaseLength;
	public float BestDist;
}

public ref struct TraceSolver<ITO_Sweep, ITO_Obstacle>
		where ITO_Sweep : ITraceObject
		where ITO_Obstacle : ITraceObject
{
	public TraceSolver(ref Trace ptr, ref ITO_Sweep sweepObject, ref TraceRay ray, ref ITO_Obstacle obstacle, in Vector3 axis) {

	}

	public Trace Trace;
	public Vector3 PointClosestToIntersection;
	public ref ITO_Sweep SweepObject;
	public ref ITO_Obstacle Obstacale;
	public ref TraceRay Ray;
	public ref Trace TotalTrace;
	public float TraceLength;
	public float TotalTraceLength;
	public float SweepObjectRadius;
	public float Epsilon;
}

public class DefConvexInfo : IConvexInfo
{
	public uint GetContents(int convexGameData) => (uint)Contents.Solid;
}

public ref struct TraceSolverSweptObject<ITO_Sweep>
		where ITO_Sweep : ITraceObject
{
	public TraceSolver<ITO_Sweep, TraceJitter> Base;
	public ref TraceJitter ObstacleJitter;
	public IConvexInfo ConvexInfo;
	public Contents ContentsMask;
	public static readonly DefConvexInfo FakeConvexInfo = new();

	public Vector3 RayCenterOS;
	public Vector3 RayStartOS;
	public Vector3 RayDirOS;
	public Vector3 RayDeltaOS;
	public float RayLengthOS;

	public TraceSolverSweptObject(ref Trace ptr, ref ITO_Sweep sweepobject, ref TraceRay ray, ref TraceJitter obstacle, in Vector3 axis, Contents contentsMask, IConvexInfo? convexInfo) {
		Base = new(ref ptr, ref sweepobject, ref ray, ref obstacle, in axis);
		ObstacleJitter = ref obstacle;
		ConvexInfo = convexInfo != null ? convexInfo : FakeConvexInfo;
	}
}

public struct TraceCone : ITraceObject
{
	public TruncatedCone Cone;
	public float Radius;
	public float SinTheta;
	public Vector3 CenterBase;

	public TraceCone(in TruncatedCone cone, in Vector3 translation) {
		Cone = cone;
		Cone.Origin += translation;
		float cosTheta;
		MathLib.SinCos(MathLib.DEG2RAD(Cone.Theta), out SinTheta, out cosTheta);
		Radius = Cone.H * SinTheta / cosTheta;
		CenterBase = Cone.Origin + Cone.H * Cone.Normal;
	}

	public Vector3 GetVertByIndex(int index) => Cone.Origin;
	public ushort SupportMap(in Vector3 dir, out Vector3 outVec) {
		Vector3 unitDir = dir;
		MathLib.VectorNormalize(ref unitDir);

		float dot = MathLib.DotProduct(unitDir, Cone.Normal);

		// anti-cone is -normal, angle = 90 - theta
		// If the normal is in the anti-cone, then return the apex

		// not in anti-cone, support map is on the surface of the disc
		if (dot > -SinTheta) {
			unitDir -= Cone.Normal * dot;
			float len = MathLib.VectorNormalize(ref unitDir);
			if (len > 1e-4f) {
				outVec = CenterBase + (unitDir * Radius);
				return 0;
			}
			outVec = CenterBase;
			return 0;


		}
		// outside the cone's angle, support map is on the surface of the cone
		outVec = Cone.Origin;
		return 0;
	}

	float ITraceObject.Radius() => Cone.H + Radius;
}

public struct TraceAABB : ITraceObject
{
	public InlineArray2<float> X;
	public InlineArray2<float> Y;
	public InlineArray2<float> Z;
	public float Radius;
	public bool Empty;

	public TraceAABB(in Vector3 hlmins, in Vector3 hlmaxs, bool isPoint) {
		if (isPoint) {
			X[0] = X[1] = 0;
			Y[0] = Y[1] = 0;
			Z[0] = Z[1] = 0;
			Radius = 0;
			Empty = true;
		}
		else {
			X[0] = hlmaxs[0];
			X[1] = hlmins[0];
			Y[0] = hlmaxs[1];
			Y[1] = hlmins[1];
			Z[0] = hlmaxs[2];
			Z[1] = hlmins[2];
			Radius = hlmaxs.Length();
			Empty = false;
		}
	}

	public Vector3 GetVertByIndex(int index) {
		Vector3 outVec;
		outVec.X = X[(index & 1)];
		outVec.Y = Y[(index & 2) >> 1];
		outVec.Z = Z[(index & 4) >> 2];

		return outVec;
	}

	public ushort SupportMap(in Vector3 dir, out Vector3 outVec) {
		if (Empty) {
			outVec = default;
			return 0;
		}
		// index is formed by the 3-bit bitfield SzSySx (negative is 1, positive is 0)
		ReadOnlySpan<uint> dirUint = const_reinterpret<float, uint>(dir.ReadOnlyBase());
		ushort x = unchecked((ushort)((dirUint[0] & 0x80000000U) >> 31));
		ushort y = unchecked((ushort)((dirUint[1] & 0x80000000U) >> 31));
		ushort z = unchecked((ushort)((dirUint[2] & 0x80000000U) >> 31));
		outVec.X = X[x];
		outVec.Y = Y[y];
		outVec.Z = Z[z];
		return unchecked((ushort)((z << 2) | (y << 1) | x));
	}

	float ITraceObject.Radius() => Radius;
}
public struct TraceJitter : ITraceObject
{
	public Vector3 GetVertByIndex(int index) {
		throw new NotImplementedException();
	}

	public float Radius() {
		throw new NotImplementedException();
	}

	public ushort SupportMap(in Vector3 dir, out Vector3 outVec) {
		throw new NotImplementedException();
	}
}
