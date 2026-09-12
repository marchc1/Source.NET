using BepuPhysics;
using BepuPhysics.Collidables;

using BepuUtilities.Memory;

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

	internal PhysicsEnvironment Env = null!;
	internal TypedIndex ShapeIndex;
	internal Vector3 MassCenterOffset;
	internal float Mass = 1.0f;
	internal bool MotionEnabled = true;
	internal bool GravityEnabled = true;
	internal bool CollisionsEnabled = true;

	Simulation Sim => Env.GetBepuEnvironment();
	BodyReference BodyRef => Sim.Bodies[Body!.Value];

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
		if (!Body.HasValue)
			return;
		var bodyRef = BodyRef;
		bodyRef.Velocity.Linear += IVPConvert.PositionToIVP(velocity);
		bodyRef.Velocity.Angular += IVPConvert.AngularToIVP(angularVelocity);
		bodyRef.Awake = true;
	}

	public void ApplyForceCenter(in Vector3 forceVector) {
		if (!Body.HasValue)
			return;
		var bodyRef = BodyRef;
		bodyRef.Awake = true;
		bodyRef.ApplyLinearImpulse(IVPConvert.ForceImpulseToIVP(forceVector) * Env.GetSimulationTimestepSeconds());
	}

	public void ApplyForceOffset(in Vector3 forceVector, in Vector3 worldPosition) {
		if (!Body.HasValue)
			return;
		var bodyRef = BodyRef;
		bodyRef.Awake = true;
		Vector3 offset = IVPConvert.PositionToIVP(worldPosition) - bodyRef.Pose.Position;
		bodyRef.ApplyImpulse(IVPConvert.ForceImpulseToIVP(forceVector) * Env.GetSimulationTimestepSeconds(), offset);
	}

	public void ApplyTorqueCenter(in Vector3 torque) {
		if (!Body.HasValue)
			return;
		var bodyRef = BodyRef;
		bodyRef.Awake = true;
		bodyRef.ApplyAngularImpulse(IVPConvert.AngularToIVP(torque) * Env.GetSimulationTimestepSeconds());
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
		Vector3 center = GetPose().Position;
		centerForce = forceVector;
		centerTorque = Vector3.Cross(worldPosition - center, forceVector);
	}

	public float CalculateLinearDrag(in Vector3 unitDirection) {
		throw new NotImplementedException();
	}

	public void CalculateVelocityOffset(in Vector3 forceVector, in Vector3 worldPosition, out Vector3 centerVelocity, out Vector3 centerAngularVelocity) {
		Vector3 center = GetPose().Position;
		float invMass = Mass > 0 ? 1.0f / Mass : 0.0f;
		centerVelocity = forceVector * invMass;
		centerAngularVelocity = Vector3.Cross(worldPosition - center, forceVector) * invMass;
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
		CollisionsEnabled = enable;
	}

	public void EnableDrag(bool enable) {
		// Drag handled via damping in the pose integrator; no per-object toggle yet.
	}

	public void EnableGravity(bool enable) {
		GravityEnabled = enable;
	}

	public void EnableMotion(bool enable) {
		if (MotionEnabled == enable)
			return;
		MotionEnabled = enable;
		if (!enable && Body.HasValue) {
			var bodyRef = BodyRef;
			bodyRef.Velocity.Linear = default;
			bodyRef.Velocity.Angular = default;
			bodyRef.Awake = false;
		}
	}

	public CallbackFlags GetCallbackFlags() {
		throw new NotImplementedException();
	}

	public PhysCollide GetCollide() {
		return Collide!;
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
		return Mass;
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

	RigidPose GetPose() {
		if (Body.HasValue)
			return BodyRef.Pose;
		if (Static.HasValue)
			return Sim.Statics[Static.Value].Pose;
		return RigidPose.Identity;
	}

	Vector3 OriginFromPose(in RigidPose pose) => pose.Position - Vector3.Transform(MassCenterOffset, pose.Orientation);

	public void GetPosition(out Vector3 worldPosition, out QAngle angles) {
		RigidPose pose = GetPose();
		worldPosition = IVPConvert.PositionToHL(OriginFromPose(pose));
		MathLib.QuaternionAngles(IVPConvert.RotationToHL(pose.Orientation), out angles);
	}

	public void GetPositionMatrix(out Matrix3x4 positionMatrix) {
		RigidPose pose = GetPose();
		MathLib.QuaternionMatrix(IVPConvert.RotationToHL(pose.Orientation), IVPConvert.PositionToHL(OriginFromPose(pose)), out positionMatrix);
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
		if (Body.HasValue) {
			var vel = BodyRef.Velocity;
			velocity = IVPConvert.PositionToHL(vel.Linear);
			angularVelocity = IVPConvert.AngularToHL(vel.Angular);
		}
		else {
			velocity = default;
			angularVelocity = default;
		}
	}

	public void GetVelocityAtPoint(in Vector3 worldPosition, out Vector3 velocity) {
		throw new NotImplementedException();
	}

	public bool IsAsleep() {
		if (!Body.HasValue)
			return true;
		return !BodyRef.Awake;
	}

	public bool IsAttachedToConstraint(bool externalOnly) {
		throw new NotImplementedException();
	}

	public bool IsCollisionEnabled() {
		return CollisionsEnabled;
	}

	public bool IsDragEnabled() {
		throw new NotImplementedException();
	}

	public bool IsFluid() {
		throw new NotImplementedException();
	}

	public bool IsGravityEnabled() {
		return GravityEnabled;
	}

	public bool IsHinged() {
		throw new NotImplementedException();
	}

	public bool IsMotionEnabled() {
		return MotionEnabled;
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
		MathLib.AngleQuaternion(in angles, out Quaternion orientation);
		Quaternion simOrient = IVPConvert.RotationToIVP(orientation);
		RigidPose pose = new(IVPConvert.PositionToIVP(worldPosition) + Vector3.Transform(MassCenterOffset, simOrient), simOrient);
		if (Body.HasValue) {
			var bodyRef = BodyRef;
			bodyRef.Pose = pose;
			bodyRef.Awake = true;
		}
		else if (Static.HasValue) {
			Sim.Statics[Static.Value].Pose = pose;
		}
	}

	public void SetPositionMatrix(in Matrix3x4 matrix, bool isTeleport) {
		MathLib.MatrixAngles(in matrix, out QAngle angles, out Vector3 position);
		SetPosition(in position, in angles, isTeleport);
	}

	public void SetShadow(float maxSpeed, float maxAngularSpeed, bool allowPhysicsMovement, bool allowPhysicsRotation) {
		throw new NotImplementedException();
	}

	public void SetVelocity(in Vector3 velocity, in Vector3 angularVelocity) {
		if (!Body.HasValue)
			return;
		var bodyRef = BodyRef;
		bodyRef.Velocity.Linear = IVPConvert.PositionToIVP(velocity);
		bodyRef.Velocity.Angular = IVPConvert.AngularToIVP(angularVelocity);
		bodyRef.Awake = true;
	}

	public void SetVelocityInstantaneous(in Vector3 velocity, in Vector3 angularVelocity) {
		SetVelocity(in velocity, in angularVelocity);
	}

	public void Sleep() {
		if (Body.HasValue) {
			var bodyRef = BodyRef;
			bodyRef.Awake = false;
		}
	}

	public void UpdateShadow(in Vector3 targetPosition, in QAngle targetAngles, bool tempDisableGravity, float timeOffset) {
		if (!Body.HasValue)
			return;

		float dt = timeOffset > 1e-4f ? timeOffset : Env.GetSimulationTimestepSeconds();
		GetPosition(out Vector3 current, out _);
		Vector3 velocity = (targetPosition - current) / dt;

		var bodyRef = BodyRef;
		bodyRef.Velocity.Linear = IVPConvert.PositionToIVP(velocity);
		bodyRef.Velocity.Angular = default;
		bodyRef.Awake = true;
	}

	internal void BecomeKinematic() {
		if (Body.HasValue) {
			var bodyRef = BodyRef;
			bodyRef.LocalInertia = default;
			bodyRef.Awake = true;
		}
	}

	public void Wake() {
		if (Body.HasValue) {
			var bodyRef = BodyRef;
			bodyRef.Awake = true;
		}
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
		RigidPose pose = new(IVPConvert.PositionToIVP(position), IVPConvert.RotationToIVP(orientation));

		PhysicsObject obj = new();
		obj.AsleepSinceCreation = true;
		obj.Collide = collisionModel;
		obj.MaterialIndex = (ushort)materialIndex;
		obj.Env = environment;
		obj.GameData = objParams.GameData;

		if (isStatic) {
			var srcTriangles = compactSurface.Triangles;
			int triCount = srcTriangles.Count / 3;
			if (triCount == 0)
				return null;

			pool.Take<Triangle>(triCount, out var tris);
			for (int t = 0; t < triCount; t++) {
				tris[t] = new Triangle(srcTriangles[t * 3 + 0], srcTriangles[t * 3 + 2], srcTriangles[t * 3 + 1]);
			}
			var mesh = new Mesh(tris, Vector3.One, pool);
			TypedIndex shapeIndex = sim.Shapes.Add(mesh);

			obj.Static = sim.Statics.Add(new StaticDescription(
				pose,
				shapeIndex
			));
			obj.ShapeIndex = shapeIndex;
		}
		else {
			float mass = objParams.Mass > 0 ? objParams.Mass : 1f;

			TypedIndex shapeIndex;
			BodyInertia inertia;
			Vector3 center;

			if (hulls.Count == 1) {
				pool.Take<Vector3>(hulls[0].Length, out var bepuVerts);
				for (int i = 0; i < hulls[0].Length; i++)
					bepuVerts[i] = hulls[0][i];
				var hull = new ConvexHull(bepuVerts, pool, out center);
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
					var hull = new ConvexHull(bepuVerts, pool, out var childCenter);
					builder.Add(hull, new RigidPose(childCenter), 1f);
					pool.Return(ref bepuVerts);
				}
				builder.BuildDynamicCompound(out var children, out inertia, out center);
				shapeIndex = sim.Shapes.Add(new Compound(children));
			}

			obj.MassCenterOffset = center;
			pose.Position += Vector3.Transform(center, pose.Orientation);

			var bodyHandle = sim.Bodies.Add(BodyDescription.CreateDynamic(
				pose,
				inertia,
				shapeIndex,
				0.01f
			));

			obj.Body = bodyHandle;
			obj.ShapeIndex = shapeIndex;
			obj.Mass = mass;

			if (objParams.Damping > 0 || objParams.RotDamping > 0) {
				obj.DragCoefficient = objParams.Damping;
				obj.AngDragCoefficient = objParams.RotDamping;
			}
		}

		return obj;
	}

	internal void RemoveFromSimulation(Simulation sim, BufferPool pool) {
		if (Body.HasValue) {
			sim.Bodies.Remove(Body.Value);
			Body = null;
		}
		else if (Static.HasValue) {
			sim.Statics.Remove(Static.Value);
			Static = null;
		}

		if (ShapeIndex.Exists) {
			sim.Shapes.RemoveAndDispose(ShapeIndex, pool);
			ShapeIndex = default;
		}
	}
}
