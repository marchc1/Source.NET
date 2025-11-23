#if (CLIENT_DLL || GAME_DLL) && GMOD_DLL
using Source.Common;
using Game.Shared;

#if CLIENT_DLL
namespace Game.Client;
#else
namespace Game.Server;
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

// ====================================================================================================== //
// WeaponHL2MPBase
// ====================================================================================================== //

public partial class
#if CLIENT_DLL
    C_WeaponHL2MPBase
#else
	WeaponHL2MPBase
#endif
	: BaseCombatWeapon
{
	public static readonly Table DT_WeaponHL2MPBase = new(DT_BaseCombatWeapon, []);

	public static readonly new Class
#if CLIENT_DLL
		ClientClass
#else
		ServerClass
#endif
		= new Class("WeaponHL2MPBase", DT_WeaponHL2MPBase).WithManualClassID(StaticClassIndices.CWeaponHL2MPBase);
}

// ====================================================================================================== //
// BaseHL2MPCombatWeapon
// ====================================================================================================== //

public partial class
#if CLIENT_DLL
    C_BaseHL2MPCombatWeapon
#else
	BaseHL2MPCombatWeapon
#endif
	: BaseCombatWeapon
{
	public static readonly Table DT_BaseHL2MPCombatWeapon = new(DT_BaseCombatWeapon, []);

	public static readonly new Class
#if CLIENT_DLL
		ClientClass
#else
		ServerClass
#endif
		= new Class("BaseHL2MPCombatWeapon", DT_BaseHL2MPCombatWeapon).WithManualClassID(StaticClassIndices.CBaseHL2MPCombatWeapon);

	public void WeaponSound(WeaponSound soundType, TimeUnit_t soundtime = 0.0) {
#if CLIENT_DLL
#endif
	}
}
#endif
