#if (CLIENT_DLL || GAME_DLL) && GMOD_DLL
using Source.Common;
namespace Game.Shared.GarrysMod;
using FIELD = Source.FIELD<WeaponStunStick>;

[LinkEntityToClass("weapon_stunstick")]
[PrecacheWeaponRegister("weapon_stunstick")]
[NetworkName("CWeaponStunStick")]
public partial class WeaponStunStick : BaseHL2MPBludgeonWeapon
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
		SendPropBool(NetworkVarFields.Active)
#endif
		]);
#if CLIENT_DLL
	public static readonly new ClientClass ClientClass = new ClientClass(null, null, DT_WeaponStunStick);
	public static readonly new DataMap PredMap = new([], typeof(WeaponStunStick), BaseHL2MPBludgeonWeapon.PredMap); public override DataMap? GetPredDescMap() => PredMap;
#else
	public static readonly new ServerClass ServerClass = new ServerClass(DT_WeaponStunStick);
#endif
	[NetworkName("m_bActive")]
	[NetworkVar] public partial bool Active { get; set; }
	public override float GetFireRate() => 0.2f;
}
#endif
