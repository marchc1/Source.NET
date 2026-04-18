namespace Game.Server.HL2;

using Game.Shared.HL2;

using Source.Common;

using System.Numerics;

using FIELD = Source.FIELD<HL2PlayerLocalData>;

public class HL2PlayerLocalData {
	public static readonly SendTable DT_HL2Local = new([
		SendPropFloat(FIELD.OF(nameof(SuitPower)), 10, PropFlags.Unsigned | PropFlags.RoundUp, 0.0f, 100.0f),
		SendPropInt(FIELD.OF(nameof(Zooming)), 1, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(BitsActiveDevices)), MAX_SUIT_DEVICES, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(SquadMemberCount))),
		SendPropInt(FIELD.OF(nameof(SquadMedicCount))),
		SendPropBool(FIELD.OF(nameof(SquadInFollowMode))),
		SendPropBool(FIELD.OF(nameof(WeaponLowered))),
		SendPropEHandle(FIELD.OF(nameof(AutoAimTargetHandle))),
		SendPropVector(FIELD.OF(nameof(AutoAimPoint))),
		SendPropEHandle(FIELD.OF(nameof(Ladder))),
		SendPropBool(FIELD.OF(nameof(DisplayReticle))),
		SendPropBool(FIELD.OF(nameof(StickyAutoAim))),
		SendPropBool(FIELD.OF(nameof(AutoAimTarget))),
	]); public static readonly ServerClass SC_Local = new ServerClass("HL2Local", DT_HL2Local);

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
