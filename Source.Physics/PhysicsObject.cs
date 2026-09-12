using Jitter2;
using Jitter2.Collision.Shapes;
using Jitter2.Dynamics;
using Jitter2.LinearMath;

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
	public RigidBody? Body;
	public PhysCollide? Collide;
	public IPhysicsShadowController? Shadow;

	internal PhysicsEnvironment Env = null!;
	internal Vector3 MassCenterOffset;
	internal float Mass = 1.0f;
	internal bool MotionEnabled = true;
	internal bool GravityEnabled = true;
	internal bool CollisionsEnabled = true;

	World World => Env.GetJitterWorld();

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

	Vector3 SimPosition => Body != null ? JitterConvert.ToVec(Body.Position) : default;
	Quaternion SimOrientation => Body != null ? JitterConvert.ToQuat(Body.Orientation) : Quaternion.Identity;

	Vector3 OriginFromPose(in Vector3 simPos, in Quaternion simOrient) => simPos - Vector3.Transform(MassCenterOffset, simOrient);

	public void AddVelocity(in Vector3 velocity, in Vector3 angularVelocity) {
		if (Body == null || Body.MotionType == MotionType.Static)
			return;
		Body.Velocity += JitterConvert.ToJ(IVPConvert.PositionToIVP(velocity));
		Body.AngularVelocity += JitterConvert.ToJ(IVPConvert.AngularToIVP(angularVelocity));
		Body.SetActivationState(true);
	}

	public void ApplyForceCenter(in Vector3 forceVector) {
		if (Body == null)
			return;
		JVector impulse = JitterConvert.ToJ(IVPConvert.ForceImpulseToIVP(forceVector) * Env.GetSimulationTimestepSeconds());
		Body.ApplyImpulse(impulse, true);
	}

	public void ApplyForceOffset(in Vector3 forceVector, in Vector3 worldPosition) {
		if (Body == null)
			return;
		JVector impulse = JitterConvert.ToJ(IVPConvert.ForceImpulseToIVP(forceVector) * Env.GetSimulationTimestepSeconds());
		JVector worldPos = JitterConvert.ToJ(IVPConvert.PositionToIVP(worldPosition));
		Body.ApplyImpulse(impulse, worldPos, true);
	}

	public void ApplyTorqueCenter(in Vector3 torque) {
		if (Body == null || Body.MotionType == MotionType.Static)
			return;
		Body.Torque += JitterConvert.ToJ(IVPConvert.AngularToIVP(torque));
		Body.SetActivationState(true);
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
		GetPosition(out Vector3 center, out _);
		centerForce = forceVector;
		centerTorque = Vector3.Cross(worldPosition - center, forceVector);
	}

	public float CalculateLinearDrag(in Vector3 unitDirection) {
		throw new NotImplementedException();
	}

	public void CalculateVelocityOffset(in Vector3 forceVector, in Vector3 worldPosition, out Vector3 centerVelocity, out Vector3 centerAngularVelocity) {
		GetPosition(out Vector3 center, out _);
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

	}

	public void EnableGravity(bool enable) {
		GravityEnabled = enable;
		if (Body != null)
			Body.AffectedByGravity = enable;
	}

	public void EnableMotion(bool enable) {
		if (MotionEnabled == enable)
			return;
		MotionEnabled = enable;
		if (!enable && Body != null && Body.MotionType != MotionType.Static) {
			Body.Velocity = JVector.Zero;
			Body.AngularVelocity = JVector.Zero;
			Body.SetActivationState(false);
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

	public void GetPosition(out Vector3 worldPosition, out QAngle angles) {
		Vector3 simPos = SimPosition;
		Quaternion simOrient = SimOrientation;
		worldPosition = IVPConvert.PositionToHL(OriginFromPose(simPos, simOrient));
		MathLib.QuaternionAngles(IVPConvert.RotationToHL(simOrient), out angles);
	}

	public void GetPositionMatrix(out Matrix3x4 positionMatrix) {
		Vector3 simPos = SimPosition;
		Quaternion simOrient = SimOrientation;
		MathLib.QuaternionMatrix(IVPConvert.RotationToHL(simOrient), IVPConvert.PositionToHL(OriginFromPose(simPos, simOrient)), out positionMatrix);
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
		if (Body != null && Body.MotionType != MotionType.Static) {
			velocity = IVPConvert.PositionToHL(JitterConvert.ToVec(Body.Velocity));
			angularVelocity = IVPConvert.AngularToHL(JitterConvert.ToVec(Body.AngularVelocity));
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
		if (Body == null)
			return true;
		return !Body.IsActive;
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

	public bool IsStatic() => Body == null || Body.MotionType == MotionType.Static;

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
		if (Body == null)
			return;
		MathLib.AngleQuaternion(in angles, out Quaternion orientation);
		Quaternion simOrient = IVPConvert.RotationToIVP(orientation);
		Vector3 simPos = IVPConvert.PositionToIVP(worldPosition) + Vector3.Transform(MassCenterOffset, simOrient);
		Body.Orientation = JitterConvert.ToJ(simOrient);
		Body.Position = JitterConvert.ToJ(simPos);
		if (Body.MotionType != MotionType.Static)
			Body.SetActivationState(true);
	}

	public void SetPositionMatrix(in Matrix3x4 matrix, bool isTeleport) {
		MathLib.MatrixAngles(in matrix, out QAngle angles, out Vector3 position);
		SetPosition(in position, in angles, isTeleport);
	}

	public void SetShadow(float maxSpeed, float maxAngularSpeed, bool allowPhysicsMovement, bool allowPhysicsRotation) {
		throw new NotImplementedException();
	}

	public void SetVelocity(in Vector3 velocity, in Vector3 angularVelocity) {
		if (Body == null || Body.MotionType == MotionType.Static)
			return;
		Body.Velocity = JitterConvert.ToJ(IVPConvert.PositionToIVP(velocity));
		Body.AngularVelocity = JitterConvert.ToJ(IVPConvert.AngularToIVP(angularVelocity));
		Body.SetActivationState(true);
	}

	public void SetVelocityInstantaneous(in Vector3 velocity, in Vector3 angularVelocity) {
		SetVelocity(in velocity, in angularVelocity);
	}

	public void Sleep() {
		if (Body != null && Body.MotionType != MotionType.Static)
			Body.SetActivationState(false);
	}

	public void UpdateShadow(in Vector3 targetPosition, in QAngle targetAngles, bool tempDisableGravity, float timeOffset) {
		if (Body == null)
			return;

		float dt = timeOffset > 1e-4f ? timeOffset : Env.GetSimulationTimestepSeconds();
		GetPosition(out Vector3 current, out _);
		Vector3 velocity = (targetPosition - current) / dt;

		Body.Velocity = JitterConvert.ToJ(IVPConvert.PositionToIVP(velocity));
		Body.AngularVelocity = JVector.Zero;
		Body.SetActivationState(true);
	}

	internal void BecomeKinematic() {
		if (Body == null)
			return;
		Body.MotionType = MotionType.Kinematic;
		Body.SetActivationState(true);
	}

	public void Wake() {
		if (Body != null && Body.MotionType != MotionType.Static)
			Body.SetActivationState(true);
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

		World world = environment.GetJitterWorld();

		MathLib.AngleQuaternion(in angles, out Quaternion orientation);
		Quaternion simOrient = IVPConvert.RotationToIVP(orientation);
		Vector3 simPos = IVPConvert.PositionToIVP(position);

		physprops.GetPhysicsProperties(materialIndex, out _, out _, out float matFriction, out float matElasticity);

		PhysicsObject obj = new();
		obj.AsleepSinceCreation = true;
		obj.Collide = collisionModel;
		obj.MaterialIndex = (ushort)materialIndex;
		obj.Env = environment;
		obj.GameData = objParams.GameData;

		RigidBody body = world.CreateRigidBody();
		body.Tag = obj;
		body.Friction = matFriction;
		body.Restitution = matElasticity;

		if (isStatic) {
			var srcTriangles = compactSurface.Triangles;
			int triCount = srcTriangles.Count / 3;
			if (triCount == 0) {
				world.Remove(body);
				return null;
			}

			JTriangle[] soup = new JTriangle[triCount];
			for (int t = 0; t < triCount; t++) {
				soup[t] = new JTriangle(
					JitterConvert.ToJ(srcTriangles[t * 3 + 0]),
					JitterConvert.ToJ(srcTriangles[t * 3 + 1]),
					JitterConvert.ToJ(srcTriangles[t * 3 + 2]));
			}

			var mesh = new TriangleMesh(soup, ignoreDegenerated: true);
			body.AddShapes(TriangleShape.CreateAllShapes(mesh), MassInertiaUpdateMode.Preserve);
			body.Position = JitterConvert.ToJ(simPos);
			body.Orientation = JitterConvert.ToJ(simOrient);
			body.MotionType = MotionType.Static;
			obj.MassCenterOffset = Vector3.Zero;
			obj.Body = body;
			return obj;
		}
		else {
			var hulls = compactSurface.ConvexHulls;
			if (hulls.Count == 0) {
				world.Remove(body);
				return null;
			}

			float mass = Math.Clamp(objParams.Mass > 0 ? objParams.Mass : 1f, PhysicsConstants.VPHYSICS_MIN_MASS, PhysicsConstants.VPHYSICS_MAX_MASS);

			var shapes = new List<PointCloudShape>(hulls.Count);
			Vector3 combinedCom = default;
			float totalMass = 0f;

			foreach (var hullVerts in hulls) {
				PointCloudShape? shape = BuildHullShape(hullVerts);
				if (shape == null)
					continue;

				shape.CalculateMassInertia(out _, out JVector c, out double sm);
				Vector3 com = JitterConvert.ToVec(c);
				combinedCom += com * (float)sm;
				totalMass += (float)sm;
				shapes.Add(shape);
			}

			if (shapes.Count == 0) {
				world.Remove(body);
				return null;
			}

			combinedCom /= totalMass > 0 ? totalMass : 1f;

			JVector shift = JitterConvert.ToJ(-combinedCom);
			foreach (var shape in shapes)
				shape.Shift = shift;

			body.AddShapes(shapes);
			body.SetMassInertia(mass);
			body.Damping = (objParams.Damping, objParams.RotDamping);

			obj.MassCenterOffset = combinedCom;
			Vector3 bodyPos = simPos + Vector3.Transform(combinedCom, simOrient);
			body.Position = JitterConvert.ToJ(bodyPos);
			body.Orientation = JitterConvert.ToJ(simOrient);

			obj.Mass = mass;

			if (objParams.Damping > 0 || objParams.RotDamping > 0) {
				obj.DragCoefficient = objParams.Damping;
				obj.AngDragCoefficient = objParams.RotDamping;
			}
		}

		obj.Body = body;
		return obj;
	}

	static PointCloudShape? BuildHullShape(Vector3[] hullVerts) {
		if (hullVerts.Length == 0)
			return null;

		JVector[] pts = new JVector[hullVerts.Length];
		for (int i = 0; i < hullVerts.Length; i++)
			pts[i] = JitterConvert.ToJ(hullVerts[i]);

		try {
			return new PointCloudShape(pts);
		}
		catch (InvalidOperationException) {
		}

		Vector3 mn = hullVerts[0], mx = hullVerts[0];
		for (int i = 1; i < hullVerts.Length; i++) {
			mn = Vector3.Min(mn, hullVerts[i]);
			mx = Vector3.Max(mx, hullVerts[i]);
		}
		Vector3 ext = mx - mn;

		const float depth = 0.5f;
		Vector3 extrude = ext.X <= ext.Y && ext.X <= ext.Z ? new Vector3(depth, 0, 0)
			: ext.Y <= ext.Z ? new Vector3(0, depth, 0)
			: new Vector3(0, 0, depth);
		if (extrude.Z > 0)
			extrude = -extrude;

		JVector[] pts2 = new JVector[hullVerts.Length * 2];
		for (int i = 0; i < hullVerts.Length; i++) {
			pts2[i] = pts[i];
			pts2[hullVerts.Length + i] = JitterConvert.ToJ(hullVerts[i] + extrude);
		}

		try {
			return new PointCloudShape(pts2);
		}
		catch (InvalidOperationException) {
			return null;
		}
	}

	internal void RemoveFromSimulation(World world) {
		if (Body != null) {
			world.Remove(Body);
			Body = null;
		}
	}
}
