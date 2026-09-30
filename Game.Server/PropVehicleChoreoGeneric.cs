using Game.Shared;
using Source.Common;
using Source;
using System.Numerics;

namespace Game.Server;

using FIELD = FIELD<PropVehicleChoreoGeneric>;

[NetworkName("CPropVehicleChoreoGeneric")]
public partial class PropVehicleChoreoGeneric : DynamicProp
{
	[NetworkName("m_hPlayer")]
	[NetworkVar] public partial NetworkHandle<BaseEntity> Player { get; }
	[NetworkName("m_bEnterAnimOn")]
	[NetworkVar] public partial bool EnterAnimOn { get; set; }
	[NetworkName("m_bExitAnimOn")]
	[NetworkVar] public partial bool ExitAnimOn { get; set; }
	[NetworkName("m_bForceEyesToAttachment")]
	[NetworkVar] public partial bool ForceEyesToAttachment { get; set; }
	[NetworkName("m_vecEyeExitEndpoint")]
	[NetworkVar] public partial Vector3 EyeExitEndpoint { get; set; }
	[NetworkName("m_vehicleView.bClampEyeAngles")]
	public bool VehicleViewClampEyeAngles;
	[NetworkName("m_vehicleView.flPitchCurveZero")]
	public float VehicleViewPitchCurveZero;
	[NetworkName("m_vehicleView.flPitchCurveLinear")]
	public float VehicleViewPitchCurveLinear;
	[NetworkName("m_vehicleView.flRollCurveZero")]
	public float VehicleViewRollCurveZero;
	[NetworkName("m_vehicleView.flRollCurveLinear")]
	public float VehicleViewRollCurveLinear;
	[NetworkName("m_vehicleView.flFOV")]
	public float VehicleViewFOV;
	[NetworkName("m_vehicleView.flYawMin")]
	public float VehicleViewYawMin;
	[NetworkName("m_vehicleView.flYawMax")]
	public float VehicleViewYawMax;
	[NetworkName("m_vehicleView.flPitchMin")]
	public float VehicleViewPitchMin;
	[NetworkName("m_vehicleView.flPitchMax")]
	public float VehicleViewPitchMax;

	public static readonly SendTable DT_PropVehicleChoreoGeneric = new(DT_DynamicProp, [
		SendPropEHandle(NetworkVarFields.Player),
		SendPropBool(NetworkVarFields.EnterAnimOn),
		SendPropBool(NetworkVarFields.ExitAnimOn),
		SendPropBool(NetworkVarFields.ForceEyesToAttachment),
		SendPropVector(NetworkVarFields.EyeExitEndpoint, 0, PropFlags.Coord),
		SendPropBool(FIELD.OF(nameof(VehicleViewClampEyeAngles))),
		SendPropFloat(FIELD.OF(nameof(VehicleViewPitchCurveZero)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(VehicleViewPitchCurveLinear)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(VehicleViewRollCurveZero)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(VehicleViewRollCurveLinear)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(VehicleViewFOV)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(VehicleViewYawMin)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(VehicleViewYawMax)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(VehicleViewPitchMin)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(VehicleViewPitchMax)), 0, PropFlags.NoScale),
	]);
	public static new readonly ServerClass ServerClass = new ServerClass(DT_PropVehicleChoreoGeneric);
}
