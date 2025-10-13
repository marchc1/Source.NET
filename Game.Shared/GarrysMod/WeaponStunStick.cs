#if (CLIENT_DLL || GAME_DLL) && GMOD_DLL
using Source.Common;
namespace Game.Shared.GarrysMod;
using FIELD = Source.FIELD<WeaponStunStick>;
public class WeaponStunStick : BaseHL2MPBludgeonWeapon
{
	public static readonly
#if CLIENT_DLL
		RecvTable
#else
		SendTable
#endif
		DT_WeaponStunStick = new(DT_BaseHL2MPBludgeonWeapon, [
#if CLIENT_DLL
		RecvPropBool(FIELD.OF(nameof(Active)))
#else
		SendPropBool(FIELD.OF(nameof(Active)))
#endif
		]);
#if CLIENT_DLL
	public static readonly new ClientClass ClientClass = new ClientClass("WeaponStunStick", null, null, DT_WeaponStunStick).WithManualClassID(StaticClassIndices.CWeaponStunStick);
#else
	public static readonly new ServerClass ServerClass = new ServerClass("WeaponStunStick", DT_WeaponStunStick).WithManualClassID(StaticClassIndices.CWeaponStunStick);
#endif
	public bool Active;
}
#endif
