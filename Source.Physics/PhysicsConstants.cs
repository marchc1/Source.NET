global using static Source.Physics.PrivatePhysicsConstants;
namespace Source.Physics;

public static class PrivatePhysicsConstants {
	public static int MAKEID(char d, char c, char b, char a) => ((int)(a) << 24) | ((int)(b) << 16) | ((int)(c) << 8) | (int)(d);

	public static readonly int IVP_COMPACT_SURFACE_ID = MAKEID('I', 'V', 'P', 'S');
	public static readonly int IVP_COMPACT_SURFACE_ID_SWAPPED = MAKEID('S', 'P', 'V', 'I');
	public static readonly int IVP_COMPACT_MOPP_ID = MAKEID('M', 'O', 'P', 'P');
	public static readonly int VPHYSICS_COLLISION_ID = MAKEID('V', 'P', 'H', 'Y');
	public static readonly int VPHYSICS_COLLISION_VERSION = 0x0100;
}
