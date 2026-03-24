using System;
using System.Collections.Generic;
using System.Text;

namespace Source.Engine;

public partial class SV {
	public void ClearWorld(){
		StaticPropMgr().LevelShutdown();

		for (int i = 0; i < 3; i++) 
			if (host_state.WorldModel!.Mins[i] < MIN_COORD_INTEGER || host_state.WorldModel!.Maxs[i] > MAX_COORD_INTEGER) 
				Host.EndGame(true, "Map coordinate extents are too large!!\nCheck for errors!\n");

		SpatialPartition().Init(host_state.WorldModel!.Mins, host_state.WorldModel!.Maxs);
	}
}
