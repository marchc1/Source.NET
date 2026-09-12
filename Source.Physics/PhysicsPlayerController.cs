using Source.Common.Mathematics;
using Source.Common.Physics;

using System.Numerics;

namespace Source.Physics;

internal class PhysicsPlayerController : IPhysicsPlayerController
{
	PhysicsObject Object;
	IPhysicsPlayerControllerEvent? Handler;
	float PushMassLimit = 350.0f;
	float PushSpeedLimit = 50.0f;
	Vector3 MaxVelocity = new(1e5f);
	Vector3 LastImpulse;

	public PhysicsPlayerController(PhysicsObject obj) {
		Object = obj;
		Object.BecomeKinematic();
	}

	public void Update(in Vector3 position, in Vector3 velocity, float secondsToArrival, bool onground, IPhysicsObject ground) {
		if (Object.Body == null)
			return;

		float dt = secondsToArrival > 1e-4f ? secondsToArrival : Object.Env.GetSimulationTimestepSeconds();
		Object.GetPosition(out Vector3 current, out _);
		Vector3 targetVelocity = velocity + (position - current) / dt;

		Object.SetVelocity(targetVelocity, default);
	}

	public void SetEventHandler(IPhysicsPlayerControllerEvent handler) => Handler = handler;

	public bool IsInContact() => Object.HasTouchedDynamic;

	public void MaxSpeed(in Vector3 maxVelocity) => MaxVelocity = maxVelocity;

	public void SetObject(IPhysicsObject obj) {
		Object = (PhysicsObject)obj;
		Object.BecomeKinematic();
	}

	public int GetShadowPosition(out Vector3 position, out QAngle angles) {
		Object.GetPosition(out position, out angles);
		return 1;
	}

	public void StepUp(float height) { }

	public void Jump() { }

	public void GetShadowVelocity(out Vector3 velocity) => Object.GetVelocity(out velocity, out _);

	public IPhysicsObject? GetObject() => Object;

	public void GetLastImpulse(out Vector3 vec) => vec = LastImpulse;

	public void SetPushMassLimit(float maxPushMass) => PushMassLimit = maxPushMass;

	public void SetPushSpeedLimit(float maxPushSpeed) => PushSpeedLimit = maxPushSpeed;

	public float GetPushMassLimit() => PushMassLimit;

	public float GetPushSpeedLimit() => PushSpeedLimit;

	public bool WasFrozen() => false;
}
