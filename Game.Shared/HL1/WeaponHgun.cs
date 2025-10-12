#if CLIENT_DLL || GAME_DLL
using Source.Common;
namespace Game.Shared.HL1;
using FIELD = Source.FIELD<WeaponHgun>;
public class WeaponHgun : BaseHL1MPCombatWeapon
{
	public static readonly
#if CLIENT_DLL
		RecvTable
#else
		SendTable
#endif
		DT_WeaponHgun = new(DT_BaseHL1MPCombatWeapon, [
#if CLIENT_DLL
			RecvPropFloat(FIELD.OF(nameof(RechargeTime))),
			RecvPropInt(FIELD.OF(nameof(FirePhase))),
#else
			SendPropFloat(FIELD.OF(nameof(RechargeTime)), 0, PropFlags.NoScale),
			SendPropInt(FIELD.OF(nameof(FirePhase)), 32),
#endif
		]);
#if CLIENT_DLL
	public static readonly new ClientClass ClientClass = new ClientClass("WeaponHgun", null, null, DT_WeaponHgun).WithManualClassID(StaticClassIndices.CWeaponHgun);
#else
	public static readonly new ServerClass ServerClass = new ServerClass("WeaponHgun", DT_WeaponHgun).WithManualClassID(StaticClassIndices.CWeaponHgun);
#endif
	public TimeUnit_t RechargeTime;
	public int FirePhase;
}
#endif
