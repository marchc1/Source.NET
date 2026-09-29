namespace Game.Client.HL2;

using Game.Shared.HL2;

using Source.Common;

using System.Numerics;

using DEFINE = Source.DEFINE<C_HL2PlayerLocalData>;
using FIELD = Source.FIELD<C_HL2PlayerLocalData>;

public class C_HL2PlayerLocalData {
	public static readonly RecvTable DT_HL2Local = new(nameof(DT_HL2Local), [
		RecvPropFloat(FIELD.OF(nameof(SuitPower))),
		RecvPropInt(FIELD.OF(nameof(Zooming))),
		RecvPropInt(FIELD.OF(nameof(BitsActiveDevices))),
		RecvPropInt(FIELD.OF(nameof(SquadMemberCount))),
		RecvPropInt(FIELD.OF(nameof(SquadMedicCount))),
		RecvPropBool(FIELD.OF(nameof(SquadInFollowMode))),
		RecvPropBool(FIELD.OF(nameof(WeaponLowered))),
		RecvPropEHandle(FIELD.OF(nameof(Ladder))),
		RecvPropBool(FIELD.OF(nameof(DisplayReticle))),
	]);

	public static readonly DataMap PredMap = new(typeof(C_HL2PlayerLocalData), [
		DEFINE.PRED_FIELD( nameof(Ladder), FieldType.EHandle, FieldTypeDescFlags.InSendTable ),
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
	[NetworkName("m_hLadder")]
	public EHANDLE Ladder = new();
	public LadderMove LadderMove = new();
}
