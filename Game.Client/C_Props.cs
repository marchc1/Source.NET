using Game.Client;
using Game.Shared;

using Source.Common;


namespace Game.Client;
using FIELD_DP = Source.FIELD<C_DynamicProp>;
using FIELD_PBM = Source.FIELD<PhysBoxMultiplayer>;

public class C_DynamicProp : C_BreakableProp
{
	public static readonly RecvTable DT_DynamicProp = new(DT_BreakableProp, [
		RecvPropBool(FIELD_DP.OF(nameof(UseHitboxesForRenderBox)))
	]);
	public static readonly new ClientClass ClientClass = new ClientClass("DynamicProp", DT_DynamicProp).WithManualClassID(StaticClassIndices.CDynamicProp);
	public bool UseHitboxesForRenderBox;
}


public class PhysicsPropMultiplayer : C_PhysicsProp
{
	public static readonly RecvTable DT_PhysicsPropMultiplayer = new(DT_PhysicsProp, [

	]);
	public static readonly new ClientClass ClientClass = new ClientClass("PhysicsPropMultiplayer", DT_PhysicsPropMultiplayer).WithManualClassID(StaticClassIndices.CPhysicsPropMultiplayer);
}

public class PhysBoxMultiplayer : C_PhysBox
{
	public static readonly RecvTable DT_PhysBoxMultiplayer = new(DT_PhysBox, [
		RecvPropInt(FIELD_PBM.OF(nameof(PhysicsMode))),
		RecvPropFloat(FIELD_PBM.OF(nameof(Mass)))
	]);
	public static readonly new ClientClass ClientClass = new ClientClass("PhysBoxMultiplayer", DT_PhysBoxMultiplayer).WithManualClassID(StaticClassIndices.CPhysBoxMultiplayer);
	public int PhysicsMode;
	public float Mass;
}


public class C_BasePropDoor : C_DynamicProp
{
	public static readonly RecvTable DT_BasePropDoor = new(DT_DynamicProp, []);
	public static readonly new ClientClass ClientClass = new ClientClass("BasePropDoor", DT_BasePropDoor).WithManualClassID(StaticClassIndices.CBasePropDoor);
}


public class C_PropDoorRotating : C_BasePropDoor
{
	public static readonly RecvTable DT_PropDoorRotating = new(DT_BasePropDoor, []);
	public static readonly new ClientClass ClientClass = new ClientClass("PropDoorRotating", DT_PropDoorRotating).WithManualClassID(StaticClassIndices.CPropDoorRotating);
}
