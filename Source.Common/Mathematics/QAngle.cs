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

	public static readonly QAngle Zero = new(0, 0, 0);

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
		fixed (QAngle* qptr = &this) {
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
	public void Init(float x, float y, float z) {
		X = x;
		Y = y;
		Z = z;
	}
	const float TORADS = MathF.PI / 180f;
	// TODO: is there a C# + SIMD way to do this?
	public void Vectors(out Vector3 forward, out Vector3 right, out Vector3 up)
		=> MathLib.AngleVectors(in this, out forward, out right, out up);
	public override string ToString() {
		return $"{{{X} {Y} {Z}}}";
	}
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
		MathLib.AngleQuaternion(this, out Quaternion quat);
		return quat;
	}
	
	public static QAngle Lerp(in QAngle q1, in QAngle q2, float percent) {
		Quaternion qa = q1.Quaternion();
		Quaternion qb = q2.Quaternion();
		MathLib.QuaternionSlerp(qa, qb, percent, out Quaternion qm);
		MathLib.QuaternionAngles(in qm, out QAngle angles);
		return angles;
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
