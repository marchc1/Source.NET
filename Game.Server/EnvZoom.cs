using System;
using System.Collections.Generic;
using System.Text;

namespace Game.Server;

public class EnvZoom : PointEntity
{
	public static bool CanOverrideEnvZoomOwner(BaseEntity? zoomOwner) {
		if (zoomOwner is not EnvZoom zoom || zoom.HasSpawnFlags(1 << 0) == false)
			return false;
		return true;
	}
}
