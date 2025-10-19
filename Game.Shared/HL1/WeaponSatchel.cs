#if CLIENT_DLL || GAME_DLL
using Source.Common;
namespace Game.Shared.HL1;
using FIELD = Source.FIELD<WeaponSatchel>;
public class WeaponSatchel : BaseHL1CombatWeapon
{
	public static readonly
#if CLIENT_DLL
		RecvTable
#else
		SendTable
#endif
		DT_WeaponSatchel = new(DT_BaseHL1CombatWeapon, [
#if CLIENT_DLL
			RecvPropInt(FIELD.OF(nameof(RadioViewIndex))),
			RecvPropInt(FIELD.OF(nameof(RadioWorldIndex))),
			RecvPropInt(FIELD.OF(nameof(SatchelViewIndex))),
			RecvPropInt(FIELD.OF(nameof(SatchelWorldIndex))),
			RecvPropInt(FIELD.OF(nameof(ChargeReady))),
#else
			SendPropInt(FIELD.OF(nameof(RadioViewIndex)), 32),
			SendPropInt(FIELD.OF(nameof(RadioWorldIndex)), 32),
			SendPropInt(FIELD.OF(nameof(SatchelViewIndex)), 32),
			SendPropInt(FIELD.OF(nameof(SatchelWorldIndex)), 32),
			SendPropInt(FIELD.OF(nameof(ChargeReady)), 32),
#endif
		]);
#if CLIENT_DLL
	public static readonly new ClientClass ClientClass = new ClientClass("WeaponSatchel", null, null, DT_WeaponSatchel).WithManualClassID(StaticClassIndices.CWeaponSatchel);
#else
	public static readonly new ServerClass ServerClass = new ServerClass("WeaponSatchel", DT_WeaponSatchel).WithManualClassID(StaticClassIndices.CWeaponSatchel);
#endif
	public int RadioViewIndex;
	public float RadioWorldIndex;
	public float SatchelViewIndex;
	public float SatchelWorldIndex;
	public float ChargeReady;
}
#endif
