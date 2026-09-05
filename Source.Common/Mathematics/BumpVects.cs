using System.Numerics;

using static Source.Constants;

namespace Source.Common.Mathematics;

public static class BumpVects
{
	public const float OO_SQRT_2 = 0.70710676908493042f;
	public const float OO_SQRT_3 = 0.57735025882720947f;
	public const float OO_SQRT_6 = 0.40824821591377258f;
	public const float OO_SQRT_2_OVER_3 = 0.81649661064147949f;

	public static readonly Vector3[] LocalBumpBasis = [
		new(OO_SQRT_2_OVER_3, 0.0f, OO_SQRT_3),
		new(-OO_SQRT_6, OO_SQRT_2, OO_SQRT_3),
		new(-OO_SQRT_6, -OO_SQRT_2, OO_SQRT_3),
	];

	public static void GetBumpNormals(in Vector3 sVect, in Vector3 tVect, in Vector3 flatNormal, in Vector3 phongNormal, Span<Vector3> bumpNormals) {
		Assert(bumpNormals.Length == NUM_BUMP_VECTS);

		MathLib.CrossProduct(in sVect, in tVect, out Vector3 tmpNormal);
		bool leftHanded = MathLib.DotProduct(flatNormal, tmpNormal) < 0.0f;

		Matrix3x4 smoothBasis = default;

		MathLib.CrossProduct(in phongNormal, in sVect, out Vector3 smooth1);
		MathLib.VectorNormalize(ref smooth1);

		MathLib.CrossProduct(in smooth1, in phongNormal, out Vector3 smooth2);
		MathLib.VectorNormalize(ref smooth2);

		smooth2.CopyTo(smoothBasis[0]);

		if (leftHanded)
			smooth1 = -smooth1;

		smooth1.CopyTo(smoothBasis[1]);
		phongNormal.CopyTo(smoothBasis[2]);

		for (int i = 0; i < NUM_BUMP_VECTS; i++)
			MathLib.VectorIRotate(in LocalBumpBasis[i], in smoothBasis, out bumpNormals[i]);
	}
}
