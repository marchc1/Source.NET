#if (CLIENT_DLL || GAME_DLL) && GMOD_DLL
using Source.Common;
namespace Game.Shared.GarrysMod;
using FIELD = Source.FIELD<BaseHL2MPBludgeonWeapon>;
public class BaseHL2MPBludgeonWeapon : BaseHL2MPCombatWeapon
{
	public static readonly
#if CLIENT_DLL
		RecvTable
#else
		SendTable
#endif
		DT_BaseHL2MPBludgeonWeapon = new(DT_BaseHL2MPCombatWeapon, [
#if CLIENT_DLL

#else

#endif
		]);
#if CLIENT_DLL
	public static readonly new ClientClass ClientClass = new ClientClass("BaseHL2MPBludgeonWeapon", null, null, DT_BaseHL2MPBludgeonWeapon).WithManualClassID(StaticClassIndices.CBaseHL2MPBludgeonWeapon);
#else
	public static readonly new ServerClass ServerClass = new ServerClass("BaseHL2MPBludgeonWeapon", DT_BaseHL2MPBludgeonWeapon).WithManualClassID(StaticClassIndices.CBaseHL2MPBludgeonWeapon);
#endif
}
#endif
