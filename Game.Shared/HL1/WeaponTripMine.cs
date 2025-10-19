#if CLIENT_DLL || GAME_DLL
using Source.Common;
namespace Game.Shared.HL1;
using FIELD = Source.FIELD<WeaponTripMine>;
public class WeaponTripMine : BaseHL1CombatWeapon
{
	public static readonly
#if CLIENT_DLL
		RecvTable
#else
		SendTable
#endif
		DT_WeaponTripMine = new(DT_BaseHL1CombatWeapon, [
#if CLIENT_DLL
			RecvPropFloat(FIELD.OF(nameof(GroundIndex))),
			RecvPropFloat(FIELD.OF(nameof(PickedUpIndex))),
#else
			SendPropFloat(FIELD.OF(nameof(GroundIndex)), 0, PropFlags.NoScale),
			SendPropFloat(FIELD.OF(nameof(PickedUpIndex)), 0, PropFlags.NoScale),
#endif
		]);
#if CLIENT_DLL
	public static readonly new ClientClass ClientClass = new ClientClass("WeaponTripMine", null, null, DT_WeaponTripMine).WithManualClassID(StaticClassIndices.CWeaponTripMine);
#else
	public static readonly new ServerClass ServerClass = new ServerClass("WeaponTripMine", DT_WeaponTripMine).WithManualClassID(StaticClassIndices.CWeaponTripMine);
#endif
	public float GroundIndex;
	public float PickedUpIndex;
}
#endif
