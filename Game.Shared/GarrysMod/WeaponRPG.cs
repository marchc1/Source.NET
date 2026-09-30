#if (CLIENT_DLL || GAME_DLL) && GMOD_DLL
using Source.Common;

using System.Numerics;
namespace Game.Shared.GarrysMod;

using FIELD = Source.FIELD<WeaponRPG>;

[LinkEntityToClass("weapon_rpg")]
[PrecacheWeaponRegister("weapon_rpg")]
[NetworkName("CWeaponRPG")]
public partial class WeaponRPG : BaseHL2MPCombatWeapon
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
			// todo: RecvProxy_MissileDied
			RecvPropEHandle(FIELD.OF(nameof(Missile))),
			RecvPropVector(FIELD.OF(nameof(LaserDot))),
#else
			SendPropBool(NetworkVarFields.InitialStateUpdate),
			SendPropBool(NetworkVarFields.Guiding),
			SendPropBool(NetworkVarFields.HideGuiding),
			SendPropEHandle(WeaponRPG.NetworkVarFields.Missile),
			SendPropVector(NetworkVarFields.LaserDot, 0, PropFlags.NoScale),
#endif
		]);
#if CLIENT_DLL
	public static readonly new ClientClass ClientClass = new ClientClass(null, null, DT_WeaponRPG);
	public static readonly new DataMap PredMap = new([], typeof(WeaponRPG), BaseHL2MPCombatWeapon.PredMap); public override DataMap? GetPredDescMap() => PredMap;
#else
	public static readonly new ServerClass ServerClass = new ServerClass(DT_WeaponRPG);
#endif
	[NetworkName("m_bInitialStateUpdate")]
	[NetworkVar] public partial bool InitialStateUpdate { get; set; }
	[NetworkName("m_bGuiding")]
	[NetworkVar] public partial bool Guiding { get; set; }
	[NetworkName("m_bHideGuiding")]
	[NetworkVar] public partial bool HideGuiding { get; set; }
	[NetworkName("m_hMissile")]
	[NetworkVar] public partial NetworkHandle<BaseEntity> Missile { get; }
	[NetworkName("m_vecLaserDot")]
	[NetworkVar] public partial Vector3 LaserDot { get; set; }
	public override float GetFireRate() => 1f;
}

[LinkEntityToClass("env_laserdot")]
[NetworkName("CLaserDot")]
public class LaserDot : BaseEntity
{
	public static readonly
#if CLIENT_DLL
		RecvTable
#else
		SendTable
#endif
		DT_LaserDot = new(DT_BaseEntity, []);
#if CLIENT_DLL
	public static readonly new ClientClass ClientClass = new ClientClass(null, null, DT_LaserDot);
#else
	public static readonly new ServerClass ServerClass = new ServerClass(DT_LaserDot);
#endif
}
#endif
