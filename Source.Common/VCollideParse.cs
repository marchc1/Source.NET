using System.Collections.Specialized;
using System.Numerics;

namespace Source.Common.Physics;

/// <summary>
/// Analog of solid_t
/// </summary>
public struct Solid
{
	public int Index;
	public InlineArray512<char> Name;
	public InlineArray512<char> Parent;
	public InlineArray512<char> SurfaceProp;
	public Vector3 MassCenterOverride;
	public ObjectParams Params;
}
/// <summary>
/// Analog of fluid_t
/// </summary>
public struct Fluid
{
	public int Index;
	public InlineArray512<char> SurfaceProp;
	public FluidParams Params;
}

public interface IVPhysicsKeyHandler;
public interface IVPhysicsKeyParser;
