using BepuPhysics;
using BepuPhysics.Collidables;

using Source.Common.Formats.BSP;
using Source.Common.Mathematics;
using Source.Common.Physics;

using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Source.Physics;

internal class PhysicsObject : IPhysicsObject
{
	public object? GameData;
	public BodyHandle? Body;
	public StaticHandle? Static;
	public PhysCollide? Collide;
	public IPhysicsShadowController? Shadow;

	public Vector3 DragBasis;
	public Vector3 AngDragBasis;
	public bool ShadowTempGravityDisable;
	public bool HasTouchedDynamic;
	public bool AsleepSinceCreation;
	public bool ForceSilentDelete;
	public byte SleepState;
	public byte HingedAxis;
	public byte CollideType;
	public ushort GameIndex;
	public ushort MaterialIndex;
	public ushort ActiveIndex;
	public ushort Callbacks;
	public ushort GameFlags;
	public Contents ContentsMask;
	public float Volume;
	public float BuoyancyRatio;
	public float DragCoefficient;
	public float AngDragCoefficient;

	public void AddVelocity(in Vector3 velocity, in Vector3 angularVelocity) {
		throw new NotImplementedException();
	}

	public void ApplyForceCenter(in Vector3 forceVector) {
		throw new NotImplementedException();
	}

	public void ApplyForceOffset(in Vector3 forceVector, in Vector3 worldPosition) {
		throw new NotImplementedException();
	}

	public void ApplyTorqueCenter(in Vector3 torque) {
		throw new NotImplementedException();
	}

	public void BecomeHinged(int localAxis) {
		throw new NotImplementedException();
	}

	public void BecomeTrigger() {
		throw new NotImplementedException();
	}

	public float CalculateAngularDrag(in Vector3 objectSpaceRotationAxis) {
		throw new NotImplementedException();
	}

	public void CalculateForceOffset(in Vector3 forceVector, in Vector3 worldPosition, out Vector3 centerForce, out Vector3 centerTorque) {
		throw new NotImplementedException();
	}

	public float CalculateLinearDrag(in Vector3 unitDirection) {
		throw new NotImplementedException();
	}

	public void CalculateVelocityOffset(in Vector3 forceVector, in Vector3 worldPosition, out Vector3 centerVelocity, out Vector3 centerAngularVelocity) {
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

	public bool GetContactPoint(out Vector3 contactPoint, IPhysicsObject contactObject) {
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

	public PhysicsFlags GetGameFlags() {
		return 0;
	}

	public ushort GetGameIndex() {
		throw new NotImplementedException();
	}

	public void GetImplicitVelocity(out Vector3 velocity, out Vector3 angularVelocity) {
		throw new NotImplementedException();
	}

	public Vector3 GetInertia() {
		throw new NotImplementedException();
	}

	public Vector3 GetInvInertia() {
		throw new NotImplementedException();
	}

	public float GetInvMass() {
		throw new NotImplementedException();
	}

	public float GetMass() {
		throw new NotImplementedException();
	}

	public Vector3 GetMassCenterLocalSpace() {
		throw new NotImplementedException();
	}

	public int GetMaterialIndex() {
		throw new NotImplementedException();
	}

	public ReadOnlySpan<char> GetName() {
		throw new NotImplementedException();
	}

	public void GetPosition(out Vector3 worldPosition, out QAngle angles) {
		throw new NotImplementedException();
	}

	public void GetPositionMatrix(out Matrix3x4 positionMatrix) {
		throw new NotImplementedException();
	}

	public IPhysicsShadowController GetShadowController() {
		throw new NotImplementedException();
	}

	public int GetShadowPosition(out Vector3 position, out QAngle angles) {
		throw new NotImplementedException();
	}

	public float GetSphereRadius() {
		throw new NotImplementedException();
	}

	public void GetVelocity(out Vector3 velocity, out Vector3 angularVelocity) {
		throw new NotImplementedException();
	}

	public void GetVelocityAtPoint(in Vector3 worldPosition, out Vector3 velocity) {
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
		if (IsStatic() || !IsMotionEnabled())
			return false;
		return true;
	}

	public bool IsStatic() => Static.HasValue;

	public bool IsTrigger() {
		throw new NotImplementedException();
	}

	public void LocalToWorld(out Vector3 worldPosition, in Vector3 localPosition) {
		throw new NotImplementedException();
	}

	public void LocalToWorldVector(out Vector3 worldVector, in Vector3 localVector) {
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

	public void SetGameFlags(PhysicsFlags userFlags) {

	}

	public void SetGameIndex(ushort gameIndex) {
		throw new NotImplementedException();
	}

	public void SetInertia(in Vector3 inertia) {
		throw new NotImplementedException();
	}

	public void SetMass(float mass) {
		throw new NotImplementedException();
	}

	public void SetMaterialIndex(int materialIndex) {
		throw new NotImplementedException();
	}

	public void SetPosition(in Vector3 worldPosition, in QAngle angles, bool isTeleport) {
		throw new NotImplementedException();
	}

	public void SetPositionMatrix(in Matrix3x4 matrix, bool isTeleport) {
		throw new NotImplementedException();
	}

	public void SetShadow(float maxSpeed, float maxAngularSpeed, bool allowPhysicsMovement, bool allowPhysicsRotation) {
		throw new NotImplementedException();
	}

	public void SetVelocity(in Vector3 velocity, in Vector3 angularVelocity) {
		throw new NotImplementedException();
	}

	public void SetVelocityInstantaneous(in Vector3 velocity, in Vector3 angularVelocity) {
		throw new NotImplementedException();
	}

	public void Sleep() {
		throw new NotImplementedException();
	}

	public void UpdateShadow(in Vector3 targetPosition, in QAngle targetAngles, bool tempDisableGravity, float timeOffset) {
		throw new NotImplementedException();
	}

	public void Wake() {
		throw new NotImplementedException();
	}

	public void WorldToLocal(out Vector3 localPosition, in Vector3 worldPosition) {
		throw new NotImplementedException();
	}

	public void WorldToLocalVector(out Vector3 localVector, in Vector3 worldVector) {
		throw new NotImplementedException();
	}

	internal static IPhysicsObject? CreatePhysicsObject(PhysicsEnvironment environment, PhysCollide collisionModel, int materialIndex, in Vector3 position, in QAngle angles, ref ObjectParams objParams, bool isStatic) {
		if (materialIndex < 0)
			materialIndex = (int)physprops.GetSurfaceIndex("default");

		if (collisionModel is not PhysCollideCompactSurface compactSurface)
			return null;

		var hulls = compactSurface.ConvexHulls;
		if (hulls.Count == 0)
			return null;

		var sim = environment.GetBepuEnvironment();
		var pool = sim.BufferPool;

		MathLib.AngleQuaternion(in angles, out Quaternion orientation);
		RigidPose pose = new(position, orientation);

		PhysicsObject obj = new();
		obj.AsleepSinceCreation = true;
		obj.Collide = collisionModel;
		obj.MaterialIndex = (ushort)materialIndex;

		if (isStatic) {
			TypedIndex shapeIndex;
			if (hulls.Count == 1) {
				pool.Take<Vector3>(hulls[0].Length, out var bepuVerts);
				for (int i = 0; i < hulls[0].Length; i++)
					bepuVerts[i] = hulls[0][i];
				var hull = new ConvexHull(bepuVerts, pool, out _);
				shapeIndex = sim.Shapes.Add(hull);
				pool.Return(ref bepuVerts);
			}
			else {
				using var builder = new CompoundBuilder(pool, sim.Shapes, hulls.Count);
				foreach (var hullVerts in hulls) {
					pool.Take<Vector3>(hullVerts.Length, out var bepuVerts);
					for (int i = 0; i < hullVerts.Length; i++)
						bepuVerts[i] = hullVerts[i];
					var hull = new ConvexHull(bepuVerts, pool, out _);
					builder.Add(hull, RigidPose.Identity, 1f);
					pool.Return(ref bepuVerts);
				}
				builder.BuildKinematicCompound(out var children);
				shapeIndex = sim.Shapes.Add(new Compound(children));
			}

			obj.Static = sim.Statics.Add(new StaticDescription(
				pose,
				shapeIndex
			));
		}
		else {
			float mass = objParams.Mass > 0 ? objParams.Mass : 1f;

			TypedIndex shapeIndex;
			BodyInertia inertia;

			if (hulls.Count == 1) {
				pool.Take<Vector3>(hulls[0].Length, out var bepuVerts);
				for (int i = 0; i < hulls[0].Length; i++)
					bepuVerts[i] = hulls[0][i];
				var hull = new ConvexHull(bepuVerts, pool, out _);
				inertia = hull.ComputeInertia(mass);
				shapeIndex = sim.Shapes.Add(hull);
				pool.Return(ref bepuVerts);
			}
			else {
				using var builder = new CompoundBuilder(pool, sim.Shapes, hulls.Count);
				foreach (var hullVerts in hulls) {
					pool.Take<Vector3>(hullVerts.Length, out var bepuVerts);
					for (int i = 0; i < hullVerts.Length; i++)
						bepuVerts[i] = hullVerts[i];
					var hull = new ConvexHull(bepuVerts, pool, out _);
					builder.Add(hull, RigidPose.Identity, 1f);
					pool.Return(ref bepuVerts);
				}
				builder.BuildDynamicCompound(out var children, out inertia);
				shapeIndex = sim.Shapes.Add(new Compound(children));
			}

			var bodyHandle = sim.Bodies.Add(BodyDescription.CreateDynamic(
				pose,
				inertia,
				shapeIndex,
				0.01f
			));

			obj.Body = bodyHandle;

			if (objParams.Damping > 0 || objParams.RotDamping > 0) {
				obj.DragCoefficient = objParams.Damping;
				obj.AngDragCoefficient = objParams.RotDamping;
			}
		}

		return obj;
	}
}
