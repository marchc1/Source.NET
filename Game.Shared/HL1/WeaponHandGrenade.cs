#if CLIENT_DLL || GAME_DLL
using Source.Common;
namespace Game.Shared.HL1;
using FIELD = Source.FIELD<WeaponHandGrenade>;
[NetworkName("CWeaponHandGrenade")]
public partial class WeaponHandGrenade : BaseHL1MPCombatWeapon
{
	public static readonly
#if CLIENT_DLL
		RecvTable
#else
		SendTable
#endif
		DT_WeaponHandGrenade = new(DT_BaseHL1MPCombatWeapon, [
#if CLIENT_DLL
			RecvPropFloat(FIELD.OF(nameof(StartThrow))),
			RecvPropFloat(FIELD.OF(nameof(ReleaseThrow))),
#else
			SendPropFloat(NetworkVarFields.StartThrow, 0, PropFlags.NoScale),
			SendPropFloat(NetworkVarFields.ReleaseThrow, 0, PropFlags.NoScale),
#endif
		]);
#if CLIENT_DLL
	public static readonly new ClientClass ClientClass = new ClientClass(null, null, DT_WeaponHandGrenade);
#else
	public static readonly new ServerClass ServerClass = new ServerClass(DT_WeaponHandGrenade);
#endif
	[NetworkName("m_flStartThrow")]
	[NetworkVar] public partial TimeUnit_t StartThrow { get; set; }
	[NetworkName("m_flReleaseThrow")]
	[NetworkVar] public partial TimeUnit_t ReleaseThrow { get; set; }
}
#endif
