#if CLIENT_DLL || GAME_DLL
using Source.Common;
namespace Game.Shared.HL1;
using FIELD = Source.FIELD<WeaponEgon>;
public class WeaponEgon : BaseHL1MPCombatWeapon
{
	public static readonly
#if CLIENT_DLL
		RecvTable
#else
		SendTable
#endif
		DT_WeaponEgon = new(DT_BaseHL1MPCombatWeapon, []);
#if CLIENT_DLL
	public static readonly new ClientClass ClientClass = new ClientClass("WeaponEgon", null, null, DT_WeaponEgon).WithManualClassID(StaticClassIndices.CWeaponEgon);
#else
	public static readonly new ServerClass ServerClass = new ServerClass("WeaponEgon", DT_WeaponEgon).WithManualClassID(StaticClassIndices.CWeaponEgon);
#endif
	public float InZoom;
}
#endif
