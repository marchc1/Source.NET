using IVP_FLOAT = float;
using IVP_DOUBLE = double;
using IVP_INT32 = int;
using IVP_UINT32 = uint;

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using CommunityToolkit.HighPerformance;

namespace Source.Physics;

// These exist for the sake of parsing.

[StructLayout(LayoutKind.Sequential)]
public struct IVP_U_Float_Point3
{
	public InlineArray3<IVP_FLOAT> k;
}

[StructLayout(LayoutKind.Sequential)]
public struct IVP_U_Float_Point
{
	public InlineArray3<IVP_FLOAT> k;
	public IVP_FLOAT hesse_val;
}

[StructLayout(LayoutKind.Sequential)]
public struct IVP_Compact_Surface
{
	public IVP_U_Float_Point3 mass_center;
	public IVP_U_Float_Point3 rotation_inertia;
	public IVP_FLOAT upper_limit_radius;
	public InlineArray4<byte> bitfield;
	public InlineArray3<int> dummy;

	public readonly uint max_factor_surface_deviation => bitfield[0];
	public readonly int byte_size {
		get {
			Span<byte> ret = stackalloc byte[4];
			bitfield[1..].CopyTo(ret[1..]);
			return ret.Cast<byte, int>()[0];
		}
	}
}
