#if (CLIENT_DLL || GAME_DLL) && GMOD_DLL
using Source.Common;
namespace Game.Shared.GarrysMod;
using FIELD = Source.FIELD<WeaponSLAM>;

[LinkEntityToClass("weapon_slam")]
public class WeaponSLAM : BaseHL2MPCombatWeapon
{
	public static readonly
#if CLIENT_DLL
		RecvTable
#else
		SendTable
#endif
		DT_WeaponSLAM = new(DT_BaseHL2MPCombatWeapon, [
#if CLIENT_DLL
			RecvPropInt(FIELD.OF(nameof(SlamState))),
			RecvPropBool(FIELD.OF(nameof(DetonatorArmed))),
			RecvPropBool(FIELD.OF(nameof(NeedDetonatorDraw))),
			RecvPropBool(FIELD.OF(nameof(NeedDetonatorHolster))),
			RecvPropBool(FIELD.OF(nameof(NeedReload))),
			RecvPropBool(FIELD.OF(nameof(ClearReload))),
			RecvPropBool(FIELD.OF(nameof(ThrowSatchel))),
			RecvPropBool(FIELD.OF(nameof(AttachSatchel))),
			RecvPropBool(FIELD.OF(nameof(AttachTripmine)))
#else
			SendPropInt(FIELD.OF(nameof(SlamState))),
			SendPropBool(FIELD.OF(nameof(DetonatorArmed))),
			SendPropBool(FIELD.OF(nameof(NeedDetonatorDraw))),
			SendPropBool(FIELD.OF(nameof(NeedDetonatorHolster))),
			SendPropBool(FIELD.OF(nameof(NeedReload))),
			SendPropBool(FIELD.OF(nameof(ClearReload))),
			SendPropBool(FIELD.OF(nameof(ThrowSatchel))),
			SendPropBool(FIELD.OF(nameof(AttachSatchel))),
			SendPropBool(FIELD.OF(nameof(AttachTripmine)))
#endif
		]);
#if CLIENT_DLL
	public static readonly new ClientClass ClientClass = new ClientClass("WeaponSLAM", null, null, DT_WeaponSLAM).WithManualClassID(StaticClassIndices.CWeapon_SLAM);
#else
	public static readonly new ServerClass ServerClass = new ServerClass("WeaponSLAM", DT_WeaponSLAM).WithManualClassID(StaticClassIndices.CWeapon_SLAM);
#endif
	public int SlamState;
	public bool DetonatorArmed;
	public bool NeedDetonatorDraw;
	public bool NeedDetonatorHolster;
	public bool NeedReload;
	public bool ClearReload;
	public bool ThrowSatchel;
	public bool AttachSatchel;
	public bool AttachTripmine;
}
#endif
