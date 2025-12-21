#if (CLIENT_DLL || GAME_DLL) && GMOD_DLL
using Source.Common;
namespace Game.Shared.GarrysMod;
using FIELD = Source.FIELD<WeaponCrowbar>;

[LinkEntityToClass("weapon_crowbar")]
public class WeaponCrowbar : BaseHL2MPBludgeonWeapon
{
	public static readonly
#if CLIENT_DLL
		RecvTable
#else
		SendTable
#endif
		DT_WeaponCrowbar = new(DT_BaseHL2MPBludgeonWeapon, [
#if CLIENT_DLL

#else

#endif
		]);
#if CLIENT_DLL
	public static readonly new ClientClass ClientClass = new ClientClass("WeaponCrowbar", null, null, DT_WeaponCrowbar).WithManualClassID(StaticClassIndices.CWeaponCrowbar);
#else
	public static readonly new ServerClass ServerClass = new ServerClass("WeaponCrowbar", DT_WeaponCrowbar).WithManualClassID(StaticClassIndices.CWeaponCrowbar);
#endif
}
#endif
