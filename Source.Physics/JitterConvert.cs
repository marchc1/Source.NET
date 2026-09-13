using Jitter2.LinearMath;

using System.Numerics;

namespace Source.Physics;

internal static class JitterConvert
{
	public static JVector ToJ(in Vector3 v) => new(v.X, v.Y, v.Z);
	public static Vector3 ToVec(in JVector v) => new((float)v.X, (float)v.Y, (float)v.Z);
	public static JQuaternion ToJ(in Quaternion q) => new(q.X, q.Y, q.Z, q.W);
	public static Quaternion ToQuat(in JQuaternion q) => new((float)q.X, (float)q.Y, (float)q.Z, (float)q.W);
}
