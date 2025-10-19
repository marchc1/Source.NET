using Game.Shared;

using Source.Common;

namespace Game.Client;

public class C_PhysMagnet : C_BaseAnimating
{
	public static readonly RecvTable DT_PhysMagnet = new(DT_BaseAnimating, []);
	public static readonly new ClientClass ClientClass = new ClientClass("PhysMagnet", DT_PhysMagnet).WithManualClassID(StaticClassIndices.CPhysMagnet);
}
