#if CLIENT_DLL || GAME_DLL
#if CLIENT_DLL
global using BaseCombatWeapon = Game.Client.C_BaseCombatWeapon;
global using WeaponHL2MPBase = Game.Client.C_WeaponHL2MPBase;
global using BaseHL2MPCombatWeapon = Game.Client.C_BaseHL2MPCombatWeapon;
#elif GAME_DLL
global using BaseCombatWeapon = Game.Server.BaseCombatWeapon;
global using WeaponHL2MPBase = Game.Server.WeaponHL2MPBase;
global using BaseHL2MPCombatWeapon = Game.Server.BaseHL2MPCombatWeapon;
#endif

using Source.Common;
using Game.Shared;
using Source.Common.Engine;


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
#if CLIENT_DLL || GAME_DLL
using FIELD = Source.FIELD<BaseCombatWeapon>;
#endif

public partial class
#if CLIENT_DLL
    C_BaseCombatWeapon : C_BaseAnimating
#elif GAME_DLL
	BaseCombatWeapon : BaseAnimating
#else
	SHUT_UP_ABOUT_GAME_SHARED_INTELLISENSE
#endif
{
#if !CLIENT_DLL && !GAME_DLL // God intellisense is annoying me. Fixme when we can get Intellisense to shut up about Game.Shared (it never gets built)
	public static readonly Table DT_BaseAnimating = new();
#endif
	public static readonly Table DT_LocalWeaponData = new([
#if CLIENT_DLL
		RecvPropIntWithMinusOneFlag(FIELD.OF(nameof(Clip1))),
		RecvPropIntWithMinusOneFlag(FIELD.OF(nameof(Clip1))),
		RecvPropInt(FIELD.OF(nameof(PrimaryAmmoType))),
		RecvPropInt(FIELD.OF(nameof(SecondaryAmmoType))),
		RecvPropInt(FIELD.OF(nameof(ViewModelIndex))),
		RecvPropInt(FIELD.OF(nameof(FlipViewModel))),
#elif GAME_DLL
		SendPropIntWithMinusOneFlag(FIELD.OF(nameof(Clip1)), 16),
		SendPropIntWithMinusOneFlag(FIELD.OF(nameof(Clip1)), 16),
		SendPropInt(FIELD.OF(nameof(PrimaryAmmoType)), 8),
		SendPropInt(FIELD.OF(nameof(SecondaryAmmoType)), 8),
		SendPropInt(FIELD.OF(nameof(ViewModelIndex)), BaseViewModel.VIEWMODEL_INDEX_BITS, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(FlipViewModel)), 8),
#endif
	]); public static readonly Class SC_LocalWeaponData = new Class("LocalWeaponData", DT_LocalWeaponData);

	public static readonly Table DT_LocalActiveWeaponData = new([
#if CLIENT_DLL
		RecvPropTime(FIELD.OF(nameof(NextPrimaryAttack))),
		RecvPropTime(FIELD.OF(nameof(NextSecondaryAttack))),
		RecvPropInt(FIELD.OF(nameof(NextThinkTick))),
		RecvPropTime(FIELD.OF(nameof(TimeWeaponIdle))),
#elif GAME_DLL
		SendPropTime(FIELD.OF(nameof(NextPrimaryAttack))),
		SendPropTime(FIELD.OF(nameof(NextSecondaryAttack))),
		SendPropInt(FIELD.OF(nameof(NextThinkTick))),
		SendPropTime(FIELD.OF(nameof(TimeWeaponIdle))),
#endif
	]); public static readonly Class SC_LocalActiveWeaponData = new Class("LocalActiveWeaponData", DT_LocalActiveWeaponData);

	public static readonly Table DT_BaseCombatWeapon = new(DT_BaseAnimating, [
#if CLIENT_DLL
		RecvPropDataTable("LocalWeaponData", DT_LocalWeaponData),
		RecvPropDataTable("LocalActiveWeaponData", DT_LocalActiveWeaponData),
		RecvPropInt(FIELD.OF(nameof(ViewModelIndex))),
		RecvPropInt(FIELD.OF(nameof(WorldModelIndex))),
		RecvPropInt(FIELD.OF(nameof(State)), 0, RecvProxy_WeaponState),
		RecvPropEHandle(FIELD.OF(nameof(Owner))),
#elif GAME_DLL
		SendPropDataTable("LocalWeaponData", DT_LocalWeaponData, SendProxy_SendLocalWeaponDataTable),
		SendPropDataTable("LocalActiveWeaponData", DT_LocalActiveWeaponData, SendProxy_SendActiveLocalWeaponDataTable ),
		SendPropModelIndex(FIELD.OF(nameof(ViewModelIndex))),
		SendPropModelIndex(FIELD.OF(nameof(WorldModelIndex))),
		SendPropInt(FIELD.OF(nameof(State)), 8, PropFlags.Unsigned),
		SendPropEHandle(FIELD.OF(nameof(Owner))),
#endif
	]);

#if CLIENT_DLL
	private static void RecvProxy_WeaponState(ref readonly RecvProxyData data, object instance, IFieldAccessor field) {
		BaseCombatWeapon weapon = (BaseCombatWeapon)instance;
		weapon.State = data.Value.Int;
		weapon.UpdateVisibility();
	}
#else

	private static object? SendProxy_SendLocalWeaponDataTable(SendProp prop, object instance, IFieldAccessor data, SendProxyRecipients recipients, int objectID) {
		throw new NotImplementedException();
	}
	private static object? SendProxy_SendActiveLocalWeaponDataTable(SendProp prop, object instance, IFieldAccessor data, SendProxyRecipients recipients, int objectID) {
		throw new NotImplementedException();
	}
#endif
	public static readonly new Class
#if CLIENT_DLL
		ClientClass
#else
		ServerClass
#endif
		= new Class("BaseCombatWeapon", DT_BaseCombatWeapon).WithManualClassID(StaticClassIndices.CBaseCombatWeapon);

	public int Clip1;
	public int Clip2;
	public int PrimaryAmmoType;
	public int SecondaryAmmoType;
	public int ViewModelIndex;
	public int WorldModelIndex;
	public bool FlipViewModel;

	public TimeUnit_t NextPrimaryAttack;
	public TimeUnit_t NextSecondaryAttack;
	public TimeUnit_t TimeWeaponIdle;

	public int State;
	public readonly EHANDLE Owner = new();

	public virtual bool IsOverridingViewmodel() => false;
	public virtual int DrawOverriddenViewmodel(BaseViewModel viewmodel, StudioFlags flags) => 0;
	public virtual void ViewModelDrawn(BaseViewModel viewmodelflags) { }

	

	public BaseCombatCharacter? GetOwner() => ToBaseCombatCharacter(Owner.Get());

	public bool SetIdealActivity(Activity ideal) {
		return false; // todo
	}
	public bool SendWeaponAnim(Activity act) {
		return SetIdealActivity(act);
	}
	public void WeaponSound(WeaponSound soundType, TimeUnit_t soundTime = 0.0) {

	}

	public override void Precache() {
		PrimaryAmmoType = SecondaryAmmoType = -1;
		if(WeaponParse.ReadWeaponDataFromFileForSlot(filesystem, GetClassname(), out WeaponFileInfoHandle)){

		}
	}

	WEAPON_FILE_INFO_HANDLE WeaponFileInfoHandle;

	public FileWeaponInfo GetWpnData(){
		return WeaponParse.GetFileWeaponInfoFromHandle(WeaponFileInfoHandle);
	}
}
#endif
