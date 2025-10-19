#if (CLIENT_DLL || GAME_DLL) && GMOD_DLL
using Source.Common;
namespace Game.Shared.GarrysMod;
using FIELD = Source.FIELD<Weapon357>;
public class Weapon357 : BaseHL2MPCombatWeapon
{
	public static readonly
#if CLIENT_DLL
		RecvTable
#else
		SendTable
#endif
		DT_Weapon357 = new(DT_BaseHL2MPCombatWeapon, [
#if CLIENT_DLL

#else

#endif
		]);
#if CLIENT_DLL
	public static readonly new ClientClass ClientClass = new ClientClass("Weapon357", null, null, DT_Weapon357).WithManualClassID(StaticClassIndices.CWeapon357);
#else
	public static readonly new ServerClass ServerClass = new ServerClass("Weapon357", DT_Weapon357).WithManualClassID(StaticClassIndices.CWeapon357);
#endif
}
#endif
