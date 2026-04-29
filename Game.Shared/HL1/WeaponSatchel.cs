#if CLIENT_DLL || GAME_DLL
using Source.Common;
namespace Game.Shared.HL1;
using FIELD = Source.FIELD<WeaponSatchel>;
public class WeaponSatchel : BaseHL1MPCombatWeapon
{
	public static readonly
#if CLIENT_DLL
		RecvTable
#else
		SendTable
#endif
		DT_WeaponSatchel = new(DT_BaseHL1MPCombatWeapon, [
#if CLIENT_DLL
			RecvPropInt(FIELD.OF(nameof(RadioViewIndex))),
			RecvPropInt(FIELD.OF(nameof(RadioWorldIndex))),
			RecvPropInt(FIELD.OF(nameof(SatchelViewIndex))),
			RecvPropInt(FIELD.OF(nameof(SatchelWorldIndex))),
			RecvPropInt(FIELD.OF(nameof(ChargeReady))),
#else
			SendPropInt(FIELD.OF(nameof(RadioViewIndex)), 14),
			SendPropInt(FIELD.OF(nameof(RadioWorldIndex)), 14),
			SendPropInt(FIELD.OF(nameof(SatchelViewIndex)), 14),
			SendPropInt(FIELD.OF(nameof(SatchelWorldIndex)), 14),
			SendPropInt(FIELD.OF(nameof(ChargeReady)), 3, PropFlags.Unsigned),
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
