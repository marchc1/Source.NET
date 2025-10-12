#if CLIENT_DLL || GAME_DLL
using Source.Common;

using System.Drawing;
namespace Game.Shared.HL1;
using FIELD_RPG = Source.FIELD<WeaponRPG_HL1>;
using FIELD_LASER = Source.FIELD<LaserDot_HL1>;
public class WeaponRPG_HL1 : BaseHL1MPCombatWeapon
{
	public static readonly
#if CLIENT_DLL
		RecvTable
#else
		SendTable
#endif
		DT_WeaponRPG_HL1 = new(DT_BaseHL1MPCombatWeapon, [
#if CLIENT_DLL
			RecvPropBool(FIELD_RPG.OF(nameof(InitialStateUpdate))),
			RecvPropBool(FIELD_RPG.OF(nameof(Guiding))),
			RecvPropBool(FIELD_RPG.OF(nameof(LaserDotSuspended)))
#else
			SendPropBool(FIELD_RPG.OF(nameof(InitialStateUpdate))),
			SendPropBool(FIELD_RPG.OF(nameof(Guiding))),
			SendPropBool(FIELD_RPG.OF(nameof(LaserDotSuspended)))
#endif
		]);
#if CLIENT_DLL
	public static readonly new ClientClass ClientClass = new ClientClass("WeaponRPG_HL1", null, null, DT_WeaponRPG_HL1).WithManualClassID(StaticClassIndices.CWeaponRPG_HL1);
#else
	public static readonly new ServerClass ServerClass = new ServerClass("WeaponRPG_HL1", DT_WeaponRPG_HL1).WithManualClassID(StaticClassIndices.CWeaponRPG_HL1);
#endif
	public bool InitialStateUpdate;
	public bool Guiding;
	public bool LaserDotSuspended;
}

public class LaserDot_HL1 : SharedBaseEntity
{
	public static readonly
#if CLIENT_DLL
		RecvTable
#else
		SendTable
#endif
		DT_LaserDot_HL1 = new(DT_BaseEntity, [
#if CLIENT_DLL
			RecvPropBool(FIELD_LASER.OF(nameof(IsOn))),
#else
			SendPropBool(FIELD_LASER.OF(nameof(IsOn))),
#endif
		]);
#if CLIENT_DLL
	public static readonly new ClientClass ClientClass = new ClientClass("LaserDot_HL1", null, null, DT_LaserDot_HL1).WithManualClassID(StaticClassIndices.CLaserDot_HL1);
#else
	public static readonly new ServerClass ServerClass = new ServerClass("LaserDot_HL1", DT_LaserDot_HL1).WithManualClassID(StaticClassIndices.CLaserDot_HL1);
#endif
	public bool IsOn;
}
#endif
