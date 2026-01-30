using Source.Common.Mathematics;

using System.Numerics;

namespace Source.Common.Physics;

public interface IPhysicsPlayerControllerEvent
{
	bool ShouldMoveTo(IPhysicsObject obj, in Vector3 position);
}

public interface IPhysicsPlayerController
{
	void Update(in Vector3 position, in Vector3 velocity, float secondsToArrival, bool onground, IPhysicsObject ground);
	void SetEventHandler(IPhysicsPlayerControllerEvent handler);
	bool IsInContact();
	void MaxSpeed(in Vector3 maxVelocity);

	// allows game code to change collision models
	void SetObject(IPhysicsObject obj);
	// UNDONE: Refactor this and shadow controllers into a single class/interface through IPhysicsObject
	int GetShadowPosition(out Vector3 position, out QAngle angles);
	void StepUp(float height);
	void Jump();
	void GetShadowVelocity(out Vector3 velocity);
	IPhysicsObject? GetObject();
	void GetLastImpulse(out Vector3 vec);

	void SetPushMassLimit(float maxPushMass);
	void SetPushSpeedLimit(float maxPushSpeed);

	float GetPushMassLimit();
	float GetPushSpeedLimit();
	bool WasFrozen();
}
