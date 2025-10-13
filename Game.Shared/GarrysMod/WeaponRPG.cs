#if (CLIENT_DLL || GAME_DLL) && GMOD_DLL
using Source.Common;

using System.Numerics;
namespace Game.Shared.GarrysMod;
using FIELD = Source.FIELD<WeaponRPG>;
public class WeaponRPG : BaseHL2MPCombatWeapon
{
	public static readonly
#if CLIENT_DLL
		RecvTable
#else
		SendTable
#endif
		DT_WeaponRPG = new(DT_BaseHL2MPCombatWeapon, [
#if CLIENT_DLL
			RecvPropBool(FIELD.OF(nameof(InitialStateUpdate))),
			RecvPropBool(FIELD.OF(nameof(Guiding))),
			RecvPropBool(FIELD.OF(nameof(HideGuiding))),
			RecvPropEHandle(FIELD.OF(nameof(Missile))),
			RecvPropVector(FIELD.OF(nameof(LaserDot))),
#else
			SendPropBool(FIELD.OF(nameof(InitialStateUpdate))),
			SendPropBool(FIELD.OF(nameof(Guiding))),
			SendPropBool(FIELD.OF(nameof(HideGuiding))),
			SendPropEHandle(FIELD.OF(nameof(Missile))),
			SendPropVector(FIELD.OF(nameof(LaserDot)), 0, PropFlags.NoScale),
#endif
		]);
#if CLIENT_DLL
	public static readonly new ClientClass ClientClass = new ClientClass("WeaponRPG", null, null, DT_WeaponRPG).WithManualClassID(StaticClassIndices.CWeaponRPG);
#else
	public static readonly new ServerClass ServerClass = new ServerClass("WeaponRPG", DT_WeaponRPG).WithManualClassID(StaticClassIndices.CWeaponRPG);
#endif
	public bool InitialStateUpdate;
	public bool Guiding;
	public bool HideGuiding;
	public readonly EHANDLE Missile = new();
	public Vector3 LaserDot;
}

public class LaserDot : SharedBaseEntity
{
	public static readonly
#if CLIENT_DLL
		RecvTable
#else
		SendTable
#endif
		DT_LaserDot = new(DT_BaseEntity, []);
#if CLIENT_DLL
	public static readonly new ClientClass ClientClass = new ClientClass("LaserDot", null, null, DT_LaserDot).WithManualClassID(StaticClassIndices.CLaserDot);
#else
	public static readonly new ServerClass ServerClass = new ServerClass("LaserDot", DT_LaserDot).WithManualClassID(StaticClassIndices.CLaserDot);
#endif
}
#endif
