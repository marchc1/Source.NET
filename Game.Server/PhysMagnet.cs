using Game.Shared;

using Source.Common;

namespace Game.Server;

[LinkEntityToClass("phys_magnet")]
[NetworkName("CPhysMagnet")]
public class PhysMagnet : BaseAnimating
{
	public static readonly SendTable DT_PhysMagnet = new(DT_BaseAnimating, []);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_PhysMagnet);
}
