#if CLIENT_DLL || GAME_DLL
using Source.Common;
namespace Game.Shared.HL1;
using FIELD = Source.FIELD<WeaponShotgun_HL1>;
public class WeaponShotgun_HL1 : BaseHL1MPCombatWeapon
{
	public static readonly
#if CLIENT_DLL
		RecvTable
#else
		SendTable
#endif
		DT_WeaponShotgun_HL1 = new(DT_BaseHL1MPCombatWeapon, [
#if CLIENT_DLL
			RecvPropFloat(FIELD.OF(nameof(PumpTime))),
			RecvPropInt(FIELD.OF(nameof(InSpecialReload))),
#else
			SendPropFloat(FIELD.OF(nameof(PumpTime)), 0, PropFlags.NoScale),
			SendPropInt(FIELD.OF(nameof(InSpecialReload)), 32),
#endif
		]);
#if CLIENT_DLL
	public static readonly new ClientClass ClientClass = new ClientClass("WeaponShotgun_HL1", null, null, DT_WeaponShotgun_HL1).WithManualClassID(StaticClassIndices.CWeaponShotgun_HL1);
#else
	public static readonly new ServerClass ServerClass = new ServerClass("WeaponShotgun_HL1", DT_WeaponShotgun_HL1).WithManualClassID(StaticClassIndices.CWeaponShotgun_HL1);
#endif
	public float PumpTime;
	public float InSpecialReload;
}
#endif
