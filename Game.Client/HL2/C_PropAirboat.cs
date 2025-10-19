using Game.Shared;

using Source.Common;

using System.Numerics;

namespace Game.Client.HL2;
using FIELD = Source.FIELD<C_PropAirboat>;

public class C_PropAirboat : C_PropVehicleDriveable
{
	public static readonly RecvTable DT_PropAirboat = new(DT_PropVehicleDriveable, [
		RecvPropBool(FIELD.OF(nameof(HeadlightIsOn))),
		RecvPropInt(FIELD.OF(nameof(AmmoCount))),
		RecvPropInt(FIELD.OF(nameof(ExactWaterLevel))),
		RecvPropInt(FIELD.OF(nameof(WaterLevel))),
		RecvPropVector(FIELD.OF(nameof(PhysVelocity))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass("PropAirboat", DT_PropAirboat).WithManualClassID(StaticClassIndices.CPropAirboat);

	public bool HeadlightIsOn;
	public int AmmoCount;
	public int ExactWaterLevel;
	public Vector3 PhysVelocity;
}
