namespace Game.Server.HL2;

using Game.Shared.HL2;

using Source.Common;

using System.Numerics;

using FIELD = Source.FIELD<HL2PlayerLocalData>;

public class HL2PlayerLocalData
{
	public static readonly SendTable DT_HL2Local = new(nameof(DT_HL2Local), [
		SendPropFloat(FIELD.OF(nameof(SuitPower)), 10, PropFlags.Unsigned | PropFlags.RoundUp, 0.0f, 100.0f),
		SendPropInt(FIELD.OF(nameof(Zooming)), 1, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(BitsActiveDevices)), MAX_SUIT_DEVICES, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(SquadMemberCount)), 5, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(SquadMedicCount)), 5, PropFlags.Unsigned),
		SendPropBool(FIELD.OF(nameof(SquadInFollowMode))),
		SendPropBool(FIELD.OF(nameof(WeaponLowered))),
		SendPropEHandle(FIELD.OF(nameof(Ladder))),
		SendPropBool(FIELD.OF(nameof(DisplayReticle))),
	]);

	[NetworkName("m_flSuitPower")]
	public float SuitPower;
	[NetworkName("m_bZooming")]
	public bool Zooming;
	[NetworkName("m_bitsActiveDevices")]
	public int BitsActiveDevices;
	[NetworkName("m_iSquadMemberCount")]
	public int SquadMemberCount;
	[NetworkName("m_iSquadMedicCount")]
	public int SquadMedicCount;
	[NetworkName("m_fSquadInFollowMode")]
	public bool SquadInFollowMode;
	[NetworkName("m_bWeaponLowered")]
	public bool WeaponLowered;
	public EHANDLE AutoAimTargetHandle = new();
	public Vector3 AutoAimPoint;
	[NetworkName("m_bDisplayReticle")]
	public bool DisplayReticle;
	public bool StickyAutoAim;
	public bool AutoAimTarget;
#if HL2_EPISODIC
	public float FlashBattery;
	public Vector3 LocatorOrigin;
#endif
	[NetworkName("m_hLadder")]
	public EHANDLE Ladder = new();
	public LadderMove LadderMove = new();
}
