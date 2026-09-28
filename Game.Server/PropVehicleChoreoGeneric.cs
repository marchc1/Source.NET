using Source.Common;
using Source;
using System.Numerics;

namespace Game.Server;

using FIELD = FIELD<PropVehicleChoreoGeneric>;

public class PropVehicleChoreoGeneric : DynamicProp
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

	public static readonly SendTable DT_PropVehicleChoreoGeneric = new(DT_DynamicProp, [
		SendPropEHandle(FIELD.OF(nameof(Player))),
		SendPropBool(FIELD.OF(nameof(EnterAnimOn))),
		SendPropBool(FIELD.OF(nameof(ExitAnimOn))),
		SendPropBool(FIELD.OF(nameof(ForceEyesToAttachment))),
		SendPropVector(FIELD.OF(nameof(EyeExitEndpoint)), 0, PropFlags.Coord),
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
	public static new readonly ServerClass ServerClass = new ServerClass("PropVehicleChoreoGeneric", DT_PropVehicleChoreoGeneric).WithManualClassID(Shared.StaticClassIndices.CPropVehicleChoreoGeneric);
}
