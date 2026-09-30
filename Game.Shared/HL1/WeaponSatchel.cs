#if CLIENT_DLL || GAME_DLL
using Source.Common;
namespace Game.Shared.HL1;
using FIELD = Source.FIELD<WeaponSatchel>;
[NetworkName("CWeaponSatchel")]
public partial class WeaponSatchel : BaseHL1MPCombatWeapon
{
	public static readonly
#if CLIENT_DLL
		RecvTable
#else
		SendTable
#endif
		DT_WeaponSatchel = new(DT_BaseHL1MPCombatWeapon, [
#if CLIENT_DLL
			RecvPropInt(FIELD.OF(nameof(RadioViewIndex))),
			RecvPropInt(FIELD.OF(nameof(RadioWorldIndex))),
			RecvPropInt(FIELD.OF(nameof(SatchelViewIndex))),
			RecvPropInt(FIELD.OF(nameof(SatchelWorldIndex))),
			RecvPropInt(FIELD.OF(nameof(ChargeReady))),
#else
			SendPropInt(NetworkVarFields.RadioViewIndex, 14),
			SendPropInt(NetworkVarFields.RadioWorldIndex, 14),
			SendPropInt(NetworkVarFields.SatchelViewIndex, 14),
			SendPropInt(NetworkVarFields.SatchelWorldIndex, 14),
			SendPropInt(NetworkVarFields.ChargeReady, 3, PropFlags.Unsigned),
#endif
		]);
#if CLIENT_DLL
	public static readonly new ClientClass ClientClass = new ClientClass(null, null, DT_WeaponSatchel);
#else
	public static readonly new ServerClass ServerClass = new ServerClass(DT_WeaponSatchel);
#endif
	[NetworkName("m_iRadioViewIndex")]
	[NetworkVar] public partial int RadioViewIndex { get; set; }
	[NetworkName("m_iRadioWorldIndex")]
	[NetworkVar] public partial float RadioWorldIndex { get; set; }
	[NetworkName("m_iSatchelViewIndex")]
	[NetworkVar] public partial float SatchelViewIndex { get; set; }
	[NetworkName("m_iSatchelWorldIndex")]
	[NetworkVar] public partial float SatchelWorldIndex { get; set; }
	[NetworkName("m_iChargeReady")]
	[NetworkVar] public partial float ChargeReady { get; set; }
}
#endif
