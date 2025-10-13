#if (CLIENT_DLL || GAME_DLL) && GMOD_DLL
using Source.Common;
namespace Game.Shared.GarrysMod;
using FIELD = Source.FIELD<WeaponFrag>;
public class WeaponFrag : BaseHL2MPCombatWeapon
{
	public static readonly
#if CLIENT_DLL
		RecvTable
#else
		SendTable
#endif
		DT_WeaponFrag = new(DT_BaseHL2MPCombatWeapon, [
#if CLIENT_DLL
			RecvPropBool(FIELD.OF(nameof(DrawbackFinished))),
			RecvPropInt(FIELD.OF(nameof(AttackPaused)))
#else
			SendPropBool(FIELD.OF(nameof(DrawbackFinished))),
			SendPropInt(FIELD.OF(nameof(AttackPaused)))
#endif
		]);
#if CLIENT_DLL
	public static readonly new ClientClass ClientClass = new ClientClass("WeaponFrag", null, null, DT_WeaponFrag).WithManualClassID(StaticClassIndices.CWeaponFrag);
#else
	public static readonly new ServerClass ServerClass = new ServerClass("WeaponFrag", DT_WeaponFrag).WithManualClassID(StaticClassIndices.CWeaponFrag);
#endif
	public bool DrawbackFinished;
	public int AttackPaused;
}
#endif
