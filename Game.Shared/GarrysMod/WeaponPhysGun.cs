#if (CLIENT_DLL || GAME_DLL) && GMOD_DLL
#if CLIENT_DLL
global using PhysBeam = Game.Client.C_PhysBeam;
#else
global using PhysBeam = Game.Server.PhysBeam;
#endif

using Game.Shared;

using Source;
using Source.Common;

using System.Numerics;

#if CLIENT_DLL
namespace Game.Client;

using FIELD = Source.FIELD<C_WeaponPhysGun>;
#else
namespace Game.Server;
using FIELD = Source.FIELD<WeaponPhysGun>;
#endif

using Table =
#if CLIENT_DLL
	RecvTable;
#else
	SendTable;
#endif

using Class =
#if CLIENT_DLL
	ClientClass;
#else
	ServerClass;
#endif



[LinkEntityToClass("weapon_physgun")]
[PrecacheWeaponRegister("weapon_physgun")]
public partial class
#if CLIENT_DLL
	C_WeaponPhysGun : C_BaseHL2MPCombatWeapon
#else
	WeaponPhysGun : BaseHL2MPCombatWeapon
#endif
{
	public static readonly Table DT_WeaponPhysGun = new(DT_BaseHL2MPCombatWeapon, [
#if CLIENT_DLL
		RecvPropEHandle(FIELD.OF(nameof(PhysBeam))),
		RecvPropVector(FIELD.OF(nameof(HitPosLocal))),
		RecvPropEHandle(FIELD.OF(nameof(GrabbedEntity)))
#elif GAME_DLL
		SendPropEHandle(FIELD.OF(nameof(PhysBeam))),
		SendPropVector(FIELD.OF(nameof(HitPosLocal)), 0, PropFlags.NoScale),
		SendPropEHandle(FIELD.OF(nameof(GrabbedEntity)))
#endif
	]);

	public static readonly new Class
#if CLIENT_DLL
		ClientClass
#else
		ServerClass
#endif
		= new Class("WeaponPhysGun", DT_WeaponPhysGun).WithManualClassID(StaticClassIndices.CWeaponPhysGun);

#if CLIENT_DLL
	public static readonly new DataMap PredMap = new([], typeof(C_WeaponPhysGun), BaseHL2MPCombatWeapon.PredMap); public override DataMap? GetPredDescMap() => PredMap;
#endif

	public EHANDLE PhysBeam = new();
	public Vector3 HitPosLocal;
	public EHANDLE GrabbedEntity = new();

	public override void Activate() {
		base.Activate();
	}

	// ActivityList?

	public
#if CLIENT_DLL
	C_WeaponPhysGun
#else
	WeaponPhysGun 
#endif
	() {

	}
	public override bool CanBePickedUpByNPCs() => false;
	public EHANDLE CreatePhysBeam() {
		RemovePhysBeam();

		if ((GetEFlags() & EFL.DirtyAbsTransform) != 0)
			CalcAbsolutePosition();

		return default;
	}
	public override bool Deploy() {
		DropEntity();
		CreatePhysBeam();
		return base.Deploy();
	}
	public override void Drop(in Vector3 velocity) {
		DropEntity();
		RemovePhysBeam();
		base.Drop(velocity);
	}
	public void DropEntity(){

	}
	public override float GetFireRate() => 0.01f;
	public override ReadOnlySpan<char> GetHoldType() => "physgun";
	public override bool HasAnyAmmo() => true;
	public override bool Holster(BaseCombatWeapon switchingTo) {
		DropEntity();
		RemovePhysBeam();
		return base.Holster(switchingTo);
	}
	public override void ItemPostFrame() {
		base.ItemPostFrame();
	}
	public override void Precache() {
		base.Precache();
	}
	public override void PrimaryAttack() {
		base.PrimaryAttack();
	}
	public override bool Reload() {
		return base.Reload();
	}
	public void RemovePhysBeam(){

	}
	public override void SecondaryAttack() {
		base.SecondaryAttack();
	}
	public override void UpdateOnRemove() {
		base.UpdateOnRemove();
	}
	public void UpdatePosition(){ }
	public void UpdateRotation(){ }
	public bool ValidatePhysObj() => false;
}
#endif
