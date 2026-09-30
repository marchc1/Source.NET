#if (CLIENT_DLL || GAME_DLL) && GMOD_DLL
using Source.Common;
namespace Game.Shared.GarrysMod;
using FIELD = Source.FIELD<WeaponFrag>;

[LinkEntityToClass("weapon_frag")]
[PrecacheWeaponRegister("weapon_frag")]
[NetworkName("CWeaponFrag")]
public partial class WeaponFrag : BaseHL2MPCombatWeapon
{
	public static readonly
#if CLIENT_DLL
		RecvTable
#else
		SendTable
#endif
		DT_WeaponFrag = new(DT_BaseHL2MPCombatWeapon, [
#if CLIENT_DLL
			RecvPropBool(FIELD.OF(nameof(Redraw))),
			RecvPropBool(FIELD.OF(nameof(DrawbackFinished))),
			RecvPropInt(FIELD.OF(nameof(AttackPaused)))
#else
			SendPropBool(NetworkVarFields.Redraw),
			SendPropBool(NetworkVarFields.DrawbackFinished),
			SendPropInt(NetworkVarFields.AttackPaused, 4)
#endif
		]);
#if CLIENT_DLL
	public static readonly new ClientClass ClientClass = new ClientClass(null, null, DT_WeaponFrag);
	public static readonly new DataMap PredMap = new([], typeof(WeaponFrag), BaseHL2MPCombatWeapon.PredMap); public override DataMap? GetPredDescMap() => PredMap;

#else
	public static readonly new ServerClass ServerClass = new ServerClass(DT_WeaponFrag);
#endif
	[NetworkName("m_bRedraw")]
	[NetworkVar] public partial bool Redraw { get; set; }
	[NetworkName("m_fDrawbackFinished")]
	[NetworkVar] public partial bool DrawbackFinished { get; set; }
	[NetworkName("m_AttackPaused")]
	[NetworkVar] public partial int AttackPaused { get; set; }
}
#endif
