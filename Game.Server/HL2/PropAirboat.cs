using Game.Server;
using Game.Shared;

using Source.Common;

using System.Numerics;

namespace Game.Server.HL2;
using FIELD = Source.FIELD<PropAirboat>;
[NetworkName("CPropAirboat")]
public partial class PropAirboat : PropVehicleDriveable
{
	public static readonly SendTable DT_PropAirboat = new(DT_PropVehicleDriveable, [
		SendPropBool(NetworkVarFields.HeadlightIsOn),
		SendPropInt(NetworkVarFields.AmmoCount, 9),
		SendPropInt(NetworkVarFields.ExactWaterLevel, 24),
		SendPropInt(BaseEntity.NetworkVarFields.WaterLevel, 2, PropFlags.Unsigned),
		SendPropVector(NetworkVarFields.PhysVelocity, 0, PropFlags.NoScale),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_PropAirboat);

	[NetworkName("m_bHeadlightIsOn")]
	[NetworkVar] public partial bool HeadlightIsOn { get; set; }
	[NetworkName("m_nAmmoCount")]
	[NetworkVar] public partial int AmmoCount { get; set; }
	[NetworkName("m_nExactWaterLevel")]
	[NetworkVar] public partial int ExactWaterLevel { get; set; }
	[NetworkName("m_vecPhysVelocity")]
	[NetworkVar] public partial Vector3 PhysVelocity { get; set; }
}
