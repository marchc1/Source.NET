using System.Numerics;

namespace Source.Physics;

internal static class IVPConvert
{
	public const float METERS_PER_INCH = 0.0254f;
	public const float HL2IVP_FACTOR = METERS_PER_INCH;
	public const float IVP2HL_FACTOR = 1.0f / METERS_PER_INCH;

	public static Vector3 GeometryToSim(in Vector3 ivp) => new(ivp.X, ivp.Z, -ivp.Y);

	public static Vector3 PositionToIVP(in Vector3 hl) => hl * HL2IVP_FACTOR;
	public static Vector3 PositionToHL(in Vector3 ivp) => ivp * IVP2HL_FACTOR;

	public static Vector3 ForceImpulseToIVP(in Vector3 hl) => hl * HL2IVP_FACTOR;

	public static Vector3 AngularToIVP(in Vector3 hl) => hl;
	public static Vector3 AngularToHL(in Vector3 ivp) => ivp;

	public static Quaternion RotationToIVP(in Quaternion hl) => hl;
	public static Quaternion RotationToHL(in Quaternion ivp) => ivp;
}
