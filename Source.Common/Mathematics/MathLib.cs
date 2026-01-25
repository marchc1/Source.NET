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

[StructLayout(LayoutKind.Sequential, Pack = 2, Size = 6)]
public struct Quaternion48
{
	public static implicit operator Quaternion(Quaternion48 self) {
		Quaternion tmp;
		tmp.X = ((int)self.x - 32768) * (1 / 32768.0f);
		tmp.Y = ((int)self.y - 32768) * (1 / 32768.0f);
		tmp.Z = ((int)self.z - 16384) * (1 / 16384.0f);
		tmp.W = MathF.Sqrt(1 - tmp.X * tmp.X - tmp.Y * tmp.Y - tmp.Z * tmp.Z);
		if (self.wneg)
			tmp.W = -tmp.W;
		return tmp;
	}

	public static implicit operator Quaternion48(Quaternion vOther) {
		Quaternion48 result;
		result.data = 0;
		result.x = (ushort)Math.Clamp((int)(vOther.X * 32768) + 32768, 0, 65535);
		result.y = (ushort)Math.Clamp((int)(vOther.Y * 32768) + 32768, 0, 65535);
		result.z = (ushort)Math.Clamp((int)(vOther.Z * 16384) + 16384, 0, 32767);
		result.wneg = vOther.W < 0;
		return result;
	}

	public ulong data;

	private const ulong X_MASK = 0xFFFF;           // 16 bits
	private const ulong Y_MASK = 0xFFFF;           // 16 bits
	private const ulong Z_MASK = 0x7FFF;           // 15 bits
	private const ulong WNEG_MASK = 0x1;           // 1 bit

	private const int X_SHIFT = 0;
	private const int Y_SHIFT = 16;
	private const int Z_SHIFT = 32;
	private const int WNEG_SHIFT = 47;

	public ushort x {
		readonly get => (ushort)((data >> X_SHIFT) & X_MASK);
		set => data = (data & ~(X_MASK << X_SHIFT)) | ((ulong)(value & X_MASK) << X_SHIFT);
	}

	public ushort y {
		readonly get => (ushort)((data >> Y_SHIFT) & Y_MASK);
		set => data = (data & ~(Y_MASK << Y_SHIFT)) | ((ulong)(value & Y_MASK) << Y_SHIFT);
	}

	public ushort z {
		readonly get => (ushort)((data >> Z_SHIFT) & Z_MASK);
		set => data = (data & ~(Z_MASK << Z_SHIFT)) | ((ulong)(value & Z_MASK) << Z_SHIFT);
	}

	public bool wneg {
		readonly get => ((data >> WNEG_SHIFT) & WNEG_MASK) != 0;
		set {
			if (value)
				data |= (WNEG_MASK << WNEG_SHIFT);
			else
				data &= ~(WNEG_MASK << WNEG_SHIFT);
		}
	}
}

[StructLayout(LayoutKind.Sequential, Pack = 2, Size = 6)]
public struct Vector48
{
	public Vector48(vec_t x, vec_t y, vec_t z) {
		X = (Half)x;
		Y = (Half)y;
		Z = (Half)z;
	}

	public static implicit operator Vector3(Vector48 self) => new((vec_t)self.X, (vec_t)self.Y, (vec_t)self.Z);

	public readonly float this[int i] => (float)(i switch {
		0 => X,
		1 => Y,
		2 => Z,
		_ => throw new IndexOutOfRangeException()
	});

	public Half X;
	public Half Y;
	public Half Z;
}



[StructLayout(LayoutKind.Sequential, Pack = 8, Size = 8)]
public struct Quaternion64
{
	public static implicit operator Quaternion(Quaternion64 self) {
		Quaternion tmp;
		// shift to -1048576, + 1048575, then round down slightly to -1.0 < x < 1.0
		tmp.X = ((int)self.x - 1048576) * (1 / 1048576.5f);
		tmp.Y = ((int)self.y - 1048576) * (1 / 1048576.5f);
		tmp.Z = ((int)self.z - 1048576) * (1 / 1048576.5f);
		tmp.W = MathF.Sqrt(1 - tmp.X * tmp.X - tmp.Y * tmp.Y - tmp.Z * tmp.Z);
		if (self.wneg)
			tmp.W = -tmp.W;
		return tmp;
	}

	public static implicit operator Quaternion64(Quaternion vOther) {
		Quaternion64 result;
		result.data = 0;
		result.x = (uint)Math.Clamp((int)(vOther.X * 1048576) + 1048576, 0, 2097151);
		result.y = (uint)Math.Clamp((int)(vOther.Y * 1048576) + 1048576, 0, 2097151);
		result.z = (uint)Math.Clamp((int)(vOther.Z * 1048576) + 1048576, 0, 2097151);
		result.wneg = vOther.W < 0;
		return result;
	}

	public ulong data;

	private const ulong X_MASK = 0x1FFFFF;
	private const ulong Y_MASK = 0x1FFFFF;
	private const ulong Z_MASK = 0x1FFFFF;
	private const ulong WNEG_MASK = 0x1;

	private const int X_SHIFT = 0;
	private const int Y_SHIFT = 21;
	private const int Z_SHIFT = 42;
	private const int WNEG_SHIFT = 63;

	public uint x {
		readonly get => (uint)((data >> X_SHIFT) & X_MASK);
		set => data = (data & ~(X_MASK << X_SHIFT)) | ((value & X_MASK) << X_SHIFT);
	}

	public uint y {
		readonly get => (uint)((data >> Y_SHIFT) & Y_MASK);
		set => data = (data & ~(Y_MASK << Y_SHIFT)) | ((value & Y_MASK) << Y_SHIFT);
	}

	public uint z {
		readonly get => (uint)((data >> Z_SHIFT) & Z_MASK);
		set => data = (data & ~(Z_MASK << Z_SHIFT)) | ((value & Z_MASK) << Z_SHIFT);
	}

	public bool wneg {
		readonly get => ((data >> WNEG_SHIFT) & WNEG_MASK) != 0;
		set {
			if (value)
				data |= (WNEG_MASK << WNEG_SHIFT);
			else
				data &= ~(WNEG_MASK << WNEG_SHIFT);
		}
	}
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
	static MathLib() {

	}

	// These can just be Systems.Numerics calls since I think System.Numerics SIMD's them. These are just to have consistent naming with Source
	// (and maybe allow us to deviate easily if need be at some point).

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static void VectorAdd(in Vector3 a, in Vector3 b, out Vector3 result) => result = Vector3.Add(a, b);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static void VectorAdd(in Vector3 a, vec_t b, out Vector3 result) => result = Vector3.Add(a, new(b));
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static void VectorSubtract(in Vector3 a, in Vector3 b, out Vector3 result) => result = Vector3.Subtract(a, b);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static void VectorSubtract(in Vector3 a, vec_t b, out Vector3 result) => result = Vector3.Subtract(a, new(b));
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static void VectorMultiply(in Vector3 inVec, in Vector3 scale, out Vector3 result) => result = Vector3.Multiply(inVec, scale);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static void VectorMultiply(in Vector3 inVec, vec_t scale, out Vector3 result) => result = Vector3.Multiply(inVec, scale);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static void VectorDivide(in Vector3 inVec, in Vector3 scale, out Vector3 result) => result = Vector3.Divide(inVec, scale);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static void VectorDivide(in Vector3 inVec, vec_t scale, out Vector3 result) => result = Vector3.Divide(inVec, scale);


	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static int RoundFloatToInt(float f) => (int)f;
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static byte RoundFloatToByte(float f) => (byte)RoundFloatToInt(f);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void ColorClamp(ref Vector3 color) {
		float maxc = MathF.Max(color.X, MathF.Max(color.Y, color.Z));
		if (maxc > 1.0f) {
			float ooMax = 1.0f / maxc;
			color.X *= ooMax;
			color.Y *= ooMax;
			color.Z *= ooMax;
		}

		if (color[0] < 0f) color[0] = 0f;
		if (color[1] < 0f) color[1] = 0f;
		if (color[2] < 0f) color[2] = 0f;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void VectorScale(in Vector3 inVec, in Vector3 scale, out Vector3 result) => result = Vector3.Multiply(inVec, scale);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void VectorScale(in Vector3 inVec, vec_t scale, out Vector3 result) => result = Vector3.Multiply(inVec, scale);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void VectorScale(in Vector3 inVec, in Vector3 scale, Span<float> result) {
		Vector3 v = Vector3.Multiply(inVec, scale);
		ReadOnlySpan<Vector3> vS = new(in v);
		vS.Cast<Vector3, float>().CopyTo(result);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void VectorScale(in Vector3 inVec, vec_t scale, Span<float> result) {
		Vector3 v = Vector3.Multiply(inVec, scale);
		ReadOnlySpan<Vector3> vS = new(in v);
		vS.Cast<Vector3, float>().CopyTo(result);
	}


	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void VectorScale(ReadOnlySpan<float> inVec, in Vector3 scale, out Vector3 result) => result = Vector3.Multiply(inVec.Cast<float, Vector3>()[0], scale);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void VectorScale(ReadOnlySpan<float> inVec, vec_t scale, out Vector3 result) => result = Vector3.Multiply(inVec.Cast<float, Vector3>()[0], scale);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void VectorScale(ReadOnlySpan<float> inVec, in Vector3 scale, Span<float> result) {
		Vector3 v = Vector3.Multiply(inVec.Cast<float, Vector3>()[0], scale);
		ReadOnlySpan<Vector3> vS = new(in v);
		vS.Cast<Vector3, float>().CopyTo(result);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void VectorScale(ReadOnlySpan<float> inVec, vec_t scale, Span<float> result) {
		Vector3 v = Vector3.Multiply(inVec.Cast<float, Vector3>()[0], scale);
		ReadOnlySpan<Vector3> vS = new(in v);
		vS.Cast<Vector3, float>().CopyTo(result);
	}



	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ref Vector3 AsVector3D(this ref Vector4 vec)
		=> ref new Span<Vector4>(ref vec).Cast<Vector4, float>()[..3].Cast<float, Vector3>()[0];

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ref Vector2 AsVector2D(this ref Vector4 vec)
		=> ref new Span<Vector4>(ref vec).Cast<Vector4, float>()[..2].Cast<float, Vector2>()[0];

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static vec_t DistTo(this in Vector3 vec, in Vector3 other) => Vector3.Distance(vec, other);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Vector3 Min(this in Vector3 vec, in Vector3 other) => Vector3.Min(vec, other);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Vector3 Max(this in Vector3 vec, in Vector3 other) => Vector3.Max(vec, other);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static vec_t DistToSqr(this in Vector3 vec, in Vector3 other) => Vector3.DistanceSquared(vec, other);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static vec_t Length2D(this in Vector3 vec) => MathF.Sqrt(vec.X * vec.X + vec.Y * vec.Y);


	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float Lerp(float percent, float start, float end) {
		return start + (end - start) * percent;
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float Lerp(float f1, float f2, float i1, float i2, float x) {
		return f1 + (f2 - f1) * (x - i1) / (i2 - i1);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float Fmodf(float x, float y) {
		return x - y * (float)MathF.Truncate(x / y);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static double Fmodf(double x, double y) {
		return x - y * Math.Truncate(x / y);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int Modulo(int a, int b) {
		return (Math.Abs(a * b) + a) % b;
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float SimpleSpline(float value) {
		return (value * value) * (3 - 2 * value);
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
	public static void AngleMatrix(in QAngle angles, in Vector3 position, out Matrix3x4 matrix) {
		AngleMatrix(in angles, out matrix);
		MatrixSetColumn(in position, 3, ref matrix);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void MatrixSetColumn(in Vector3 inVec, int column, ref Matrix3x4 outMatrix) {
		outMatrix[0, column] = inVec.X;
		outMatrix[1, column] = inVec.Y;
		outMatrix[2, column] = inVec.Z;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ref float SubFloat(ref Vector3 a, int idx) {
		ArgumentOutOfRangeException.ThrowIfNegative(idx);
		ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(idx, 3);

		return ref new Span<Vector3>(ref a).Cast<Vector3, float>()[idx];
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ref float SubFloat(ref Vector4 a, int idx) {
		ArgumentOutOfRangeException.ThrowIfNegative(idx);
		ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(idx, 4);

		return ref new Span<Vector4>(ref a).Cast<Vector4, float>()[idx];
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static unsafe void AngleQuaternion(in RadianEuler angles, out Quaternion outQuat) {
		fixed (RadianEuler* pQ = &angles) {
			Vector4 radians = new(*(Vector3*)pQ, 0);
			radians = Vector4.Multiply(radians, 0.5f);
			(Vector4 sine, Vector4 cosine) = Vector4.SinCos(radians);

			float sr = SubFloat(ref sine, 0), sp = SubFloat(ref sine, 1), sy = SubFloat(ref sine, 2);
			float cr = SubFloat(ref cosine, 0), cp = SubFloat(ref cosine, 1), cy = SubFloat(ref cosine, 2);

			float srXcp = sr * cp, crXsp = cr * sp;
			outQuat.X = srXcp * cy - crXsp * sy;
			outQuat.Y = crXsp * cy + srXcp * sy;

			float crXcp = cr * cp, srXsp = sr * sp;
			outQuat.Z = crXcp * sy - srXsp * cy;
			outQuat.W = crXcp * cy + srXsp * sy;
		}
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static unsafe void AngleQuaternion(in QAngle angles, out Quaternion outQuat) {
		fixed (QAngle* pQ = &angles) {
			Vector4 radians = new(*(Vector3*)pQ, 0.5f);
			(Vector4 sine, Vector4 cosine) = Vector4.SinCos(radians);

			float sr = SubFloat(ref sine, 0), sp = SubFloat(ref sine, 1), sy = SubFloat(ref sine, 2);
			float cr = SubFloat(ref cosine, 0), cp = SubFloat(ref cosine, 1), cy = SubFloat(ref cosine, 2);

			float srXcp = sr * cp, crXsp = cr * sp;
			outQuat.X = srXcp * cy - crXsp * sy;
			outQuat.Y = crXsp * cy + srXcp * sy;

			float crXcp = cr * cp, srXsp = sr * sp;
			outQuat.Z = crXcp * sy - srXsp * cy;
			outQuat.W = crXcp * cy + srXsp * sy;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void AngleMatrix(in QAngle angles, out Matrix3x4 matrix) {
		matrix = default;

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

	public static void QuaternionMult(in Quaternion p, in Quaternion q, out Quaternion qt) {
		qt = Quaternion.Multiply(p, q);
	}
	public static void QuaternionScale(in Quaternion p, float t, out Quaternion q) {
		float r;
		float sinom = MathF.Sqrt(DotProduct(in p, in p));
		sinom = Math.Min(sinom, 1.0f);

		float sinsom = MathF.Sin(MathF.Asin(sinom) * t);

		t = sinsom / (sinom + float.Epsilon);
		q = default;
		VectorScale(in p.AsVector3ReadOnlyRef(), t, out q.AsVector3Ref());

		// rescale rotation
		r = 1.0f - sinsom * sinsom;

		// Assert( r >= 0 );
		if (r < 0.0f)
			r = 0.0f;
		r = MathF.Sqrt(r);

		// keep sign of rotation
		if (p.W < 0)
			q.W = -r;
		else
			q.W = r;

		Assert(q.IsValid());
	}

	public static void QuaternionSM(float s, in Quaternion p, in Quaternion q, ref Quaternion qt) {
		Quaternion p1, q1;
		QuaternionScale(p, s, out p1);
		QuaternionMult(p1, q, out q1);
		QuaternionNormalize2(ref q1);

		qt = q1;
	}
	public static void QuaternionMA(in Quaternion p, float s, in Quaternion q, ref Quaternion qt) {
		// TODO: simd
		QuaternionScale(q, s, out Quaternion q1);
		QuaternionMult(p, q1, out Quaternion p1);
		QuaternionNormalize2(ref p1);
		qt[0] = p1[0];
		qt[1] = p1[1];
		qt[2] = p1[2];
		qt[3] = p1[3];
	}

	public static void VectorMA(in Vector3 start, float scale, in Vector3 direction, out Vector3 dest) {
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

	public static void QuaternionSlerp(in Quaternion p, in Quaternion q, float t, out Quaternion qt) {
		qt = Quaternion.Slerp(p, q, t);
	}

	public static void QuaternionSlerpNoAlign(in Quaternion p, in Quaternion q, float t, out Quaternion qt) {
		qt = Quaternion.Slerp(p, q, t);
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

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static vec_t DotProduct(ReadOnlySpan<vec_t> v1, ReadOnlySpan<vec_t> v2) {
		return v1[0] * v2[0] + v1[1] * v2[1] + v1[2] * v2[2];
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static unsafe vec_t DotProduct(in Quaternion v1, in Quaternion v2) {
		fixed (Quaternion* pV1 = &v1)
		fixed (Quaternion* pV2 = &v2) {
			return DotProduct(new(pV1, 4), new(pV2, 4));
		}
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static vec_t DotProduct(in Vector3 v1, in Vector3 v2) => v1.X * v2.X + v1.Y * v2.Y + v1.Z * v2.Z;


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

	public static bool IsZero(this in Vector3 v, float tolerance = 0.01f) {
		Vector3 zero = Vector3.Zero;
		Vector3 diff = Vector3.Abs(v - zero);
		Vector3 toleranceVec = new(tolerance);

		return diff.X <= toleranceVec.X &&
				 diff.Y <= toleranceVec.Y &&
				 diff.Z <= toleranceVec.Z;
	}

	public static void Init(this ref Vector2 v) => v.X = v.Y = 0;
	public static void Init(this ref Vector3 v) => v.X = v.Y = v.Z = 0;
	public static void Init(this ref QAngle a) => a.X = a.Y = a.Z = 0;
	public static void Init(this ref RadianEuler r) => r.X = r.Y = r.Z = 0;
	public static void Init(this ref Vector4 v) => v.X = v.Y = v.Z = v.W = 0;
	public static void Init(this ref Quaternion v) => v.X = v.Y = v.Z = v.W = 0;
	public static vec_t Dot(this in Vector3 a, in Vector3 b) => Vector3.Dot(a, b);


	public static void Init(this ref Vector3 m, float x, float y, float z) {
		m.X = x;
		m.Y = y;
		m.Z = z;
	}

	public static void Init(this ref Vector3 m, ReadOnlySpan<float> f) {
		m.X = f[0];
		m.Y = f[1];
		m.Z = f[2];
	}

	public static void Init(this ref Quaternion m, float x, float y, float z, float w) {
		m.X = x;
		m.Y = y;
		m.Z = z;
		m.W = w;
	}

	public static ref Vector3 AsVector3Ref(this ref Quaternion q) => ref new Span<Quaternion>(ref q).Cast<Quaternion, float>()[..3].Cast<float, Vector3>()[0];
	public static ref Vector4 AsVector4Ref(this ref Quaternion q) => ref new Span<Quaternion>(ref q).Cast<Quaternion, Vector4>()[0];
	public static ref readonly Vector3 AsVector3ReadOnlyRef(this ref readonly Quaternion q) => ref new ReadOnlySpan<Quaternion>(in q).Cast<Quaternion, float>()[..3].Cast<float, Vector3>()[0];
	public static ref readonly Vector4 AsVector4ReadOnlyRef(this ref readonly Quaternion q) => ref new ReadOnlySpan<Quaternion>(in q).Cast<Quaternion, Vector4>()[0];

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool IsValid(this ref Vector2 v) => !Vector2.AnyWhereAllBitsSet(Vector2.IsNaN(v));
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool IsValid(this ref Vector3 v) => !Vector3.AnyWhereAllBitsSet(Vector3.IsNaN(v));
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool IsValid(this ref Vector4 v) => !Vector4.AnyWhereAllBitsSet(Vector4.IsNaN(v));

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static unsafe bool IsValid(this ref Quaternion q) {
		fixed (Quaternion* pQ = &q)
			return !Vector4.AnyWhereAllBitsSet(Vector4.IsNaN(*(Vector4*)pQ));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static unsafe bool IsValid(this ref QAngle a) {
		fixed (QAngle* pA = &a)
			return !Vector3.AnyWhereAllBitsSet(Vector3.IsNaN(*(Vector3*)pA));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static unsafe bool IsValid(this ref RadianEuler R) {
		fixed (RadianEuler* pR = &R)
			return !Vector3.AnyWhereAllBitsSet(Vector3.IsNaN(*(Vector3*)pR));
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

	public static void QuaternionBlend(in Quaternion p, in Quaternion q, float t, out Quaternion qt) {
		float dot = p.X * q.X + p.Y * q.Y + p.Z * q.Z + p.W * q.W;

		float sign = dot < 0.0f ? -1.0f : 1.0f;

		float oneMinusT = 1.0f - t;
		qt.X = p.X * oneMinusT + (q.X * sign) * t;
		qt.Y = p.Y * oneMinusT + (q.Y * sign) * t;
		qt.Z = p.Z * oneMinusT + (q.Z * sign) * t;
		qt.W = p.W * oneMinusT + (q.W * sign) * t;

		float length = MathF.Sqrt(qt.X * qt.X + qt.Y * qt.Y + qt.Z * qt.Z + qt.W * qt.W);
		if (length > 0.0f) {
			float invLength = 1.0f / length;
			qt.X *= invLength;
			qt.Y *= invLength;
			qt.Z *= invLength;
			qt.W *= invLength;
		}
	}

	public static void QuaternionBlendNoAlign(in Quaternion p, in Quaternion q, float t, out Quaternion qt) {
		float oneMinusT = 1.0f - t;
		qt.X = p.X * oneMinusT + q.X * t;
		qt.Y = p.Y * oneMinusT + q.Y * t;
		qt.Z = p.Z * oneMinusT + q.Z * t;
		qt.W = p.W * oneMinusT + q.W * t;

		float length = MathF.Sqrt(qt.X * qt.X + qt.Y * qt.Y + qt.Z * qt.Z + qt.W * qt.W);
		if (length > 0.0f) {
			float invLength = 1.0f / length;
			qt.X *= invLength;
			qt.Y *= invLength;
			qt.Z *= invLength;
			qt.W *= invLength;
		}
	}

	public static void QuaternionIdentityBlend(in Quaternion p, float t, out Quaternion qt) {
		float sclp = 1.0f - t;
		qt.X = p.X * sclp;
		qt.Y = p.Y * sclp;
		qt.Z = p.Z * sclp;
		qt.W = p.W * sclp;

		if (qt.W < 0.0f)
			t = -t;

		qt.W += t;

		float length = MathF.Sqrt(qt.X * qt.X + qt.Y * qt.Y + qt.Z * qt.Z + qt.W * qt.W);
		if (length > 0.0f) {
			float invLength = 1.0f / length;
			qt.X *= invLength;
			qt.Y *= invLength;
			qt.Z *= invLength;
			qt.W *= invLength;
		}
	}
	public static void Hermite_Spline(in Vector3 p1, in Vector3 p2, in Vector3 d1, in Vector3 d2, float t, out Vector3 output) {
		float tSqr = t * t;
		float tCube = t * tSqr;

		float b1 = 2.0f * tCube - 3.0f * tSqr + 1.0f;
		float b2 = 1.0f - b1; // -2*tCube+3*tSqr;
		float b3 = tCube - 2 * tSqr + t;
		float b4 = tCube - tSqr;

		VectorScale(p1, b1, out output);
		VectorMA(output, b2, p2, out output);
		VectorMA(output, b3, d1, out output);
		VectorMA(output, b4, d2, out output);
	}
	public static void Hermite_Spline(in Vector3 p0, in Vector3 p1, in Vector3 p2, float t, out Vector3 output) {
		Vector3 e10 = p1 - p0, e21 = p2 - p1;
		Hermite_Spline(p1, p2, e10, e21, t, out output);
	}

	public static void QuaternionAlign(in Quaternion p, in Quaternion q, out Quaternion qt) {
		qt = default;
		int i;
		// decide if one of the quaternions is backwards
		float a = 0;
		float b = 0;
		for (i = 0; i < 4; i++) {
			a += (p[i] - q[i]) * (p[i] - q[i]);
			b += (p[i] + q[i]) * (p[i] + q[i]);
		}
		if (a > b) {
			for (i = 0; i < 4; i++) {
				qt[i] = -q[i];
			}
		}
		else if (!Unsafe.AreSame(in q, ref qt)) {
			for (i = 0; i < 4; i++) {
				qt[i] = q[i];
			}
		}
	}

	public static float Hermite_Spline(float p1, float p2, float d1, float d2, float t) {
		float output;
		float tSqr = t * t;
		float tCube = t * tSqr;

		float b1 = 2.0f * tCube - 3.0f * tSqr + 1.0f;
		float b2 = 1.0f - b1; // -2*tCube+3*tSqr;
		float b3 = tCube - 2 * tSqr + t;
		float b4 = tCube - tSqr;

		output = p1 * b1;
		output += p2 * b2;
		output += d1 * b3;
		output += d2 * b4;

		return output;
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float Hermite_Spline(float p0, float p1, float p2, float t) {
		return Hermite_Spline(p1, p2, p1 - p0, p2 - p1, t);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void QuaternionNormalize2(ref Quaternion q) {
		q = Quaternion.Normalize(q);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static void Hermite_Spline(Quaternion q0, Quaternion q1, Quaternion q2, float t, out Quaternion output) {
		QuaternionAlign(q2, q0, out Quaternion q0a);
		QuaternionAlign(q2, q1, out Quaternion q1a);

		output.X = Hermite_Spline(q0a.X, q1a.X, q2.X, t);
		output.Y = Hermite_Spline(q0a.Y, q1a.Y, q2.Y, t);
		output.Z = Hermite_Spline(q0a.Z, q1a.Z, q2.Z, t);
		output.W = Hermite_Spline(q0a.W, q1a.W, q2.W, t);

		QuaternionNormalize2(ref output);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static double RemapVal(double val, double A, double B, double C, double D) {
		if (A == B)
			return val >= B ? D : C;
		return C + (D - C) * (val - A) / (B - A);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static double RemapValClamped(double val, double A, double B, double C, double D) {
		if (A == B)
			return val >= B ? D : C;

		double cVal = (val - A) / (B - A);
		cVal = Math.Clamp(cVal, 0.0, 1.0);

		return C + (D - C) * cVal;
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static double SimpleSplineRemapVal(double val, double A, double B, double C, double D) {
		if (A == B)
			return val >= B ? D : C;
		double cVal = (val - A) / (B - A);
		return C + (D - C) * SimpleSpline(cVal);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static unsafe void AngleVectors(in QAngle angles, out Vector3 forward, out Vector3 right, out Vector3 up) {
		fixed (QAngle* aptr = &angles) {
			Vector3 radians = Vector3.Multiply(*(Vector3*)aptr, MathF.PI / 180f);
			(Vector3 sine, Vector3 cosine) = Vector3.SinCos(radians);

			float sp = SubFloat(ref sine, 0), sy = SubFloat(ref sine, 1), sr = SubFloat(ref sine, 2);
			float cp = SubFloat(ref cosine, 0), cy = SubFloat(ref cosine, 1), cr = SubFloat(ref cosine, 2);

			forward = new(cp * cy, cp * sy, -sp);
			right = new(-1 * sr * sp * cy + -1 * cr * -sy, -1 * sr * sp * sy + -1 * cr * cy, -1 * sr * cp);
			up = new(cr * sp * cy + -sr * -sy, cr * sp * sy + -sr * cy, cr * cp);
		}
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static void VectorCopy(in Vector3 inV, out Vector3 outV) => outV = inV;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float VectorNormalize(ref Vector3 fwd) {
		float len = fwd.Length();
		fwd = Vector3.Normalize(fwd);
		return len;
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void CrossProduct(in Vector3 a, in Vector3 b, out Vector3 result) {
		result = Vector3.Cross(a, b);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void MatrixGetColumn(in Matrix3x4 inMatrix, int column, out Vector3 outVec) {
		outVec.X = inMatrix[0][column];
		outVec.Y = inMatrix[1][column];
		outVec.Z = inMatrix[2][column];
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void MatrixPosition(in Matrix3x4 matrix, out Vector3 origin) {
		MatrixGetColumn(matrix, 3, out origin);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void VectorTransform(in Vector3 in1, in Matrix3x4 in2, out Vector3 vecOut) {

		vecOut = VectorTransform(in1, in2);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static Vector3 VectorTransform(in Vector3 in1, in Matrix3x4 in2) {
		Vector3 translation = new(in2.M03, in2.M13, in2.M23);

		Vector3 dotResult = new(
			in1.X * in2.M00 + in1.Y * in2.M01 + in1.Z * in2.M02,
			in1.X * in2.M10 + in1.Y * in2.M11 + in1.Z * in2.M12,
			in1.X * in2.M20 + in1.Y * in2.M21 + in1.Z * in2.M22
		);

		return Vector3.Add(dotResult, translation);
	}

	public static float VectorLength(in Vector3 delta) => delta.Length();

	public static void SinCos(float v, out float s, out float c) => (s, c) = float.SinCos(v);

	static readonly float[] power2_n = [
		1.152445441982634800E-041f, 2.304890883965269600E-041f, 4.609781767930539200E-041f, 9.219563535861078400E-041f,
		1.843912707172215700E-040f, 3.687825414344431300E-040f, 7.375650828688862700E-040f, 1.475130165737772500E-039f,
		2.950260331475545100E-039f, 5.900520662951090200E-039f, 1.180104132590218000E-038f, 2.360208265180436100E-038f,
		4.720416530360872100E-038f, 9.440833060721744200E-038f, 1.888166612144348800E-037f, 3.776333224288697700E-037f,
		7.552666448577395400E-037f, 1.510533289715479100E-036f, 3.021066579430958200E-036f, 6.042133158861916300E-036f,
		1.208426631772383300E-035f, 2.416853263544766500E-035f, 4.833706527089533100E-035f, 9.667413054179066100E-035f,
		1.933482610835813200E-034f, 3.866965221671626400E-034f, 7.733930443343252900E-034f, 1.546786088668650600E-033f,
		3.093572177337301200E-033f, 6.187144354674602300E-033f, 1.237428870934920500E-032f, 2.474857741869840900E-032f,
		4.949715483739681800E-032f, 9.899430967479363700E-032f, 1.979886193495872700E-031f, 3.959772386991745500E-031f,
		7.919544773983491000E-031f, 1.583908954796698200E-030f, 3.167817909593396400E-030f, 6.335635819186792800E-030f,
		1.267127163837358600E-029f, 2.534254327674717100E-029f, 5.068508655349434200E-029f, 1.013701731069886800E-028f,
		2.027403462139773700E-028f, 4.054806924279547400E-028f, 8.109613848559094700E-028f, 1.621922769711818900E-027f,
		3.243845539423637900E-027f, 6.487691078847275800E-027f, 1.297538215769455200E-026f, 2.595076431538910300E-026f,
		5.190152863077820600E-026f, 1.038030572615564100E-025f, 2.076061145231128300E-025f, 4.152122290462256500E-025f,
		8.304244580924513000E-025f, 1.660848916184902600E-024f, 3.321697832369805200E-024f, 6.643395664739610400E-024f,
		1.328679132947922100E-023f, 2.657358265895844200E-023f, 5.314716531791688300E-023f, 1.062943306358337700E-022f,
		2.125886612716675300E-022f, 4.251773225433350700E-022f, 8.503546450866701300E-022f, 1.700709290173340300E-021f,
		3.401418580346680500E-021f, 6.802837160693361100E-021f, 1.360567432138672200E-020f, 2.721134864277344400E-020f,
		5.442269728554688800E-020f, 1.088453945710937800E-019f, 2.176907891421875500E-019f, 4.353815782843751100E-019f,
		8.707631565687502200E-019f, 1.741526313137500400E-018f, 3.483052626275000900E-018f, 6.966105252550001700E-018f,
		1.393221050510000300E-017f, 2.786442101020000700E-017f, 5.572884202040001400E-017f, 1.114576840408000300E-016f,
		2.229153680816000600E-016f, 4.458307361632001100E-016f, 8.916614723264002200E-016f, 1.783322944652800400E-015f,
		3.566645889305600900E-015f, 7.133291778611201800E-015f, 1.426658355722240400E-014f, 2.853316711444480700E-014f,
		5.706633422888961400E-014f, 1.141326684577792300E-013f, 2.282653369155584600E-013f, 4.565306738311169100E-013f,
		9.130613476622338300E-013f, 1.826122695324467700E-012f, 3.652245390648935300E-012f, 7.304490781297870600E-012f,
		1.460898156259574100E-011f, 2.921796312519148200E-011f, 5.843592625038296500E-011f, 1.168718525007659300E-010f,
		2.337437050015318600E-010f, 4.674874100030637200E-010f, 9.349748200061274400E-010f, 1.869949640012254900E-009f,
		3.739899280024509800E-009f, 7.479798560049019500E-009f, 1.495959712009803900E-008f, 2.991919424019607800E-008f,
		5.983838848039215600E-008f, 1.196767769607843100E-007f, 2.393535539215686200E-007f, 4.787071078431372500E-007f,
		9.574142156862745000E-007f, 1.914828431372549000E-006f, 3.829656862745098000E-006f, 7.659313725490196000E-006f,
		1.531862745098039200E-005f, 3.063725490196078400E-005f, 6.127450980392156800E-005f, 1.225490196078431400E-004f,
		2.450980392156862700E-004f, 4.901960784313725400E-004f, 9.803921568627450800E-004f, 1.960784313725490200E-003f,
		3.921568627450980300E-003f, 7.843137254901960700E-003f, 1.568627450980392100E-002f, 3.137254901960784300E-002f,
		6.274509803921568500E-002f, 1.254901960784313700E-001f, 2.509803921568627400E-001f, 5.019607843137254800E-001f,
		1.003921568627451000E+000f, 2.007843137254901900E+000f, 4.015686274509803900E+000f, 8.031372549019607700E+000f,
		1.606274509803921500E+001f, 3.212549019607843100E+001f, 6.425098039215686200E+001f, 1.285019607843137200E+002f,
		2.570039215686274500E+002f, 5.140078431372548900E+002f, 1.028015686274509800E+003f, 2.056031372549019600E+003f,
		4.112062745098039200E+003f, 8.224125490196078300E+003f, 1.644825098039215700E+004f, 3.289650196078431300E+004f,
		6.579300392156862700E+004f, 1.315860078431372500E+005f, 2.631720156862745100E+005f, 5.263440313725490100E+005f,
		1.052688062745098000E+006f, 2.105376125490196000E+006f, 4.210752250980392100E+006f, 8.421504501960784200E+006f,
		1.684300900392156800E+007f, 3.368601800784313700E+007f, 6.737203601568627400E+007f, 1.347440720313725500E+008f,
		2.694881440627450900E+008f, 5.389762881254901900E+008f, 1.077952576250980400E+009f, 2.155905152501960800E+009f,
		4.311810305003921500E+009f, 8.623620610007843000E+009f, 1.724724122001568600E+010f, 3.449448244003137200E+010f,
		6.898896488006274400E+010f, 1.379779297601254900E+011f, 2.759558595202509800E+011f, 5.519117190405019500E+011f,
		1.103823438081003900E+012f, 2.207646876162007800E+012f, 4.415293752324015600E+012f, 8.830587504648031200E+012f,
		1.766117500929606200E+013f, 3.532235001859212500E+013f, 7.064470003718425000E+013f, 1.412894000743685000E+014f,
		2.825788001487370000E+014f, 5.651576002974740000E+014f, 1.130315200594948000E+015f, 2.260630401189896000E+015f,
		4.521260802379792000E+015f, 9.042521604759584000E+015f, 1.808504320951916800E+016f, 3.617008641903833600E+016f,
		7.234017283807667200E+016f, 1.446803456761533400E+017f, 2.893606913523066900E+017f, 5.787213827046133800E+017f,
		1.157442765409226800E+018f, 2.314885530818453500E+018f, 4.629771061636907000E+018f, 9.259542123273814000E+018f,
		1.851908424654762800E+019f, 3.703816849309525600E+019f, 7.407633698619051200E+019f, 1.481526739723810200E+020f,
		2.963053479447620500E+020f, 5.926106958895241000E+020f, 1.185221391779048200E+021f, 2.370442783558096400E+021f,
		4.740885567116192800E+021f, 9.481771134232385600E+021f, 1.896354226846477100E+022f, 3.792708453692954200E+022f,
		7.585416907385908400E+022f, 1.517083381477181700E+023f, 3.034166762954363400E+023f, 6.068333525908726800E+023f,
		1.213666705181745400E+024f, 2.427333410363490700E+024f, 4.854666820726981400E+024f, 9.709333641453962800E+024f,
		1.941866728290792600E+025f, 3.883733456581585100E+025f, 7.767466913163170200E+025f, 1.553493382632634000E+026f,
		3.106986765265268100E+026f, 6.213973530530536200E+026f, 1.242794706106107200E+027f, 2.485589412212214500E+027f,
		4.971178824424429000E+027f, 9.942357648848857900E+027f, 1.988471529769771600E+028f, 3.976943059539543200E+028f,
		7.953886119079086300E+028f, 1.590777223815817300E+029f, 3.181554447631634500E+029f, 6.363108895263269100E+029f,
		1.272621779052653800E+030f, 2.545243558105307600E+030f, 5.090487116210615300E+030f, 1.018097423242123100E+031f,
		2.036194846484246100E+031f, 4.072389692968492200E+031f, 8.144779385936984400E+031f, 1.628955877187396900E+032f,
		3.257911754374793800E+032f, 6.515823508749587500E+032f, 1.303164701749917500E+033f, 2.606329403499835000E+033f,
		5.212658806999670000E+033f, 1.042531761399934000E+034f, 2.085063522799868000E+034f, 4.170127045599736000E+034f,
		8.340254091199472000E+034f, 1.668050818239894400E+035f, 3.336101636479788800E+035f, 6.672203272959577600E+035f
	];

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float TexLightToLinear(int c, int exponent) {
		Assert(exponent >= -128 && exponent <= 127);
		return (float)c * power2_n[exponent + 128];
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void VectorRotate(in Vector3 in1, in Matrix3x4 in2, out Vector3 outVec) {
		outVec.X = in1.X * in2.M00 + in1.Y * in2.M01 + in1.Z * in2.M02;
		outVec.Y = in1.X * in2.M10 + in1.Y * in2.M11 + in1.Z * in2.M12;
		outVec.Z = in1.X * in2.M20 + in1.Y * in2.M21 + in1.Z * in2.M22;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool VectorCompare(in Vector3 v1, in Vector3 v2) {
		return v1 == v2;
	}



	public static void VectorIRotate(ReadOnlySpan<float> in1, in Matrix3x4 in2, Span<float> @out) {
		@out[0] = in1[0] * in2[0][0] + in1[1] * in2[1][0] + in1[2] * in2[2][0];
		@out[1] = in1[0] * in2[0][1] + in1[1] * in2[1][1] + in1[2] * in2[2][1];
		@out[2] = in1[0] * in2[0][2] + in1[1] * in2[1][2] + in1[2] * in2[2][2];
	}

	public static unsafe void VectorIRotate(in Vector3 relVelocity, in Matrix3x4 in2, out Vector3 @out) {
		fixed (Vector3* pin1 = &relVelocity)
		fixed (Vector3* pout = &@out) {
			VectorIRotate(new ReadOnlySpan<float>(pin1, 3), in in2, new Span<float>(pout, 3));
		}
	}
}
