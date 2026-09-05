using Game.Shared;

using System;
using System.Collections.Generic;
using System.Text;

namespace Game.Server;

[LinkEntityToClass("info_hint")]
[LinkEntityToClass("info_node")]
[LinkEntityToClass("info_node_hint")]
[LinkEntityToClass("info_node_air")]
[LinkEntityToClass("info_node_air_hint")]
[LinkEntityToClass("info_node_climb")]
public class NodeEnt : ServerOnlyPointEntity {

}
