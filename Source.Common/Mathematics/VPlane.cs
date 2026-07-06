using System.Numerics;

namespace Source.Common.Mathematics;

public enum FrustumPlane
{
	Right = 0,
	Left = 1,
	Top = 2,
	Bottom = 3,
	NearZ = 4,
	FarZ = 5,
	NumPlanes = 6
}

public struct Frustum
{
	public VPlane Right;
	public VPlane Left;
	public VPlane Top;
	public VPlane Bottom;
	public VPlane NearZ;
	public VPlane FarZ;
	public readonly VPlane this[int index] => index switch {
		0 => Right,
		1 => Left,
		2 => Top,
		3 => Bottom,
		4 => NearZ,
		5 => FarZ,
		_ => throw new ArgumentOutOfRangeException()
	};
	public readonly VPlane this[FrustumPlane index] => index switch {
		FrustumPlane.Right => Right,
		FrustumPlane.Left => Left,
		FrustumPlane.Top => Top,
		FrustumPlane.Bottom => Bottom,
		FrustumPlane.NearZ => NearZ,
		FrustumPlane.FarZ => FarZ,
		_ => throw new ArgumentOutOfRangeException()
	};
	public void SetPlane(int index, in Vector3 normal, float dist) {
		switch (index) {
			case 0: Right.Normal = normal; Right.Dist = dist; break;
			case 1: Left.Normal = normal; Left.Dist = dist; break;
			case 2: Top.Normal = normal; Top.Dist = dist; break;
			case 3: Bottom.Normal = normal; Bottom.Dist = dist; break;
			case 4: NearZ.Normal = normal; NearZ.Dist = dist; break;
			case 5: FarZ.Normal = normal; FarZ.Dist = dist; break;
			default: throw new ArgumentOutOfRangeException();
		}
	}
}

public struct VPlane
{
	public const int SIDE_FRONT = 0;
	public const int SIDE_BACK = 1;
	public const int SIDE_ON = 2;
	public const float VP_EPSILON = 0.01f;

	public Vector3 Normal;
	public vec_t Dist;

	public void Init(in Vector3 normal, in vec_t dist) {

	}
	public vec_t DistTo(in Vector3 vec) {
		return 0; // todo
	}
}
