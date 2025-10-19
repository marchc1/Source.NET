using Game.Server;
using Game.Shared;

using Source.Common;

using System.Numerics;

namespace Game.Server.HL2;
using FIELD = Source.FIELD<PropAirboat>;
public class PropAirboat : PropVehicleDriveable
{
	public static readonly SendTable DT_PropAirboat = new(DT_PropVehicleDriveable, [
		SendPropBool(FIELD.OF(nameof(HeadlightIsOn))),
		SendPropInt(FIELD.OF(nameof(AmmoCount)), 9),
		SendPropInt(FIELD.OF(nameof(ExactWaterLevel)), 32),
		SendPropInt(FIELD.OF(nameof(WaterLevel)), 8),
		SendPropVector(FIELD.OF(nameof(PhysVelocity)), 0, PropFlags.NoScale),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass("PropAirboat", DT_PropAirboat).WithManualClassID(StaticClassIndices.CPropAirboat);

	public bool HeadlightIsOn;
	public int AmmoCount;
	public int ExactWaterLevel;
	public Vector3 PhysVelocity;
}
