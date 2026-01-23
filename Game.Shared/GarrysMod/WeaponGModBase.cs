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


	public new void WeaponSound(WeaponSound soundType, TimeUnit_t soundTime = 0.0) {
#if CLIENT_DLL

#else
	base.WeaponSound(soundType, soundTime);
#endif
	}
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

	protected bool Lowered;
	protected TimeUnit_t RaiseTime;
	protected TimeUnit_t HolsterTime;

	public override bool Holster(BaseCombatWeapon switchingTo) {
		if(base.Holster(switchingTo)){
			SetWeaponVisible(false);
			HolsterTime = gpGlobals.CurTime;
			return true;
		}
		return false;
	}

#if CLIENT_DLL
	public override void OnDataChanged(DataUpdateType updateType) {
		base.OnDataChanged(updateType);
		if (GetPredictable() && !ShouldPredict())
			ShutdownPredictable();
	}

	public override bool ShouldPredict() {
		if (GetOwner() != null && GetOwner() == C_BasePlayer.GetLocalPlayer())
			return true;
		return base.ShouldPredict();
	}
#endif
}
#endif
