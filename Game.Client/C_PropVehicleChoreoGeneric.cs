using Source.Common;
using Source;
using System.Numerics;

namespace Game.Client;

using FIELD = FIELD<C_PropVehicleChoreoGeneric>;

[NetworkName("CPropVehicleChoreoGeneric")]
public class C_PropVehicleChoreoGeneric : C_DynamicProp
{
	[NetworkName("m_hPlayer")]
	public EHANDLE Player;
	[NetworkName("m_bEnterAnimOn")]
	public bool EnterAnimOn;
	[NetworkName("m_bExitAnimOn")]
	public bool ExitAnimOn;
	[NetworkName("m_bForceEyesToAttachment")]
	public bool ForceEyesToAttachment;
	[NetworkName("m_vecEyeExitEndpoint")]
	public Vector3 EyeExitEndpoint;
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

	public static readonly RecvTable DT_PropVehicleChoreoGeneric = new(DT_DynamicProp, [
		RecvPropEHandle(FIELD.OF(nameof(Player))),
		RecvPropBool(FIELD.OF(nameof(EnterAnimOn))),
		RecvPropBool(FIELD.OF(nameof(ExitAnimOn))),
		RecvPropBool(FIELD.OF(nameof(ForceEyesToAttachment))),
		RecvPropVector(FIELD.OF(nameof(EyeExitEndpoint))),
		RecvPropBool(FIELD.OF(nameof(VehicleViewClampEyeAngles))),
		RecvPropFloat(FIELD.OF(nameof(VehicleViewPitchCurveZero))),
		RecvPropFloat(FIELD.OF(nameof(VehicleViewPitchCurveLinear))),
		RecvPropFloat(FIELD.OF(nameof(VehicleViewRollCurveZero))),
		RecvPropFloat(FIELD.OF(nameof(VehicleViewRollCurveLinear))),
		RecvPropFloat(FIELD.OF(nameof(VehicleViewFOV))),
		RecvPropFloat(FIELD.OF(nameof(VehicleViewYawMin))),
		RecvPropFloat(FIELD.OF(nameof(VehicleViewYawMax))),
		RecvPropFloat(FIELD.OF(nameof(VehicleViewPitchMin))),
		RecvPropFloat(FIELD.OF(nameof(VehicleViewPitchMax))),
	]);
	public static new readonly ClientClass ClientClass = new ClientClass(DT_PropVehicleChoreoGeneric);
}
