using Game.Server;
using Game.Shared;

using Source.Common;

using System.Numerics;

namespace Game.Server.HL2;
using FIELD = Source.FIELD<PropAirboat>;
[LinkEntityToClass("prop_vehicle_airboat")]
[NetworkName("CPropAirboat")]
public class PropAirboat : PropVehicleDriveable
{
	public static readonly SendTable DT_PropAirboat = new(DT_PropVehicleDriveable, [
		SendPropBool(FIELD.OF(nameof(HeadlightIsOn))),
		SendPropInt(FIELD.OF(nameof(AmmoCount)), 9),
		SendPropInt(FIELD.OF(nameof(ExactWaterLevel)), 24),
		SendPropInt(FIELD.OF(nameof(WaterLevel)), 2, PropFlags.Unsigned),
		SendPropVector(FIELD.OF(nameof(PhysVelocity)), 0, PropFlags.NoScale),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_PropAirboat);

	[NetworkName("m_bHeadlightIsOn")]
	public bool HeadlightIsOn;
	[NetworkName("m_nAmmoCount")]
	public int AmmoCount;
	[NetworkName("m_nExactWaterLevel")]
	public int ExactWaterLevel;
	[NetworkName("m_vecPhysVelocity")]
	public Vector3 PhysVelocity;
}
