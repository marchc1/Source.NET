namespace Game.Client.HL2;

using Game.Shared.HL2;

using Source.Common;

using System.Numerics;

using FIELD = Source.FIELD<C_HL2PlayerLocalData>;

public class C_HL2PlayerLocalData {
	public static readonly RecvTable DT_HL2Local = new([
		RecvPropFloat(FIELD.OF(nameof(SuitPower))),
		RecvPropInt(FIELD.OF(nameof(Zooming))),
		RecvPropInt(FIELD.OF(nameof(BitsActiveDevices))),
		RecvPropInt(FIELD.OF(nameof(SquadMemberCount))),
		RecvPropInt(FIELD.OF(nameof(SquadMedicCount))),
		RecvPropBool(FIELD.OF(nameof(SquadInFollowMode))),
		RecvPropBool(FIELD.OF(nameof(WeaponLowered))),
		RecvPropEHandle(FIELD.OF(nameof(AutoAimTargetHandle))),
		RecvPropVector(FIELD.OF(nameof(AutoAimPoint))),
		RecvPropEHandle(FIELD.OF(nameof(Ladder))),
		RecvPropBool(FIELD.OF(nameof(DisplayReticle))),
		RecvPropBool(FIELD.OF(nameof(StickyAutoAim))),
		RecvPropBool(FIELD.OF(nameof(AutoAimTarget))),
	]); public static readonly ClientClass CC_Local = new ClientClass("HL2Local", null, null, DT_HL2Local);

	public float SuitPower;
	public bool Zooming;
	public int BitsActiveDevices;
	public int SquadMemberCount;
	public int SquadMedicCount;
	public bool SquadInFollowMode;
	public bool WeaponLowered;
	public EHANDLE AutoAimTargetHandle = new();
	public Vector3 AutoAimPoint;
	public bool DisplayReticle;
	public bool StickyAutoAim;
	public bool AutoAimTarget;
	public EHANDLE Ladder = new();
	public LadderMove LadderMove = new();
}
