using CommunityToolkit.HighPerformance;

using Source.Common.Formats.BSP;

using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Source.Common.Mathematics;

public static class MathLibConsts
{
	public const int PITCH = 0;
	public const int YAW = 1;
	public const int ROLL = 2;

	public static readonly Vector3 vec3_origin = new(0, 0, 0);
	public static readonly QAngle vec3_angle = new(0, 0, 0);
}

public struct RadianEuler {
	public vec_t X, Y, Z;
}

/// <summary>
/// Mostly for data structure compatibility
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 4, Size = 48)]
public struct Matrix3x4 {
	public float M00, M01, M02, M03;
	public float M10, M11, M12, M13;
	public float M20, M21, M22, M23;
	public unsafe float this[int row, int col] {
		get {
			if (row < 0 || row >= 3) throw new ArgumentOutOfRangeException(nameof(row));
			if (col < 0 || col >= 4) throw new ArgumentOutOfRangeException(nameof(col));

			return Unsafe.Add(ref M00, (row * 4) + col);
		}
		set {
			if (row < 0 || row >= 3) throw new ArgumentOutOfRangeException(nameof(row));
			if (col < 0 || col >= 4) throw new ArgumentOutOfRangeException(nameof(col));

			Unsafe.Add(ref M00, (row * 4) + col) = value;
		}
	}

	public Matrix4x4 To4x4() {
		return new(
			M00, M01, M02, M03,
			M10, M11, M12, M13,
			M20, M21, M22, M23,
			0f, 0f, 0f, 1f
			);
	}
}

public enum PlaneType : byte
{
	NormalX = 0,
	NormalY = 4,
	NormalZ = 8,
	Dist = 12,
	Type = 16,
	SignBits = 17,
	Pad0 = 18,
	Pad1 = 19
}

public struct CollisionLeaf
{
	public Contents Contents;
	public short Cluster;

	private short areaFlags;
	public ushort FirstLeafBrush;
	public ushort NumLeafBrushes;
	public ushort DispListStart;
	public ushort DispCount;

	public short Area {
		readonly get => (short)(areaFlags & 0x1FF);
		set => areaFlags = (short)((areaFlags & ~0x1FF) | (value & 0x1FF));
	}

	public short Flags { 
		readonly get => (short)((areaFlags >> 9) & 0x7F);
		set => areaFlags = (short)((areaFlags & ~(0x7F << 9)) | ((value & 0x7F) << 9));
	}
}

public struct CollisionNode {
	public int CollisionPlaneIdx;
	public InlineArray2<int> Children;
}

public struct CollisionPlane
{
	public Vector3 Normal;
	public float Dist;
	public PlaneType Type;
	public byte SignBits;
	public InlineArray2<byte> Pad;
}
public static class MathLib
{
	public static Vector3 AsVector3(this ReadOnlySpan<float> span) => new(span[0], span[1], span[2]);
	public static bool IsValid(this Vector2 vec) => !float.IsNaN(vec.X) && !float.IsNaN(vec.Y);
	public static bool IsValid(this Vector3 vec) => !float.IsNaN(vec.X) && !float.IsNaN(vec.Y) && !float.IsNaN(vec.Z);
	public static bool IsValid(this Vector4 vec) => !float.IsNaN(vec.X) && !float.IsNaN(vec.Y) && !float.IsNaN(vec.Z) && !float.IsNaN(vec.W);
	static MathLib() {

	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ref Vector3 AsVector3D(this ref Vector4 vec)
		=> ref new Span<Vector4>(ref vec).Cast<Vector4, float>()[..3].Cast<float, Vector3>()[0];
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ref Vector2 AsVector2D(this ref Vector4 vec)
		=> ref new Span<Vector4>(ref vec).Cast<Vector4, float>()[..2].Cast<float, Vector2>()[0];


	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float Lerp(float f1, float f2, float i1, float i2, float x) {
		return f1 + (f2 - f1) * (x - i1) / (i2 - i1);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float Fmodf(float x, float y) {
		return x - y * (float)MathF.Truncate(x / y);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int Modulo(int a, int b) {
		return (Math.Abs(a * b) + a) % b;
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static double SimpleSpline(double value) {
		return (value * value) * (3 - 2 * value);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int CeilPow2(int input) {
		int retval = 1;
		while (retval < input)
			retval <<= 2;
		return retval;
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int FloorPow2(int input) {
		int retval = 1;
		while (retval < input)
			retval <<= 1;
		return retval >> 1;
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static nuint CeilPow2(nuint input) {
		nuint retval = 1;
		while (retval < input)
			retval <<= 2;
		return retval;
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static nuint FloorPow2(nuint input) {
		nuint retval = 1;
		while (retval < input)
			retval <<= 1;
		return retval >> 1;
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static double Bias(double x, double biasAmount) {
		double fRet = Math.Pow(x, Math.Log(biasAmount) * -1.4427);
		Assert(!double.IsNaN(fRet));
		return fRet;
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static double Gain(double x, double biasAmount) {
		if (x < 0.5)
			return 0.5f * Bias(2 * x, 1 - biasAmount);
		else
			return 1 - 0.5f * Bias(2 - 2 * x, 1 - biasAmount);
	}
	public static Matrix4x4 CreateOpenGLOrthoOffCenter(float left, float right, float bottom, float top, float near, float far) {
		float m11 = 2.0f / (right - left);
		float m22 = -2.0f / (top - bottom);
		float m33 = -2.0f / (far - near);

		float m41 = -(right + left) / (right - left);
		float m42 = -(top + bottom) / (top - bottom);
		float m43 = -(far + near) / (far - near);

		return new Matrix4x4(
			m11, 0, 0, 0,
			0, m22, 0, 0,
			0, 0, m33, 0,
			m41, m42, m43, 1
		);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static float RAD2DEG(float x) => x * (180f / MathF.PI);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static double RAD2DEG(double x) => x * (180 / Math.PI);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static float DEG2RAD(float x) => x * (MathF.PI / 180);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static double DEG2RAD(double x) => x * (Math.PI / 180);

	public static float CalcFovX(float fovY, float aspect)
		=> RAD2DEG(MathF.Atan(MathF.Tan(DEG2RAD(fovY) * 0.5f) * aspect)) * 2.0f;
	public static float CalcFovY(float fovX, float aspect) {
		if (fovX < 0 || fovX > 179)
			fovX = 90;

		return RAD2DEG(MathF.Atan(MathF.Tan(DEG2RAD(fovX) * 0.5f) / aspect)) * 2.0f;
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static uint RoundFloatToUnsignedLong(float f) {
		long rounded = checked((long)MathF.Round(f, MidpointRounding.ToEven));
		return (uint)rounded; 
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static unsafe int FastFloatToSmallInt(float f) {
		float shifted = f + (3 << 22);
		int* ptr = (int*)&shifted;
		return (*ptr & ((1 << 23) - 1)) - (1 << 22);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float AngleMod(float a) => (360f / 65536) * ((int)(a * (65536f / 360.0f)) & 65535);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void AngleMatrix(in QAngle angles, in Vector3 position, ref Matrix4x4 matrix) {
		AngleMatrix(in angles, ref matrix);
		MatrixSetColumn(in position, 3, ref matrix);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void MatrixSetColumn(in Vector3 position, int column, ref Matrix4x4 matrix) {
		matrix[0, column] = position.X;
		matrix[1, column] = position.Y;
		matrix[2, column] = position.Z;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void AngleMatrix(in QAngle angles, ref Matrix4x4 matrix) {
		float pitch = angles.X * MathF.PI / 180.0f;
		float yaw = angles.Y * MathF.PI / 180.0f;
		float roll = angles.Z * MathF.PI / 180.0f;

		float sp = MathF.Sin(pitch);
		float cp = MathF.Cos(pitch);
				   
		float sy = MathF.Sin(yaw);
		float cy = MathF.Cos(yaw);
				   		
		float sr = MathF.Sin(roll);
		float cr = MathF.Cos(roll);

		matrix.M11 = cp * cy;
		matrix.M12 = sp * sr * cy - cr * sy;
		matrix.M13 = sp * cr * cy + sr * sy;
		matrix.M14 = 0f;

		matrix.M21 = cp * sy;
		matrix.M22 = sp * sr * sy + cr * cy;
		matrix.M23 = sp * cr * sy - sr * cy;
		matrix.M24 = 0f;

		matrix.M31 = -sp;
		matrix.M32 = sr * cp;
		matrix.M33 = cr * cp;
		matrix.M34 = 0f;

		// Translation part
		matrix.M41 = 0f;
		matrix.M42 = 0f;
		matrix.M43 = 0f;
		matrix.M44 = 1f;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void NormalizeAngles(ref QAngle angles) {
		int i;

		for (i = 0; i < 3; i++) {
			if (angles[i] > 180.0f) 
				angles[i] -= 360.0f;
			else if (angles[i] < -180.0f) 
				angles[i] += 360.0f;
		}
	}

	public static void VectorMA(in Vector3 start, float scale, in Vector3 direction, ref Vector3 dest) {
		dest.X = start.X + direction.X * scale;
		dest.Y = start.Y + direction.Y * scale;
		dest.Z = start.Z + direction.Z * scale;
	}

	public static void Vector4DMultiply(Matrix4x4 src1, in Vector4 src2, ref Vector4 dst) {
		Vector4 v = src2;

		// If src2 and dst reference the same location, copy first
		if (Unsafe.AreSame(ref Unsafe.AsRef(in src2), ref dst)) {
			v = new Vector4(src2.X, src2.Y, src2.Z, src2.W);
		}

		dst.X = src1.M11 * v.X + src1.M12 * v.Y + src1.M13 * v.Z + src1.M14 * v.W;
		dst.Y = src1.M21 * v.X + src1.M22 * v.Y + src1.M23 * v.Z + src1.M24 * v.W;
		dst.Z = src1.M31 * v.X + src1.M32 * v.Y + src1.M33 * v.Z + src1.M34 * v.W;
		dst.W = src1.M41 * v.X + src1.M42 * v.Y + src1.M43 * v.Z + src1.M44 * v.W;
	}

	public static void ConcatTransforms(in Matrix4x4 in1, in Matrix4x4 in2, out Matrix4x4 output) {
		output = Matrix4x4.Multiply(in1, in2);
	}

	public static void QuaternionMatrix(in Quaternion quaternion, in Vector3 pos, out Matrix4x4 bonematrix) {
		QuaternionMatrix(quaternion, out bonematrix);
		bonematrix.M14 = pos.X;
		bonematrix.M24 = pos.Y;
		bonematrix.M34 = pos.Z;
		bonematrix.M44 = 1f;
	}

	public static void QuaternionMatrix(in Quaternion q, out Matrix4x4 m) {
		m = default;
		float xx = q.X * q.X;
		float yy = q.Y * q.Y;
		float zz = q.Z * q.Z;
		float xy = q.X * q.Y;
		float xz = q.X * q.Z;
		float yz = q.Y * q.Z;
		float wx = q.W * q.X;
		float wy = q.W * q.Y;
		float wz = q.W * q.Z;

		// ROW 0
		m.M11 = 1 - 2 * (yy + zz);
		m.M12 = 2 * (xy - wz);
		m.M13 = 2 * (xz + wy);

		// ROW 1
		m.M21 = 2 * (xy + wz);
		m.M22 = 1 - 2 * (xx + zz);
		m.M23 = 2 * (yz - wx);

		// ROW 2
		m.M31 = 2 * (xz - wy);
		m.M32 = 2 * (yz + wx);
		m.M33 = 1 - 2 * (xx + yy);

		// ROW 3
		m.M41 = m.M42 = m.M43 = 0;
		m.M44 = 1;
	}

	public static void SetIdentityMatrix(out Matrix4x4 matrix4x4) {
		matrix4x4 = Matrix4x4.Identity;
	}
}
