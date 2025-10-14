using Game.Shared;

using Source.Common;

namespace Game.Server;

public class PhysMagnet : BaseAnimating
{
	public static readonly SendTable DT_PhysMagnet = new(DT_BaseAnimating, []);
	public static readonly new ServerClass ServerClass = new ServerClass("PhysMagnet", DT_PhysMagnet).WithManualClassID(StaticClassIndices.CPhysMagnet);
}
