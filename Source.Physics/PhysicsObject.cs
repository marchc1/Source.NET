using BepuPhysics;

using Source.Common.Mathematics;
using Source.Common.Physics;

using System;
using System.Collections.Generic;
using System.Text;

namespace Source.Physics;

internal class PhysicsObject : IPhysicsObject
{
	internal PhysicsEnvironment Environment = null!;
	internal BodyHandle Handle;

	internal void SetHandle(PhysicsEnvironment env, BodyHandle handle){
		Environment = env;
		Handle = handle;
	}

	internal object? GameData;

	public void AddVelocity(in AngularImpulse velocity, in AngularImpulse angularVelocity) {
		throw new NotImplementedException();
	}

	public void ApplyForceCenter(in AngularImpulse forceVector) {
		throw new NotImplementedException();
	}

	public void ApplyForceOffset(in AngularImpulse forceVector, in AngularImpulse worldPosition) {
		throw new NotImplementedException();
	}

	public void ApplyTorqueCenter(in AngularImpulse torque) {
		throw new NotImplementedException();
	}

	public void BecomeHinged(int localAxis) {
		throw new NotImplementedException();
	}

	public void BecomeTrigger() {
		throw new NotImplementedException();
	}

	public float CalculateAngularDrag(in AngularImpulse objectSpaceRotationAxis) {
		throw new NotImplementedException();
	}

	public void CalculateForceOffset(in AngularImpulse forceVector, in AngularImpulse worldPosition, out AngularImpulse centerForce, out AngularImpulse centerTorque) {
		throw new NotImplementedException();
	}

	public float CalculateLinearDrag(in AngularImpulse unitDirection) {
		throw new NotImplementedException();
	}

	public void CalculateVelocityOffset(in AngularImpulse forceVector, in AngularImpulse worldPosition, out AngularImpulse centerVelocity, out AngularImpulse centerAngularVelocity) {
		throw new NotImplementedException();
	}

	public float ComputeShadowControl(in HLShadowControlParams parms, double secondsToArrival, double dt) {
		throw new NotImplementedException();
	}

	public IPhysicsFrictionSnapshot CreateFrictionSnapshot() {
		throw new NotImplementedException();
	}

	public void DestroyFrictionSnapshot(IPhysicsFrictionSnapshot snapshot) {
		throw new NotImplementedException();
	}

	public void EnableCollisions(bool enable) {
		throw new NotImplementedException();
	}

	public void EnableDrag(bool enable) {
		throw new NotImplementedException();
	}

	public void EnableGravity(bool enable) {
		throw new NotImplementedException();
	}

	public void EnableMotion(bool enable) {
		throw new NotImplementedException();
	}

	public CallbackFlags GetCallbackFlags() {
		throw new NotImplementedException();
	}

	public PhysCollide GetCollide() {
		throw new NotImplementedException();
	}

	public bool GetContactPoint(out AngularImpulse contactPoint, IPhysicsObject contactObject) {
		throw new NotImplementedException();
	}

	public uint GetContents() {
		throw new NotImplementedException();
	}

	public void GetDamping(out float speed, out float rot) {
		throw new NotImplementedException();
	}

	public float GetEnergy() {
		throw new NotImplementedException();
	}

	public object? GetGameData() => GameData;

	public ushort GetGameFlags() {
		throw new NotImplementedException();
	}

	public ushort GetGameIndex() {
		throw new NotImplementedException();
	}

	public void GetImplicitVelocity(out AngularImpulse velocity, out AngularImpulse angularVelocity) {
		throw new NotImplementedException();
	}

	public AngularImpulse GetInertia() {
		throw new NotImplementedException();
	}

	public AngularImpulse GetInvInertia() {
		throw new NotImplementedException();
	}

	public float GetInvMass() {
		throw new NotImplementedException();
	}

	public float GetMass() {
		throw new NotImplementedException();
	}

	public AngularImpulse GetMassCenterLocalSpace() {
		throw new NotImplementedException();
	}

	public int GetMaterialIndex() {
		throw new NotImplementedException();
	}

	public ReadOnlySpan<char> GetName() {
		throw new NotImplementedException();
	}

	public void GetPosition(out AngularImpulse worldPosition, out QAngle angles) {
		throw new NotImplementedException();
	}

	public void GetPositionMatrix(out Matrix3x4 positionMatrix) {
		throw new NotImplementedException();
	}

	public IPhysicsShadowController GetShadowController() {
		throw new NotImplementedException();
	}

	public int GetShadowPosition(out AngularImpulse position, out QAngle angles) {
		throw new NotImplementedException();
	}

	public float GetSphereRadius() {
		throw new NotImplementedException();
	}

	public void GetVelocity(out AngularImpulse velocity, out AngularImpulse angularVelocity) {
		throw new NotImplementedException();
	}

	public void GetVelocityAtPoint(in AngularImpulse worldPosition, out AngularImpulse velocity) {
		throw new NotImplementedException();
	}

	public bool IsAsleep() {
		throw new NotImplementedException();
	}

	public bool IsAttachedToConstraint(bool externalOnly) {
		throw new NotImplementedException();
	}

	public bool IsCollisionEnabled() {
		throw new NotImplementedException();
	}

	public bool IsDragEnabled() {
		throw new NotImplementedException();
	}

	public bool IsFluid() {
		throw new NotImplementedException();
	}

	public bool IsGravityEnabled() {
		throw new NotImplementedException();
	}

	public bool IsHinged() {
		throw new NotImplementedException();
	}

	public bool IsMotionEnabled() {
		throw new NotImplementedException();
	}

	public bool IsMoveable() {
		throw new NotImplementedException();
	}

	public bool IsStatic() {
		throw new NotImplementedException();
	}

	public bool IsTrigger() {
		throw new NotImplementedException();
	}

	public void LocalToWorld(out AngularImpulse worldPosition, in AngularImpulse localPosition) {
		throw new NotImplementedException();
	}

	public void LocalToWorldVector(out AngularImpulse worldVector, in AngularImpulse localVector) {
		throw new NotImplementedException();
	}

	public void OutputDebugInfo() {
		throw new NotImplementedException();
	}

	public void RecheckCollisionFilter() {
		throw new NotImplementedException();
	}

	public void RecheckContactPoints() {
		throw new NotImplementedException();
	}

	public void RemoveHinged() {
		throw new NotImplementedException();
	}

	public void RemoveShadowController() {
		throw new NotImplementedException();
	}

	public void RemoveTrigger() {
		throw new NotImplementedException();
	}

	public void SetBuoyancyRatio(float ratio) {
		throw new NotImplementedException();
	}

	public void SetCallbackFlags(CallbackFlags callbackflags) {
		throw new NotImplementedException();
	}

	public void SetContents(uint contents) {
		throw new NotImplementedException();
	}

	public void SetDamping(ref float speed, ref float rot) {
		throw new NotImplementedException();
	}

	public void SetDragCoefficient(ref float drag, ref float angularDrag) {
		throw new NotImplementedException();
	}

	public void SetGameData(object? gameData) => GameData = gameData;

	public void SetGameFlags(ushort userFlags) {
		throw new NotImplementedException();
	}

	public void SetGameIndex(ushort gameIndex) {
		throw new NotImplementedException();
	}

	public void SetInertia(in AngularImpulse inertia) {
		throw new NotImplementedException();
	}

	public void SetMass(float mass) {
		throw new NotImplementedException();
	}

	public void SetMaterialIndex(int materialIndex) {
		throw new NotImplementedException();
	}

	public void SetPosition(in AngularImpulse worldPosition, in QAngle angles, bool isTeleport) {
		throw new NotImplementedException();
	}

	public void SetPositionMatrix(in Matrix3x4 matrix, bool isTeleport) {
		throw new NotImplementedException();
	}

	public void SetShadow(float maxSpeed, float maxAngularSpeed, bool allowPhysicsMovement, bool allowPhysicsRotation) {
		throw new NotImplementedException();
	}

	public void SetVelocity(in AngularImpulse velocity, in AngularImpulse angularVelocity) {
		throw new NotImplementedException();
	}

	public void SetVelocityInstantaneous(in AngularImpulse velocity, in AngularImpulse angularVelocity) {
		throw new NotImplementedException();
	}

	public void Sleep() {
		throw new NotImplementedException();
	}

	public void UpdateShadow(in AngularImpulse targetPosition, in QAngle targetAngles, bool tempDisableGravity, float timeOffset) {
		throw new NotImplementedException();
	}

	public void Wake() {
		throw new NotImplementedException();
	}

	public void WorldToLocal(out AngularImpulse localPosition, in AngularImpulse worldPosition) {
		throw new NotImplementedException();
	}

	public void WorldToLocalVector(out AngularImpulse localVector, in AngularImpulse worldVector) {
		throw new NotImplementedException();
	}
}
