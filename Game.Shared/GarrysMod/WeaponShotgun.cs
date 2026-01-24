#if (CLIENT_DLL || GAME_DLL) && GMOD_DLL
using Source.Common;
namespace Game.Shared.GarrysMod;
using FIELD = Source.FIELD<WeaponShotgun>;

[LinkEntityToClass("weapon_shotgun")]
[PrecacheWeaponRegister("weapon_shotgun")]
public class WeaponShotgun : BaseHL2MPCombatWeapon
{
	public static readonly
#if CLIENT_DLL
		RecvTable
#else
		SendTable
#endif
		DT_WeaponShotgun = new(DT_BaseHL2MPCombatWeapon, [
#if CLIENT_DLL
			RecvPropBool(FIELD.OF(nameof(NeedPump))),
			RecvPropBool(FIELD.OF(nameof(DelayedFire1))),
			RecvPropBool(FIELD.OF(nameof(DelayedFire2))),
			RecvPropBool(FIELD.OF(nameof(DelayedReload))),
#else
			SendPropBool(FIELD.OF(nameof(NeedPump))),
			SendPropBool(FIELD.OF(nameof(DelayedFire1))),
			SendPropBool(FIELD.OF(nameof(DelayedFire2))),
			SendPropBool(FIELD.OF(nameof(DelayedReload))),
#endif
		]);
#if CLIENT_DLL
	public static readonly new ClientClass ClientClass = new ClientClass("WeaponShotgun", null, null, DT_WeaponShotgun).WithManualClassID(StaticClassIndices.CWeaponShotgun);
	public static readonly new DataMap PredMap = new([], nameof(WeaponShotgun), BaseHL2MPCombatWeapon.PredMap); public override DataMap? GetPredDescMap() => PredMap;
#else
	public static readonly new ServerClass ServerClass = new ServerClass("WeaponShotgun", DT_WeaponShotgun).WithManualClassID(StaticClassIndices.CWeaponShotgun);
#endif
	public bool NeedPump;
	public bool DelayedFire1;
	public bool DelayedFire2;
	public bool DelayedReload;
}
#endif
