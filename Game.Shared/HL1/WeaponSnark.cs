#if CLIENT_DLL || GAME_DLL
using Source.Common;
namespace Game.Shared.HL1;
using FIELD = Source.FIELD<WeaponSnark>;
public class WeaponSnark : BaseHL1CombatWeapon
{
	public static readonly
#if CLIENT_DLL
		RecvTable
#else
		SendTable
#endif
		DT_WeaponSnark = new(DT_BaseHL1CombatWeapon, [
#if CLIENT_DLL
			RecvPropBool(FIELD.OF(nameof(JustThrown))),
#else
			SendPropBool(FIELD.OF(nameof(JustThrown))),
#endif
		]);
#if CLIENT_DLL
	public static readonly new ClientClass ClientClass = new ClientClass("WeaponSnark", null, null, DT_WeaponSnark).WithManualClassID(StaticClassIndices.CWeaponSnark);
#else
	public static readonly new ServerClass ServerClass = new ServerClass("WeaponSnark", DT_WeaponSnark).WithManualClassID(StaticClassIndices.CWeaponSnark);
#endif
	public bool JustThrown;
}
#endif
