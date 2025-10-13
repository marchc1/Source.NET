#if (CLIENT_DLL || GAME_DLL) && GMOD_DLL
using Source.Common;
namespace Game.Shared.GarrysMod;
using FIELD = Source.FIELD<HL2MPMachineGun>;
public class HL2MPMachineGun : WeaponHL2MPBase
{
	public static readonly
#if CLIENT_DLL
		RecvTable
#else
		SendTable
#endif
		DT_HL2MPMachineGun = new(DT_WeaponHL2MPBase, [
#if CLIENT_DLL
		RecvPropInt(FIELD.OF(nameof(ShotsFired))),
#else
		SendPropInt(FIELD.OF(nameof(ShotsFired))),
#endif
		]);
#if CLIENT_DLL
	public static readonly new ClientClass ClientClass = new ClientClass("HL2MPMachineGun", null, null, DT_HL2MPMachineGun).WithManualClassID(StaticClassIndices.CHL2MPMachineGun);
#else
	public static readonly new ServerClass ServerClass = new ServerClass("HL2MPMachineGun", DT_HL2MPMachineGun).WithManualClassID(StaticClassIndices.CHL2MPMachineGun);
#endif
	public int ShotsFired;
}
#endif
