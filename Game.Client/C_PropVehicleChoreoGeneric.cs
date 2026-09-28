using Source.Common;
using Source;
using System.Numerics;

namespace Game.Client;

using FIELD = FIELD<C_PropVehicleChoreoGeneric>;

public class C_PropVehicleChoreoGeneric : C_DynamicProp
{
	public EHANDLE Player;
	public bool EnterAnimOn;
	public bool ExitAnimOn;
	public bool ForceEyesToAttachment;
	public Vector3 EyeExitEndpoint;
	public bool VehicleViewClampEyeAngles;
	public float VehicleViewPitchCurveZero;
	public float VehicleViewPitchCurveLinear;
	public float VehicleViewRollCurveZero;
	public float VehicleViewRollCurveLinear;
	public float VehicleViewFOV;
	public float VehicleViewYawMin;
	public float VehicleViewYawMax;
	public float VehicleViewPitchMin;
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
	public static new readonly ClientClass ClientClass = new ClientClass("PropVehicleChoreoGeneric", DT_PropVehicleChoreoGeneric).WithManualClassID(Shared.StaticClassIndices.CPropVehicleChoreoGeneric);
}
