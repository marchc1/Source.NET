using System.Numerics;
using System.Runtime.InteropServices;

namespace Source.Common.Mathematics;

public struct AddAngle
{
	public double Total;
	public double StartTime;
}

[StructLayout(LayoutKind.Sequential)]
public struct QAngle
{
	public float X, Y, Z;

	public QAngle() {
		X = 0;
		Y = 0;
		Z = 0;
	}

	public QAngle(float xyz) {
		X = xyz;
		Y = xyz;
		Z = xyz;
	}
	public QAngle(float x, float y, float z) {
		X = x;
		Y = y;
		Z = z;
	}

	public unsafe vec_t LengthSqr() {
		fixed(QAngle* qptr = &this) {
			return ((Vector3*)qptr)->LengthSquared();
		}
	}

	public QAngle(Vector3 vec) {
		X = vec.X;
		Y = vec.Y;
		Z = vec.Z;
	}

	public static implicit operator Vector3(QAngle angle) => new(angle.X, angle.Y, angle.Z);
	public static implicit operator QAngle(Vector3 vector) => new(vector);
	public static QAngle operator +(QAngle a, QAngle b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
	public static QAngle operator -(QAngle a, QAngle b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
	public static QAngle operator *(QAngle a, QAngle b) => new(a.X * b.X, a.Y * b.Y, a.Z * b.Z);
	public static QAngle operator /(QAngle a, QAngle b) => new(a.X / b.X, a.Y / b.Y, a.Z / b.Z);

	public static QAngle operator +(float a, QAngle b) => new(a + b.X, a + b.Y, a + b.Z);
	public static QAngle operator -(float a, QAngle b) => new(a - b.X, a - b.Y, a - b.Z);
	public static QAngle operator *(float a, QAngle b) => new(a * b.X, a * b.Y, a * b.Z);
	public static QAngle operator /(float a, QAngle b) => new(a / b.X, a / b.Y, a / b.Z);

	public static QAngle operator +(QAngle a, float b) => new(a.X + b, a.Y + b, a.Z + b);
	public static QAngle operator -(QAngle a, float b) => new(a.X - b, a.Y - b, a.Z - b);
	public static QAngle operator *(QAngle a, float b) => new(a.X * b, a.Y * b, a.Z * b);
	public static QAngle operator /(QAngle a, float b) => new(a.X / b, a.Y / b, a.Z / b);

	public static bool operator ==(QAngle a, QAngle b) => a.X == b.X && a.Y == b.Y && a.Z == b.Z;
	public static bool operator !=(QAngle a, QAngle b) => a.X != b.X || a.Y != b.Y || a.Z != b.Z;
	public void Init() {
		X = 0;
		Y = 0;
		Z = 0;
	}
	const float TORADS = MathF.PI / 180f;
	// TODO: is there a C# + SIMD way to do this?
	public void Vectors(out Vector3 forward, out Vector3 right, out Vector3 up)
		=> MathLib.AngleVectors(in this, out forward, out right, out up);

	public static float Normalize(float angle) {
		angle = MathLib.Fmodf(angle, 360.0f);
		if (angle > 180) {
			angle -= 360;
		}
		if (angle < -180) {
			angle += 360;
		}
		return angle;
	}
	private const float DEG2RAD = MathF.PI / 180f;
	private const float RAD2DEG = 180f / MathF.PI;

	public Quaternion Quaternion() {
		// Source's convention: pitch=X, yaw=Y, roll=Z
		// Rotation order: Yaw -> Pitch -> Roll (YXZ intrinsic)

		float halfPitch = X * DEG2RAD * 0.5f;
		float halfYaw = Y * DEG2RAD * 0.5f;
		float halfRoll = Z * DEG2RAD * 0.5f;

		float sp = MathF.Sin(halfPitch);
		float cp = MathF.Cos(halfPitch);
		float sy = MathF.Sin(halfYaw);
		float cy = MathF.Cos(halfYaw);
		float sr = MathF.Sin(halfRoll);
		float cr = MathF.Cos(halfRoll);

		// YXZ intrinsic rotation order
		Quaternion q = new() {
			W = cr * cp * cy + sr * sp * sy,
			X = cr * sp * cy + sr * cp * sy,
			Y = cr * cp * sy - sr * sp * cy,
			Z = cr * cp * cy * sr - sp * sy * cr + sr * cp * cy // Fixed
		};

		return System.Numerics.Quaternion.Normalize(q);
	}
	public static QAngle FromQuaternion(in Quaternion iq) {
		Quaternion q = System.Numerics.Quaternion.Normalize(iq);

		float xx = q.X * q.X;
		float yy = q.Y * q.Y;
		float zz = q.Z * q.Z;
		float xy = q.X * q.Y;
		float xz = q.X * q.Z;
		float yz = q.Y * q.Z;
		float wx = q.W * q.X;
		float wy = q.W * q.Y;
		float wz = q.W * q.Z;

		float m00 = 1f - 2f * (yy + zz);
		float m01 = 2f * (xy - wz);
		float m02 = 2f * (xz + wy);
		float m10 = 2f * (xy + wz);
		float m11 = 1f - 2f * (xx + zz);
		float m12 = 2f * (yz - wx);
		float m20 = 2f * (xz - wy);
		float m21 = 2f * (yz + wx);
		float m22 = 1f - 2f * (xx + yy);

		// Extract YXZ Euler angles
		float pitch, yaw, roll;

		float sinPitch = m10;
		if (MathF.Abs(sinPitch) >= 0.9999f) {
			pitch = MathF.CopySign(MathF.PI / 2f, sinPitch);
			roll = MathF.Atan2(-m02, m22);
			yaw = 0f;
		}
		else {
			pitch = MathF.Asin(sinPitch);
			roll = MathF.Atan2(-m12, m11);
			yaw = MathF.Atan2(-m20, m00);
		}

		return new QAngle(
			roll * RAD2DEG,
			yaw * RAD2DEG,
			pitch * RAD2DEG
		);
	}
	public static QAngle Lerp(in QAngle q1, in QAngle q2, float percent) {
		Quaternion qa = q1.Quaternion();
		Quaternion qb = q2.Quaternion();
		Quaternion qm = System.Numerics.Quaternion.Slerp(qa, qb, percent);
		return FromQuaternion(qm);
	}


	public static QAngle Normalize(in QAngle angle) => new(Normalize(angle.X), Normalize(angle.Y), Normalize(angle.Z));

	public float this[int index] {
		get {
			switch (index) {
				case 0: return X;
				case 1: return Y;
				case 2: return Z;
				default: throw new IndexOutOfRangeException();
			}
		}
		set {
			switch (index) {
				case 0: X = value; return;
				case 1: Y = value; return;
				case 2: Z = value; return;
				default: throw new IndexOutOfRangeException();
			}
		}
	}
}
