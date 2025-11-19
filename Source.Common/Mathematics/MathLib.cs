using CommunityToolkit.HighPerformance;

using Source.Common.Formats.BSP;

using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Source.Common.Mathematics;

public static class MathLibConsts
{
	public const int PITCH = 0;
	public const int YAW = 1;
	public const int ROLL = 2;

	public static readonly Vector3 vec3_origin = new(0, 0, 0);
	public static readonly QAngle vec3_angle = new(0, 0, 0);
}

public struct RadianEuler
{
	public vec_t X, Y, Z;
}

/// <summary>
/// Mostly for data structure compatibility
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 4, Size = 48)]
public record struct Matrix3x4
{
	public static readonly Matrix3x4 Identity = new(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0);
	public float M00, M01, M02, M03;
	public float M10, M11, M12, M13;
	public float M20, M21, M22, M23;
	public unsafe Span<float> this[int row] {
		get {
			if (row < 0 || row >= 3) throw new ArgumentOutOfRangeException(nameof(row));
			return new(Unsafe.AsPointer(ref Unsafe.Add(ref M00, (row * 4))), 4);
		}
	}
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

	public static implicit operator Matrix4x4(Matrix3x4 self) {
		Matrix4x4 ret = default;
		ret.Init(self);
		return ret;
	}

	public Matrix3x4(
		float m00, float m01, float m02, float m03,
		float m10, float m11, float m12, float m13,
		float m20, float m21, float m22, float m23) {
		M00 = m00; M01 = m01; M02 = m02; M03 = m03;
		M10 = m10; M11 = m11; M12 = m12; M13 = m13;
		M20 = m20; M21 = m21; M22 = m22; M23 = m23;
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

public struct CollisionNode
{
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
	public static void AngleMatrix(in QAngle angles, in Vector3 position, ref Matrix3x4 matrix) {
		AngleMatrix(in angles, ref matrix);
		MatrixSetColumn(in position, 3, ref matrix);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void MatrixSetColumn(in Vector3 inVec, int column, ref Matrix3x4 outMatrix) {
		outMatrix[0, column] = inVec.X;
		outMatrix[1, column] = inVec.Y;
		outMatrix[2, column] = inVec.Z;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void AngleMatrix(in QAngle angles, ref Matrix3x4 matrix) {
		float radiansX = angles.X * (MathF.PI / 180.0f);
		float radiansY = angles.Y * (MathF.PI / 180.0f);
		float radiansZ = angles.Z * (MathF.PI / 180.0f);

		float sp = MathF.Sin(radiansX);
		float sy = MathF.Sin(radiansY);
		float sr = MathF.Sin(radiansZ);
		float cp = MathF.Cos(radiansX);
		float cy = MathF.Cos(radiansY);
		float cr = MathF.Cos(radiansZ);

		matrix[0, 0] = cp * cy;
		matrix[1, 0] = cp * sy;
		matrix[2, 0] = -sp;

		float crcy = cr * cy;
		float crsy = cr * sy;
		float srcy = sr * cy;
		float srsy = sr * sy;
		matrix[0, 1] = sp * srcy - crsy;
		matrix[1, 1] = sp * srsy + crcy;
		matrix[2, 1] = sr * cp;

		matrix[0, 2] = (sp * crcy + srsy);
		matrix[1, 2] = (sp * crsy - srcy);
		matrix[2, 2] = cr * cp;

		matrix[0, 3] = 0.0f;
		matrix[1, 3] = 0.0f;
		matrix[2, 3] = 0.0f;
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

		dst = Vector4.Transform(src2, src1);
	}

	public static void ConcatTransforms(in Matrix3x4 in1, in Matrix3x4 in2, out Matrix3x4 result) {
		float a00 = in1.M00, a01 = in1.M01, a02 = in1.M02, a03 = in1.M03;
		float a10 = in1.M10, a11 = in1.M11, a12 = in1.M12, a13 = in1.M13;
		float a20 = in1.M20, a21 = in1.M21, a22 = in1.M22, a23 = in1.M23;

		float b00 = in2.M00, b01 = in2.M01, b02 = in2.M02, b03 = in2.M03;
		float b10 = in2.M10, b11 = in2.M11, b12 = in2.M12, b13 = in2.M13;
		float b20 = in2.M20, b21 = in2.M21, b22 = in2.M22, b23 = in2.M23;

		float out00 = a00 * b00 + a01 * b10 + a02 * b20;
		float out01 = a00 * b01 + a01 * b11 + a02 * b21;
		float out02 = a00 * b02 + a01 * b12 + a02 * b22;
		float out03 = a00 * b03 + a01 * b13 + a02 * b23 + a03;

		float out10 = a10 * b00 + a11 * b10 + a12 * b20;
		float out11 = a10 * b01 + a11 * b11 + a12 * b21;
		float out12 = a10 * b02 + a11 * b12 + a12 * b22;
		float out13 = a10 * b03 + a11 * b13 + a12 * b23 + a13;

		float out20 = a20 * b00 + a21 * b10 + a22 * b20;
		float out21 = a20 * b01 + a21 * b11 + a22 * b21;
		float out22 = a20 * b02 + a21 * b12 + a22 * b22;
		float out23 = a20 * b03 + a21 * b13 + a22 * b23 + a23;

		result = new Matrix3x4(
			out00, out01, out02, out03,
			out10, out11, out12, out13,
			out20, out21, out22, out23
		);
	}

	public static void QuaternionMatrix(in Quaternion quaternion, in Vector3 pos, out Matrix3x4 matrix) {
		QuaternionMatrix(quaternion, out matrix);

		matrix[0, 3] = pos.X;
		matrix[1, 3] = pos.Y;
		matrix[2, 3] = pos.Z;
	}

	public static void QuaternionMatrix(in Quaternion q, out Matrix3x4 m) {
		m = default;
		m[0, 0] = 1.0F - 2.0F * q.Y * q.Y - 2.0F * q.Z * q.Z;
		m[1, 0] = 2.0F * q.X * q.Y + 2.0F * q.W * q.Z;
		m[2, 0] = 2.0F * q.X * q.Z - 2.0F * q.W * q.Y;
		m[0, 1] = 2.0F * q.X * q.Y - 2.0F * q.W * q.Z;
		m[1, 1] = 1.0F - 2.0F * q.X * q.X - 2.0F * q.Z * q.Z;
		m[2, 1] = 2.0F * q.Y * q.Z + 2.0F * q.W * q.X;
		m[0, 2] = 2.0F * q.X * q.Z + 2.0F * q.W * q.Y;
		m[1, 2] = 2.0F * q.Y * q.Z - 2.0F * q.W * q.X;
		m[2, 2] = 1.0F - 2.0F * q.X * q.X - 2.0F * q.Y * q.Y;
		m[0, 3] = 0.0F;
		m[1, 3] = 0.0F;
		m[2, 3] = 0.0F;
	}

	public static void SetIdentityMatrix(out Matrix3x4 matrix) {
		matrix = default;
		matrix[0, 0] = 1.0f;
		matrix[1, 1] = 1.0f;
		matrix[2, 2] = 1.0f;
	}

	public static void MatrixInvert(in Matrix3x4 inM, out Matrix3x4 outM) {
		outM = default;
		// transpose the matrix
		outM[0, 0] = inM[0, 0];
		outM[0, 1] = inM[1, 0];
		outM[0, 2] = inM[2, 0];
		outM[1, 0] = inM[0, 1];
		outM[1, 1] = inM[1, 1];
		outM[1, 2] = inM[2, 1];
		outM[2, 0] = inM[0, 2];
		outM[2, 1] = inM[1, 2];
		outM[2, 2] = inM[2, 2];

		// now fix up the translation to be in the other space
		Span<float> tmp = stackalloc float[3];
		tmp[0] = inM[0, 3];
		tmp[1] = inM[1, 3];
		tmp[2] = inM[2, 3];

		outM[0, 3] = -DotProduct(tmp, outM[0]);
		outM[1, 3] = -DotProduct(tmp, outM[1]);
		outM[2, 3] = -DotProduct(tmp, outM[2]);
	}

	public static vec_t DotProduct(Span<vec_t> v1, Span<vec_t> v2) {
		return v1[0] * v2[0] + v1[1] * v2[1] + v1[2] * v2[2];
	}

	public static void MatrixBuildPerspectiveX(ref Matrix4x4 dst, float fovX, float aspectRatio, float zNear, float zFar) {
		float flWidthScale = 1.0f / MathF.Tan(fovX * MathF.PI / 360.0f);
		float flHeightScale = aspectRatio * flWidthScale;
		dst.Init(flWidthScale, 0.0f, 0.0f, 0.0f,
					0.0f, flHeightScale, 0.0f, 0.0f,
					0.0f, 0.0f, 0.0f, 0.0f,
					0.0f, 0.0f, -1.0f, 0.0f);

		MatrixBuildPerspectiveZRange(ref dst, zNear, zFar);
	}

	private static void MatrixBuildPerspectiveZRange(ref Matrix4x4 dst, float znear, float zfar) {
		dst[2, 0] = 0.0f;
		dst[2, 1] = 0.0f;
		dst[2, 2] = zfar / (znear - zfar);
		dst[2, 3] = znear * zfar / (znear - zfar);
	}

	public static void Init(this ref Matrix4x4 m, in Matrix3x4 m3x4) {
		new ReadOnlySpan<Matrix3x4>(in m3x4).Cast<Matrix3x4, float>().CopyTo(new Span<Matrix4x4>(ref m).Cast<Matrix4x4, float>());

		m[3, 0] = 0.0f;
		m[3, 1] = 0.0f;
		m[3, 2] = 0.0f;
		m[3, 3] = 1.0f;
	}
	public static void Init(this ref Matrix4x4 m, vec_t m00, vec_t m01, vec_t m02, vec_t m03,
	vec_t m10, vec_t m11, vec_t m12, vec_t m13,
	vec_t m20, vec_t m21, vec_t m22, vec_t m23,
	vec_t m30, vec_t m31, vec_t m32, vec_t m33) {
		m[0, 0] = m00;
		m[0, 1] = m01;
		m[0, 2] = m02;
		m[0, 3] = m03;

		m[1, 0] = m10;
		m[1, 1] = m11;
		m[1, 2] = m12;
		m[1, 3] = m13;

		m[2, 0] = m20;
		m[2, 1] = m21;
		m[2, 2] = m22;
		m[2, 3] = m23;

		m[3, 0] = m30;
		m[3, 1] = m31;
		m[3, 2] = m32;
		m[3, 3] = m33;
	}

	public static void MatrixOrtho(ref Matrix4x4 dst, float left, float right, float bottom, float top, float zNear, float zFar) {
		Matrix4x4 mat = default;
		MatrixBuildOrtho(ref mat, left, top, right, bottom, zNear, zFar);

		MatrixMultiply(in dst, in mat, out Matrix4x4 temp);
		dst = temp;
	}

	// TODO: SIMD this
	public static void MatrixMultiply(in Matrix4x4 src1, in Matrix4x4 src2, out Matrix4x4 dst) {
		float a00 = src1.M11, a01 = src1.M12, a02 = src1.M13, a03 = src1.M14;
		float a10 = src1.M21, a11 = src1.M22, a12 = src1.M23, a13 = src1.M24;
		float a20 = src1.M31, a21 = src1.M32, a22 = src1.M33, a23 = src1.M34;
		float a30 = src1.M41, a31 = src1.M42, a32 = src1.M43, a33 = src1.M44;

		float b00 = src2.M11, b01 = src2.M12, b02 = src2.M13, b03 = src2.M14;
		float b10 = src2.M21, b11 = src2.M22, b12 = src2.M23, b13 = src2.M24;
		float b20 = src2.M31, b21 = src2.M32, b22 = src2.M33, b23 = src2.M34;
		float b30 = src2.M41, b31 = src2.M42, b32 = src2.M43, b33 = src2.M44;

		float out00 = a00 * b00 + a01 * b10 + a02 * b20 + a03 * b30;
		float out01 = a00 * b01 + a01 * b11 + a02 * b21 + a03 * b31;
		float out02 = a00 * b02 + a01 * b12 + a02 * b22 + a03 * b32;
		float out03 = a00 * b03 + a01 * b13 + a02 * b23 + a03 * b33;

		float out10 = a10 * b00 + a11 * b10 + a12 * b20 + a13 * b30;
		float out11 = a10 * b01 + a11 * b11 + a12 * b21 + a13 * b31;
		float out12 = a10 * b02 + a11 * b12 + a12 * b22 + a13 * b32;
		float out13 = a10 * b03 + a11 * b13 + a12 * b23 + a13 * b33;

		float out20 = a20 * b00 + a21 * b10 + a22 * b20 + a23 * b30;
		float out21 = a20 * b01 + a21 * b11 + a22 * b21 + a23 * b31;
		float out22 = a20 * b02 + a21 * b12 + a22 * b22 + a23 * b32;
		float out23 = a20 * b03 + a21 * b13 + a22 * b23 + a23 * b33;

		float out30 = a30 * b00 + a31 * b10 + a32 * b20 + a33 * b30;
		float out31 = a30 * b01 + a31 * b11 + a32 * b21 + a33 * b31;
		float out32 = a30 * b02 + a31 * b12 + a32 * b22 + a33 * b32;
		float out33 = a30 * b03 + a31 * b13 + a32 * b23 + a33 * b33;

		dst = default;
		dst.Init(
			out00, out01, out02, out03,
			out10, out11, out12, out13,
			out20, out21, out22, out23,
			out30, out31, out32, out33
		);
	}

	private static void MatrixBuildOrtho(ref Matrix4x4 dst, float left, float top, float right, float bottom, float zNear, float zFar) {
		dst.Init(2.0f / (right - left), 0.0f, 0.0f, (left + right) / (left - right),
				0.0f, 2.0f / (bottom - top), 0.0f, (bottom + top) / (top - bottom),
				0.0f, 0.0f, 1.0f / (zNear - zFar), zNear / (zNear - zFar),
				0.0f, 0.0f, 0.0f, 1.0f);
	}

	public static void MatrixBuildScale(out Matrix4x4 dst, float x, float y, float z) {
		dst = default;
		dst[0, 0] = x; dst[0, 1] = 0.0f; dst[0, 2] = 0.0f; dst[0, 3] = 0.0f;
		dst[1, 0] = 0.0f; dst[1, 1] = y; dst[1, 2] = 0.0f; dst[1, 3] = 0.0f;
		dst[2, 0] = 0.0f; dst[2, 1] = 0.0f; dst[2, 2] = z; dst[2, 3] = 0.0f;
		dst[3, 0] = 0.0f; dst[3, 1] = 0.0f; dst[3, 2] = 0.0f; dst[3, 3] = 1.0f;
	}

	public static void MatrixAngles(in Matrix3x4 matrix, out QAngle angles) {
		angles = default;
		Span<float> forward = stackalloc float[3];
		Span<float> left = stackalloc float[3];
		Span<float> up = stackalloc float[3];

		//
		// Extract the basis vectors from the matrix. Since we only need the Z
		// component of the up vector, we don't get X and Y.
		//
		forward[0] = matrix[0][0];
		forward[1] = matrix[1][0];
		forward[2] = matrix[2][0];
		left[0] = matrix[0][1];
		left[1] = matrix[1][1];
		left[2] = matrix[2][1];
		up[2] = matrix[2][2];

		float xyDist = MathF.Sqrt(forward[0] * forward[0] + forward[1] * forward[1]);

		// enough here to get angles?
		if (xyDist > 0.001f) {
			// (yaw)	y = ATAN( forward.y, forward.x );		-- in our space, forward is the X axis
			angles[1] = RAD2DEG(MathF.Atan2(forward[1], forward[0]));

			// (pitch)	x = ATAN( -forward.z, sqrt(forward.x*forward.x+forward.y*forward.y) );
			angles[0] = RAD2DEG(MathF.Atan2(-forward[2], xyDist));

			// (roll)	z = ATAN( left.z, up.z );
			angles[2] = RAD2DEG(MathF.Atan2(left[2], up[2]));
		}
		else    // forward is mostly Z, gimbal lock-
		{
			// (yaw)	y = ATAN( -left.x, left.y );			-- forward is mostly z, so use right for yaw
			angles[1] = RAD2DEG(MathF.Atan2(-left[0], left[1]));

			// (pitch)	x = ATAN( -forward.z, sqrt(forward.x*forward.x+forward.y*forward.y) );
			angles[0] = RAD2DEG(MathF.Atan2(-forward[2], xyDist));

			// Assume no roll in this case as one degree of freedom has been lost (i.e. yaw == roll)
			angles[2] = 0;
		}
	}
}
