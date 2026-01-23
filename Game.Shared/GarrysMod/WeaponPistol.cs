#if (CLIENT_DLL || GAME_DLL) && GMOD_DLL

using Source.Common;
namespace Game.Shared.GarrysMod;
using FIELD = Source.FIELD<WeaponPistol>;

[LinkEntityToClass("weapon_pistol")]
public class WeaponPistol : HL2MPMachineGun
{
	public static readonly
#if CLIENT_DLL
		RecvTable
#else
		SendTable
#endif
		DT_WeaponPistol = new(DT_HL2MPMachineGun, [
#if CLIENT_DLL
			RecvPropFloat(FIELD.OF(nameof(SoonestPrimaryAttack))),
			RecvPropFloat(FIELD.OF(nameof(LastAttackTime))),
			RecvPropFloat(FIELD.OF(nameof(AccuracyPenalty))),
			RecvPropInt(FIELD.OF(nameof(NumShotsFired))),
#else
			SendPropFloat(FIELD.OF(nameof(SoonestPrimaryAttack)), 0, PropFlags.NoScale),
			SendPropFloat(FIELD.OF(nameof(LastAttackTime)), 0, PropFlags.NoScale),
			SendPropFloat(FIELD.OF(nameof(AccuracyPenalty)), 0, PropFlags.NoScale),
			SendPropInt(FIELD.OF(nameof(NumShotsFired))),
#endif
		]);
#if CLIENT_DLL
	public static readonly new ClientClass ClientClass = new ClientClass("WeaponPistol", null, null, DT_WeaponPistol).WithManualClassID(StaticClassIndices.CWeaponPistol);
	public static readonly new DataMap PredMap = new([], nameof(WeaponPistol), HL2MPMachineGun.PredMap); public override DataMap? GetPredDescMap() => PredMap;
#else
	public static readonly new ServerClass ServerClass = new ServerClass("WeaponPistol", DT_WeaponPistol).WithManualClassID(StaticClassIndices.CWeaponPistol);
#endif
	public TimeUnit_t SoonestPrimaryAttack;
	public TimeUnit_t LastAttackTime;
	public TimeUnit_t AccuracyPenalty;
	public int NumShotsFired;
}
#endif
