#if CLIENT_DLL || GAME_DLL
using Source.Common;
namespace Game.Shared.HL1;
using FIELD = Source.FIELD<WeaponHandGrenade>;
public class WeaponHandGrenade : BaseHL1MPCombatWeapon
{
	public static readonly
#if CLIENT_DLL
		RecvTable
#else
		SendTable
#endif
		DT_WeaponHandGrenade = new(DT_BaseHL1MPCombatWeapon, [
#if CLIENT_DLL
			RecvPropFloat(FIELD.OF(nameof(StartThrow))),
			RecvPropFloat(FIELD.OF(nameof(ReleaseThrow))),
#else
			SendPropFloat(FIELD.OF(nameof(StartThrow)), 0, PropFlags.NoScale),
			SendPropFloat(FIELD.OF(nameof(ReleaseThrow)), 0, PropFlags.NoScale),
#endif
		]);
#if CLIENT_DLL
	public static readonly new ClientClass ClientClass = new ClientClass("WeaponHandGrenade", null, null, DT_WeaponHandGrenade).WithManualClassID(StaticClassIndices.CWeaponHandGrenade);
#else
	public static readonly new ServerClass ServerClass = new ServerClass("WeaponHandGrenade", DT_WeaponHandGrenade).WithManualClassID(StaticClassIndices.CWeaponHandGrenade);
#endif
	public TimeUnit_t StartThrow;
	public TimeUnit_t ReleaseThrow;
}
#endif
