using Game.Client;
using Game.Shared;

using Source.Common;

namespace Game.Server;
using FIELD = Source.FIELD<C_PhysicsProp>;

public class C_PhysicsProp : C_BreakableProp
{
	public static readonly RecvTable DT_PhysicsProp = new(DT_BreakableProp, [
		RecvPropBool(FIELD.OF(nameof(Awake)))
	]);
	public static readonly new ClientClass ClientClass = new ClientClass("PhysicsProp", DT_PhysicsProp).WithManualClassID(StaticClassIndices.CBreakableProp);
	public bool Awake;
}
