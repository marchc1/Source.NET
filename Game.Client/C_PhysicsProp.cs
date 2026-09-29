using Game.Client;
using Game.Shared;

using Source.Common;

namespace Game.Client;
using FIELD = Source.FIELD<C_PhysicsProp>;

[NetworkName("CPhysicsProp")]
public class C_PhysicsProp : C_BreakableProp
{
	public static readonly RecvTable DT_PhysicsProp = new(DT_BreakableProp, [
		RecvPropBool(FIELD.OF(nameof(Awake)))
	]);
	public static readonly new ClientClass ClientClass = new ClientClass(DT_PhysicsProp);
	[NetworkName("m_bAwake")]
	public bool Awake;
}
