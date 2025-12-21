#if (CLIENT_DLL || GAME_DLL) && GMOD_DLL
using Source.Common;
namespace Game.Shared.GarrysMod;
using FIELD = Source.FIELD<WeaponBugBait>;

[LinkEntityToClass("weapon_bugbait")]
public class WeaponBugBait : BaseHL2MPCombatWeapon
{
	public static readonly
#if CLIENT_DLL
		RecvTable
#else
		SendTable
#endif
		DT_WeaponBugBait = new(DT_BaseHL2MPCombatWeapon, [
#if CLIENT_DLL
			RecvPropBool(FIELD.OF(nameof(Redraw))),
			RecvPropBool(FIELD.OF(nameof(DrawbackFinished)))
#else
			SendPropBool(FIELD.OF(nameof(Redraw))),
			SendPropBool(FIELD.OF(nameof(DrawbackFinished)))
#endif
		]);
#if CLIENT_DLL
	public static readonly new ClientClass ClientClass = new ClientClass("WeaponBugBait", null, null, DT_WeaponBugBait).WithManualClassID(StaticClassIndices.CWeaponBugBait);
#else
	public static readonly new ServerClass ServerClass = new ServerClass("WeaponBugBait", DT_WeaponBugBait).WithManualClassID(StaticClassIndices.CWeaponBugBait);
#endif
	public bool Redraw;
	public bool DrawbackFinished;
}
#endif
