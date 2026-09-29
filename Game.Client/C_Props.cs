using Game.Client;
using Game.Shared;

using Source.Common;


namespace Game.Client;
using System.Numerics;
using FIELD_DP = Source.FIELD<C_DynamicProp>;
using FIELD_PBM = Source.FIELD<PhysBoxMultiplayer>;
using FIELD_BPD = Source.FIELD<C_BasePropDoor>;
using FIELD_PPM = Source.FIELD<PhysicsPropMultiplayer>;

[NetworkName("CDynamicProp")]
public class C_DynamicProp : C_BreakableProp
{
	public static readonly RecvTable DT_DynamicProp = new(DT_BreakableProp, [
		RecvPropBool(FIELD_DP.OF(nameof(UseHitboxesForRenderBox)))
	]);
	public static readonly new ClientClass ClientClass = new ClientClass(DT_DynamicProp);
	[NetworkName("m_bUseHitboxesForRenderBox")]
	public bool UseHitboxesForRenderBox;
}


[NetworkName("CPhysicsPropMultiplayer")]
public class PhysicsPropMultiplayer : C_PhysicsProp
{
	public static readonly RecvTable DT_PhysicsPropMultiplayer = new(DT_PhysicsProp, [
		RecvPropInt(FIELD_PPM.OF(nameof(PhysicsMode))),
		RecvPropFloat(FIELD_PPM.OF(nameof(Mass))),
		RecvPropVector(FIELD_PPM.OF(nameof(CollisionMins))),
		RecvPropVector(FIELD_PPM.OF(nameof(CollisionMaxs))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass(DT_PhysicsPropMultiplayer);

	[NetworkName("m_iPhysicsMode")]
	public int PhysicsMode;
	[NetworkName("m_fMass")]
	public float Mass;
	[NetworkName("m_collisionMins")]
	public Vector3 CollisionMins;
	[NetworkName("m_collisionMaxs")]
	public Vector3 CollisionMaxs;
}

[NetworkName("CPhysBoxMultiplayer")]
public class PhysBoxMultiplayer : C_PhysBox
{
	public static readonly RecvTable DT_PhysBoxMultiplayer = new(DT_PhysBox, [
		RecvPropInt(FIELD_PBM.OF(nameof(PhysicsMode)), PropFlags.Unsigned),
		RecvPropFloat(FIELD_PBM.OF(nameof(Mass)))
	]);
	public static readonly new ClientClass ClientClass = new ClientClass(DT_PhysBoxMultiplayer);
	[NetworkName("m_iPhysicsMode")]
	public int PhysicsMode;
	[NetworkName("m_fMass")]
	public float Mass;
}


[NetworkName("CBasePropDoor")]
public class C_BasePropDoor : C_DynamicProp
{
	[NetworkName("m_bLocked")]
	bool Locked;
	[NetworkName("m_eDoorState")]
	int DoorState;
	public static readonly RecvTable DT_BasePropDoor = new(DT_DynamicProp, [
		RecvPropBool(FIELD_BPD.OF(nameof(Locked))),
		RecvPropInt(FIELD_BPD.OF(nameof(DoorState)))
	]);
	public static readonly new ClientClass ClientClass = new ClientClass(DT_BasePropDoor);
}


[NetworkName("CPropDoorRotating")]
public class C_PropDoorRotating : C_BasePropDoor
{
	public static readonly RecvTable DT_PropDoorRotating = new(DT_BasePropDoor, []);
	public static readonly new ClientClass ClientClass = new ClientClass(DT_PropDoorRotating);
}
