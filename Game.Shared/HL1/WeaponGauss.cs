#if CLIENT_DLL || GAME_DLL
using Source.Common;
namespace Game.Shared.HL1;
using FIELD = Source.FIELD<WeaponGauss>;
public class WeaponGauss : BaseHL1MPCombatWeapon
{
	public static readonly
#if CLIENT_DLL
		RecvTable
#else
		SendTable
#endif
		DT_WeaponGauss = new(DT_BaseHL1MPCombatWeapon, [
#if CLIENT_DLL
			RecvPropInt(FIELD.OF(nameof(AttackState))),
			RecvPropBool(FIELD.OF(nameof(PrimaryFire))),
			RecvPropFloat(FIELD.OF(nameof(StartCharge))),
			RecvPropFloat(FIELD.OF(nameof(AmmoStartCharge))),
			RecvPropFloat(FIELD.OF(nameof(PlayAftershock))),
			RecvPropFloat(FIELD.OF(nameof(NextAmmoBurn)))
#else
			SendPropInt(FIELD.OF(nameof(AttackState)), 32),
			SendPropBool(FIELD.OF(nameof(PrimaryFire))),
			SendPropFloat(FIELD.OF(nameof(StartCharge)), 0, PropFlags.NoScale),
			SendPropFloat(FIELD.OF(nameof(AmmoStartCharge)), 0, PropFlags.NoScale),
			SendPropFloat(FIELD.OF(nameof(PlayAftershock)), 0, PropFlags.NoScale),
			SendPropFloat(FIELD.OF(nameof(NextAmmoBurn)), 0, PropFlags.NoScale)
#endif
		]);
#if CLIENT_DLL
	public static readonly new ClientClass ClientClass = new ClientClass("WeaponGauss", null, null, DT_WeaponGauss).WithManualClassID(StaticClassIndices.CWeaponGauss);
#else
	public static readonly new ServerClass ServerClass = new ServerClass("WeaponGauss", DT_WeaponGauss).WithManualClassID(StaticClassIndices.CWeaponGauss);
#endif
	public int AttackState;
	public bool PrimaryFire;
	public TimeUnit_t StartCharge;
	public TimeUnit_t AmmoStartCharge;
	public TimeUnit_t PlayAftershock;
	public TimeUnit_t NextAmmoBurn;
}
#endif
