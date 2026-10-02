using Source.Common.Mathematics;

using System.Numerics;

namespace Source.Common.GarrysMod;

public enum GMODVariantType
{
	NIL,
	Number,
	Int,
	Bool,
	Vector,
	Angle,
	Entity,
	String,
	Count
}

public struct GMODVariant
{
	public GMODVariantType Type;
	public string? String;
	public bool Bool;
	public float Float;
	public int Int;
	public BaseHandle Ent; // Raphael defined this as a short; not sure if that's appropriate though
	public Vector3 Vec;
	public QAngle Ang;


	public static implicit operator int(in GMODVariant v) => v.Int;
	public static implicit operator bool(in GMODVariant v) => v.Bool;
	public static implicit operator float(in GMODVariant v) => v.Float;
	public static implicit operator Vector3(in GMODVariant v) => v.Vec;
	public static implicit operator QAngle(in GMODVariant v) => v.Ang;
	public static implicit operator string?(in GMODVariant v) => v.String;
}
