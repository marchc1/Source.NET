#if CLIENT_DLL || GAME_DLL
using Source.Common;
namespace Game.Shared.HL1;
using FIELD = Source.FIELD<WeaponHgun>;
[NetworkName("CWeaponHgun")]
public partial class WeaponHgun : BaseHL1MPCombatWeapon
{
	public static readonly
#if CLIENT_DLL
		RecvTable
#else
		SendTable
#endif
		DT_WeaponHgun = new(DT_BaseHL1MPCombatWeapon, [
#if CLIENT_DLL
			RecvPropFloat(FIELD.OF(nameof(RechargeTime))),
			RecvPropInt(FIELD.OF(nameof(FirePhase))),
#else
			SendPropFloat(NetworkVarFields.RechargeTime, 0, PropFlags.NoScale),
			SendPropInt(NetworkVarFields.FirePhase, 4, PropFlags.Unsigned),
#endif
		]);
#if CLIENT_DLL
	public static readonly new ClientClass ClientClass = new ClientClass(null, null, DT_WeaponHgun);
#else
	public static readonly new ServerClass ServerClass = new ServerClass(DT_WeaponHgun);
#endif
	[NetworkName("m_flRechargeTime")]
	[NetworkVar] public partial TimeUnit_t RechargeTime { get; set; }
	[NetworkName("m_iFirePhase")]
	[NetworkVar] public partial int FirePhase { get; set; }
}
#endif
