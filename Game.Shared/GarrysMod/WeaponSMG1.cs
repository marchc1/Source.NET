#if (CLIENT_DLL || GAME_DLL) && GMOD_DLL
using Source.Common;
namespace Game.Shared.GarrysMod;
using FIELD = Source.FIELD<WeaponSMG1>;

[LinkEntityToClass("weapon_smg1")]
public class WeaponSMG1 : HL2MPMachineGun
{
	public static readonly
#if CLIENT_DLL
		RecvTable
#else
		SendTable
#endif
		DT_WeaponSMG1 = new(DT_HL2MPMachineGun, [
#if CLIENT_DLL

#else

#endif
		]);
#if CLIENT_DLL
	public static readonly new ClientClass ClientClass = new ClientClass("WeaponSMG1", null, null, DT_WeaponSMG1).WithManualClassID(StaticClassIndices.CWeaponSMG1);
#else
	public static readonly new ServerClass ServerClass = new ServerClass("WeaponSMG1", DT_WeaponSMG1).WithManualClassID(StaticClassIndices.CWeaponSMG1);
#endif
}
#endif
