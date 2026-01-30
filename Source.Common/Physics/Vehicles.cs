using System.Numerics;
using System.Runtime.CompilerServices;

namespace Source.Common.Physics;


public enum VehicleType
{
	CarWheels = 1 << 0,
	CarRaycast = 1 << 1,
	JetskiRaycast = 1 << 2,
	AirboatRaycast = 1 << 3
}

public static class VehicleConstants
{

	public const int VEHICLE_MAX_AXLE_COUNT = 4;
	public const int VEHICLE_MAX_GEAR_COUNT = 6;
	public const int VEHICLE_MAX_WHEEL_COUNT = (2 * VEHICLE_MAX_AXLE_COUNT);
	public const int VEHICLE_TIRE_NORMAL = 0;
	public const int VEHICLE_TIRE_BRAKING = 1;
	public const int VEHICLE_TIRE_POWERSLIDE = 2;
}


public struct VehicleControlParams
{
	public float Throttle;
	public float Steering;
	public float Brake;
	public float Boost;
	public bool Handbrake;
	public bool HandbrakeLeft;
	public bool HandbrakeRight;
	public bool Brakepedal;
	public bool BHasBrakePedal;
	public bool BAnalogSteering;
}

public struct VehicleOperatingParams
{
	public float Speed;
	public float EngineRPM;
	public int Gear;
	public float BoostDelay;
	public int BoostTimeLeft;
	public float SkidSpeed;
	public int SkidMaterial;
	public float SteeringAngle;
	public int WheelsNotInContact;
	public int WheelsInContact;
	public bool IsTorqueBoosting;
}

public struct VehicleParams
{

}

public struct VehicleDebugCarSystem
{
	const int VEHICLE_DEBUGRENDERDATA_MAX_WHEELS = 10;
	const int VEHICLE_DEBUGRENDERDATA_MAX_AXLES = 3;
	[InlineArray(VEHICLE_DEBUGRENDERDATA_MAX_AXLES)] public struct InlineArrayMaxAxles<T> { T first; }
	[InlineArray(VEHICLE_DEBUGRENDERDATA_MAX_WHEELS)] public struct InlineArrayMaxWheels<T> { T first; }

	public InlineArrayMaxAxles<Vector3> AxlePos;
	public InlineArrayMaxWheels<Vector3> WheelPos;
	public InlineArray2<InlineArrayMaxWheels<Vector3>> WheelRaycasts;
	public InlineArrayMaxWheels<Vector3> WheelRaycastImpacts;
}


public interface IPhysicsVehicleController
{
	// call this from the game code with the control parameters
	void Update(TimeUnit_t dt, ref VehicleControlParams controls);
	ref readonly VehicleOperatingParams GetOperatingParams();
	ref readonly VehicleParams GetVehicleParams();
	ref VehicleParams GetVehicleParamsForChange();
	float UpdateBooster(float dt);
	int GetWheelCount();
	IPhysicsObject GetWheel(int index);
	bool GetWheelContactPoint(int index, out Vector3 contactPoint, out int surfaceProps);
	void SetSpringLength(int wheelIndex, float length);
	void SetWheelFriction(int wheelIndex, float friction);

	void OnVehicleEnter();
	void OnVehicleExit();

	void SetEngineDisabled(bool disable);
	bool IsEngineDisabled();

	// Debug
	void GetCarSystemDebugData(out VehicleDebugCarSystem debugCarSystem);
	void VehicleDataReload();
}
