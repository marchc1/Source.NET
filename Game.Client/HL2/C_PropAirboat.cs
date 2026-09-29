using Game.Shared;

using Source.Common;

using System.Numerics;

namespace Game.Client.HL2;
using FIELD = Source.FIELD<C_PropAirboat>;

[NetworkName("CPropAirboat")]
public class C_PropAirboat : C_PropVehicleDriveable
{
	public static readonly RecvTable DT_PropAirboat = new(DT_PropVehicleDriveable, [
		RecvPropBool(FIELD.OF(nameof(HeadlightIsOn))),
		RecvPropInt(FIELD.OF(nameof(AmmoCount))),
		RecvPropInt(FIELD.OF(nameof(ExactWaterLevel))),
		RecvPropInt(FIELD.OF(nameof(WaterLevel))),
		RecvPropVector(FIELD.OF(nameof(PhysVelocity))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass(DT_PropAirboat);

	[NetworkName("m_bHeadlightIsOn")]
	public bool HeadlightIsOn;
	[NetworkName("m_nAmmoCount")]
	public int AmmoCount;
	[NetworkName("m_nExactWaterLevel")]
	public int ExactWaterLevel;
	[NetworkName("m_vecPhysVelocity")]
	public Vector3 PhysVelocity;
}
