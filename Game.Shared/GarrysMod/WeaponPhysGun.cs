#if (CLIENT_DLL || GAME_DLL) && GMOD_DLL
using Source.Common;
using Game.Shared;
using Source;
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

	public readonly EHANDLE PhysBeam = new();
	public Vector3 HitPosLocal;
	public readonly EHANDLE GrabbedEntity = new();
}
#endif
