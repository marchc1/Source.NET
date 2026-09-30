using Game.Shared;
namespace Game.Server.HL2;

using Game.Shared.HL2;

using Source.Common;

using System.Numerics;

using FIELD = Source.FIELD<HL2PlayerLocalData>;

public partial class HL2PlayerLocalData {
	public static readonly SendTable DT_HL2Local = new(nameof(DT_HL2Local), [
		SendPropFloat(HL2PlayerLocalData.NetworkVarFields.SuitPower, 10, PropFlags.Unsigned | PropFlags.RoundUp, 0.0f, 100.0f),
		SendPropInt(HL2PlayerLocalData.NetworkVarFields.Zooming, 1, PropFlags.Unsigned),
		SendPropInt(HL2PlayerLocalData.NetworkVarFields.BitsActiveDevices, MAX_SUIT_DEVICES, PropFlags.Unsigned),
		SendPropInt(HL2PlayerLocalData.NetworkVarFields.SquadMemberCount, 5, PropFlags.Unsigned),
		SendPropInt(HL2PlayerLocalData.NetworkVarFields.SquadMedicCount, 5, PropFlags.Unsigned),
		SendPropBool(HL2PlayerLocalData.NetworkVarFields.SquadInFollowMode),
		SendPropBool(HL2PlayerLocalData.NetworkVarFields.WeaponLowered),
		SendPropEHandle(HL2PlayerLocalData.NetworkVarFields.Ladder),
		SendPropBool(HL2PlayerLocalData.NetworkVarFields.DisplayReticle),
	]);

	[NetworkName("m_flSuitPower")]
	[NetworkVar] public partial float SuitPower { get; set; }
	[NetworkName("m_bZooming")]
	[NetworkVar] public partial bool Zooming { get; set; }
	[NetworkName("m_bitsActiveDevices")]
	[NetworkVar] public partial int BitsActiveDevices { get; set; }
	[NetworkName("m_iSquadMemberCount")]
	[NetworkVar] public partial int SquadMemberCount { get; set; }
	[NetworkName("m_iSquadMedicCount")]
	[NetworkVar] public partial int SquadMedicCount { get; set; }
	[NetworkName("m_fSquadInFollowMode")]
	[NetworkVar] public partial bool SquadInFollowMode { get; set; }
	[NetworkName("m_bWeaponLowered")]
	[NetworkVar] public partial bool WeaponLowered { get; set; }
	public EHANDLE AutoAimTargetHandle = new();
	public Vector3 AutoAimPoint;
	[NetworkName("m_bDisplayReticle")]
	[NetworkVar] public partial bool DisplayReticle { get; set; }
	public bool StickyAutoAim;
	public bool AutoAimTarget;
	[NetworkName("m_hLadder")]
	[NetworkVar] public partial NetworkHandle<BaseEntity> Ladder { get; }
	public LadderMove LadderMove = new();
}
