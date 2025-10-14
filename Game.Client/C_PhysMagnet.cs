using Game.Shared;

using Source.Common;

namespace Game.Client;

public class C_PhysMagnet : C_BaseEntity
{
	public static readonly RecvTable DT_PhysMagnet = new(DT_BaseEntity, []);
	public static readonly new ClientClass ClientClass = new ClientClass("PhysMagnet", DT_PhysMagnet).WithManualClassID(StaticClassIndices.CPhysMagnet);
}
