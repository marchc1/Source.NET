#if CLIENT_DLL || GAME_DLL
using Source.Common;
namespace Game.Shared.HL1;
using FIELD = Source.FIELD<WeaponCrowbar_HL1>;
public class WeaponCrowbar_HL1 : BaseHL1MPCombatWeapon
{
	public static readonly
#if CLIENT_DLL
		RecvTable
#else
		SendTable
#endif
		DT_WeaponCrowbar_HL1 = new(DT_BaseHL1MPCombatWeapon, []);
#if CLIENT_DLL
	public static readonly new ClientClass ClientClass = new ClientClass("WeaponCrowbar_HL1", null, null, DT_WeaponCrowbar_HL1).WithManualClassID(StaticClassIndices.CWeaponCrowbar_HL1);
#else
	public static readonly new ServerClass ServerClass = new ServerClass("WeaponCrowbar_HL1", DT_WeaponCrowbar_HL1).WithManualClassID(StaticClassIndices.CWeaponCrowbar_HL1);
#endif
	public float InZoom;
}
#endif
