using CommunityToolkit.HighPerformance;

using Source.Common.Physics;

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace Source.Physics;

public static class PhysCollideParse
{
	public static PhysCollide? UnserializeFromBuffer(ReadOnlySpan<byte> buffer, int index, bool swap) {
		ref readonly PhysCollideHeader header = ref buffer.Cast<byte, PhysCollideHeader>()[0];
		if (header.VPhysicsID == VPHYSICS_COLLISION_ID) {
			Assert(header.Version == VPHYSICS_COLLISION_VERSION);
			switch (header.ModelType) {
				case CollideType.Poly:
					return new PhysCollideCompactSurface(in buffer.Cast<byte, CompactSurfaceHeader>()[0], index, swap);
				case CollideType.MOPP:
					DevMsg(2, "Null physics model\n");
					return null;
				default:
					Assert(false);
					return null;
			}
		}

		ref readonly IVP_Compact_Surface surface = ref buffer.Cast<byte, IVP_Compact_Surface>()[0];
		if (surface.dummy[2] == IVP_COMPACT_MOPP_ID) {
#if ENABLE_IVP_MOPP
		return new PhysCollideMopp( buffer, size );
#else
			Assert(false);
			return null;
#endif
		}
		if (surface.dummy[2] == IVP_COMPACT_SURFACE_ID || surface.dummy[2] == IVP_COMPACT_SURFACE_ID_SWAPPED || surface.dummy[2] == 0) {
			if (surface.dummy[2] == 0) {
				// UNDONE: Print a name here?
				DevMsg(1, "Old format .PHY file loaded!!!\n");
			}
			return new PhysCollideCompactSurface(buffer, index, swap);
		}

		Assert(false);
		return null;
	}
}
