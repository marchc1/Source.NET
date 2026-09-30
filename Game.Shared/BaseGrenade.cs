#if CLIENT_DLL || GAME_DLL

#if CLIENT_DLL
global using C_BaseGrenade = Game.Shared.BaseGrenade;
#endif

using Source.Common;

namespace Game.Shared;

using FIELD = Source.FIELD<BaseGrenade>;
[NetworkName("CBaseGrenade")]
public partial class BaseGrenade : BaseProjectile
{
	public static readonly
#if CLIENT_DLL
		RecvTable
#else
		SendTable
#endif
		DT_BaseGrenade = new(DT_BaseAnimating, [
#if CLIENT_DLL
			RecvPropFloat(FIELD.OF(nameof(Damage))),
			RecvPropFloat(FIELD.OF(nameof(DmgRadius))),
			RecvPropBool(FIELD.OF(nameof(IsLive))),
			RecvPropEHandle(FIELD.OF(nameof(Thrower))),
			RecvPropVector(FIELD.OF(nameof(Velocity))),
			RecvPropInt(FIELD.OF(nameof(Flags)))
#else
			SendPropFloat(NetworkVarFields.Damage, 10, PropFlags.RoundDown, 0, 256),
			SendPropFloat(NetworkVarFields.DmgRadius, 10, PropFlags.RoundDown, 0, 1024),
			SendPropBool(NetworkVarFields.IsLive),
			SendPropEHandle(BaseGrenade.NetworkVarFields.Thrower),
			SendPropVector(FIELD.OF(nameof(Velocity)), 0, PropFlags.NoScale),
			SendPropInt(NetworkVarFields.Flags, 16, PropFlags.Unsigned)
#endif
		]);
#if CLIENT_DLL
	public static readonly new ClientClass ClientClass = new ClientClass(null, null, DT_BaseGrenade);
#else
	public static readonly new ServerClass ServerClass = new ServerClass(DT_BaseGrenade);
#endif
	[NetworkName("m_flDamage")]
	[NetworkVar] public partial float Damage { get; set; }
	[NetworkName("m_DmgRadius")]
	[NetworkVar] public partial float DmgRadius { get; set; }
	[NetworkName("m_bIsLive")]
	[NetworkVar] public partial bool IsLive { get; set; }
	[NetworkName("m_hThrower")]
	[NetworkVar] public partial NetworkHandle<BaseEntity> Thrower { get; }
	[NetworkName("m_fFlags")]
	[NetworkVar] public partial int Flags { get; set; }
}
#endif
