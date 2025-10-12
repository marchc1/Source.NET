#if CLIENT_DLL || GAME_DLL
using Source.Common;
namespace Game.Shared.HL1;
using FIELD = Source.FIELD<WeaponGlock>;
public class WeaponGlock : BaseHL1MPCombatWeapon
{
	public static readonly
#if CLIENT_DLL
		RecvTable
#else
		SendTable
#endif
		DT_WeaponGlock = new(DT_BaseHL1MPCombatWeapon, []);
#if CLIENT_DLL
	public static readonly new ClientClass ClientClass = new ClientClass("WeaponGlock", null, null, DT_WeaponGlock).WithManualClassID(StaticClassIndices.CWeaponGlock);
#else
	public static readonly new ServerClass ServerClass = new ServerClass("WeaponGlock", DT_WeaponGlock).WithManualClassID(StaticClassIndices.CWeaponGlock);
#endif
	public float InZoom;
}
#endif
