using System.Numerics;

namespace Source.Common.Mathematics;

public enum LightType
{
	Disable = 0,
	Point,
	Directional,
	Spot
}

public struct LightDesc
{
	public LightType Type;                          //< MATERIAL_LIGHT_xxx
	public Vector3 Color;                           //< color+intensity
	public Vector3 Position;                        //< light source center position
	public Vector3 Direction;                       //< for SPOT, direction it is pointing
	public float Range;                             //< distance range for light.0=infinite
	public float Falloff;                           //< angular falloff exponent for spot lights
	public float Attenuation0;                      //< constant distance falloff term
	public float Attenuation1;                      //< linear term of falloff
	public float Attenuation2;                      //< quadatic term of falloff
	public float Theta;                             //< inner cone angle. no angular falloff within this cone
	public float Phi;                               //< outer cone angle

	// the values below are derived from the above settings for optimizations
	public float ThetaDot;
	public float PhiDot;
	public uint Flags;
	private float OneOver_ThetaDot_Minus_PhiDot;
	private float RangeSquared;
}
