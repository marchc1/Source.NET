using System.Numerics;

namespace Source.Common.Mathematics;

public struct Vector
{
	public float X, Y, Z;

	public Vector() {
		X = 0;
		Y = 0;
		Z = 0;
	}

	public Vector(float xyz) {
		X = xyz;
		Y = xyz;
		Z = xyz;
	}
	public Vector(float x, float y, float z) {
		X = x;
		Y = y;
		Z = z;
	}

	public Vector(Vector3 vec) {
		X = vec.X;
		Y = vec.Y;
		Z = vec.Z;
	}

	public void Init() {
		X = 0;
		Y = 0;
		Z = 0;
	}

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
