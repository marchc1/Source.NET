global using static Source.Physics.PhysicsEnvironmentGlobals;

using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.CollisionDetection;
using BepuPhysics.Constraints;

using BepuUtilities;
using BepuUtilities.Memory;

using Source.Common;
using Source.Common.Mathematics;
using Source.Common.Physics;

using System.Numerics;
using System.Runtime.CompilerServices;

namespace Source.Physics;

internal static class PhysicsEnvironmentGlobals
{
	public static IPhysicsEnvironment CreatePhysicsEnvironment() => new PhysicsEnvironment();

	internal static IPhysicsObjectPairHash CreateObjectPairHash() => new ObjectPairHash();
}

internal struct PhysicsNarrowPhaseCallbacks(PhysicsEnvironment env) : INarrowPhaseCallbacks
{
	private readonly PhysicsEnvironment env = env;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool AllowContactGeneration(int workerIndex, CollidableReference a, CollidableReference b, ref float speculativeMargin) {
		return a.Mobility == CollidableMobility.Dynamic || b.Mobility == CollidableMobility.Dynamic;
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool AllowContactGeneration(int workerIndex, CollidablePair pair, int childIndexA, int childIndexB) {
		return true;
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool ConfigureContactManifold<TManifold>(int workerIndex, CollidablePair pair, ref TManifold manifold, out PairMaterialProperties pairMaterial) where TManifold : unmanaged, IContactManifold<TManifold> {
		pairMaterial.FrictionCoefficient = 1f;
		pairMaterial.MaximumRecoveryVelocity = 2f;
		pairMaterial.SpringSettings = new(30, 1);
		return true;
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool ConfigureContactManifold(int workerIndex, CollidablePair pair, int childIndexA, int childIndexB, ref ConvexContactManifold manifold) {
		return true;
	}

	public void Dispose() {

	}

	public void Initialize(Simulation simulation) {

	}
}

internal struct PhysicsPoseIntegratorCallbacks(PhysicsEnvironment env) : IPoseIntegratorCallbacks
{
	public AngularIntegrationMode AngularIntegrationMode => AngularIntegrationMode.Nonconserving;
	public bool AllowSubstepsForUnconstrainedBodies => true;
	public bool IntegrateVelocityForKinematics => false;
	public void Initialize(Simulation simulation) {

	}

	Vector3Wide gravityWideDt;
	Vector<float> linearDampingDt;
	Vector<float> angularDampingDt;

	public void IntegrateVelocity(Vector<int> bodyIndices, Vector3Wide position, QuaternionWide orientation, BodyInertiaWide localInertia, Vector<int> integrationMask, int workerIndex, Vector<float> dt, ref BodyVelocityWide velocity) {
		velocity.Linear = (velocity.Linear + gravityWideDt) * linearDampingDt;
		velocity.Angular = velocity.Angular * angularDampingDt;
	}

	public void PrepareForIntegration(float dt) {
		var damping = env.GetLinearDamping();
		var angDamping = env.GetAngularDamping();
		linearDampingDt = new Vector<float>(MathF.Pow(MathHelper.Clamp(1 - damping, 0, 1), dt));
		angularDampingDt = new Vector<float>(MathF.Pow(MathHelper.Clamp(1 - angDamping, 0, 1), dt));
		env.GetGravity(out var grav);
		gravityWideDt = Vector3Wide.Broadcast(grav * dt);
	}
}

internal class PhysicsEnvironment : IPhysicsEnvironment
{
	readonly Simulation PhysEnv;
	readonly BufferPool BufferPool;

	Vector3 Gravity;
	float AirDensity = 2.0f;
	TimeUnit_t SimulationTimestep = 1.0 / 60.0;
	TimeUnit_t SimulationTime;
	TimeUnit_t NextFrameTime;
	bool InSimulation;
	bool DeleteQueueEnabled = true;
	bool QuickDeleteEnabled;

	PhysicsPerformanceParams PerformanceSettings;
	PhysicsStats Stats;

	readonly List<IPhysicsObject> Objects = [];
	readonly List<IPhysicsObject> DeleteQueue = [];

	IPhysicsCollisionEvent? CollisionEventHandler;
	IPhysicsObjectEvent? ObjectEventHandler;
	IPhysicsConstraintEvent? ConstraintEventHandler;
	IPhysicsCollisionSolver? CollisionSolver;
	IVPhysicsDebugOverlay? DebugOverlay;
	bool ConstraintNotifyEnabled;

	public PhysicsEnvironment() {
		BufferPool = new();
		var narrowPhaseCallbacks = new PhysicsNarrowPhaseCallbacks(this);
		var poseIntegratorCallbacks = new PhysicsPoseIntegratorCallbacks(this);
		var solveDescription = new SolveDescription() {
			SubstepCount = 2,
			VelocityIterationCount = 2
		};
		PhysEnv = Simulation.Create(BufferPool, narrowPhaseCallbacks, poseIntegratorCallbacks, solveDescription);

		PerformanceSettings.Defaults();
	}

	internal float GetLinearDamping() => 0.03f;
	internal float GetAngularDamping() => 0.03f;

	public void GetGravity(out Vector3 gravityVector) {
		gravityVector = Gravity;
	}

	public void SetGravity(in Vector3 gravityVector) {
		Gravity = gravityVector;
	}

	public void SetAirDensity(float density) {
		AirDensity = density;
	}

	public float GetAirDensity() {
		return AirDensity;
	}

	public void SetSimulationTimestep(TimeUnit_t timestep) {
		SimulationTimestep = timestep;
	}

	public TimeUnit_t GetSimulationTimestep() {
		return SimulationTimestep;
	}

	public TimeUnit_t GetSimulationTime() {
		return SimulationTime;
	}

	public void ResetSimulationClock() {
		SimulationTime = default;
		NextFrameTime = default;
	}

	public TimeUnit_t GetNextFrameTime() {
		return NextFrameTime;
	}

	public void Simulate(TimeUnit_t deltaTime) {
		InSimulation = true;

		float dt = (float)SimulationTimestep;
		float remaining = (float)deltaTime;

		while (remaining >= dt) {
			PhysEnv.Timestep(dt);
			SimulationTime += SimulationTimestep;
			remaining -= dt;
		}

		NextFrameTime = SimulationTime + SimulationTimestep;

		CollisionEventHandler?.PostSimulationFrame();

		if (DeleteQueueEnabled) 
			CleanupDeleteList();

		InSimulation = false;
	}

	public bool IsInSimulation() {
		return InSimulation;
	}

	public int GetActiveObjectCount() {
		return PhysEnv.Bodies.ActiveSet.Count;
	}

	public void GetActiveObjects(Span<IPhysicsObject> outputObjectList) {
	}

	public ReadOnlySpan<IPhysicsObject> GetObjectList() {
		return Objects.ToArray();
	}

	public void GetPerformanceSettings(out PhysicsPerformanceParams output) {
		output = PerformanceSettings;
	}

	public void SetPerformanceSettings(in PhysicsPerformanceParams settings) {
		PerformanceSettings = settings;
	}

	public bool TransferObject(IPhysicsObject obj, IPhysicsEnvironment destinationEnvironment) {
		if (obj == null || destinationEnvironment == null)
			return false;

		Objects.Remove(obj);
		return true;
	}

	public void SetCollisionEventHandler(IPhysicsCollisionEvent collisionEvents) {
		CollisionEventHandler = collisionEvents;
	}

	public void SetObjectEventHandler(IPhysicsObjectEvent objectEvents) {
		ObjectEventHandler = objectEvents;
	}

	public void SetConstraintEventHandler(IPhysicsConstraintEvent constraintEvents) {
		ConstraintEventHandler = constraintEvents;
	}

	public void SetCollisionSolver(IPhysicsCollisionSolver solver) {
		CollisionSolver = solver;
	}

	public void SetQuickDelete(bool quick) {
		QuickDeleteEnabled = quick;
	}

	public void EnableDeleteQueue(bool enable) {
		DeleteQueueEnabled = enable;
	}

	public void CleanupDeleteList() {
		for (int i = DeleteQueue.Count - 1; i >= 0; i--) {
			DestroyObject(DeleteQueue[i]);
		}
		DeleteQueue.Clear();
	}

	public void EnableConstraintNotify(bool enable) {
		ConstraintNotifyEnabled = enable;
	}

	public void SetDebugOverlay(IServiceProvider debugOverlayFactory) {
		DebugOverlay = debugOverlayFactory.GetService(typeof(IVPhysicsDebugOverlay)) as IVPhysicsDebugOverlay;
	}

	public IVPhysicsDebugOverlay? GetDebugOverlay() {
		return DebugOverlay;
	}

	public void ReadStats(out PhysicsStats output) {
		output = Stats;
	}

	public void ClearStats() {
		Stats = default;
	}

	public bool IsCollisionModelUsed(PhysCollide collide) {
		for (int i = 0; i < Objects.Count; i++) {
			if (Objects[i].GetCollide() == collide)
				return true;
		}
		return false;
	}

	public void DebugCheckContacts() {
	}

	public uint GetObjectSerializeSize(IPhysicsObject obj) {
		throw new NotImplementedException();
	}

	public void SerializeObjectToBuffer(IPhysicsObject obj, Span<byte> buffer) {
		throw new NotImplementedException();
	}

	public IPhysicsObject UnserializeObjectFromBuffer(object gameData, ReadOnlySpan<byte> buffer, bool enableCollisions) {
		throw new NotImplementedException();
	}

	public bool Save(in PhysSaveParams parms) {
		throw new NotImplementedException();
	}

	public void PreRestore(in PhysPreRestoreParams parms) {
		throw new NotImplementedException();
	}

	public bool Restore(in PhysRestoreParams parms) {
		throw new NotImplementedException();
	}

	public void PostRestore() {
		throw new NotImplementedException();
	}

	public void TraceRay<Filter>(in Ray ray, uint fMask, in Filter traceFilter, out Trace trace) where Filter : IPhysicsTraceFilter {
		throw new NotImplementedException();
	}

	public void SweepCollideable<Filter>(PhysCollide collide, in Vector3 absStart, in Vector3 absEnd, in QAngle angles, uint fMask, in Filter traceFilter, out Trace trace) where Filter : IPhysicsTraceFilter {
		throw new NotImplementedException();
	}

	public IPhysicsConstraint CreateBallsocketConstraint(IPhysicsObject pReferenceObject, IPhysicsObject pAttachedObject, IPhysicsConstraintGroup group, in ConstraintBallSocketParams ballsocket) {
		throw new NotImplementedException();
	}

	public IPhysicsConstraintGroup CreateConstraintGroup(in ConstraintGroupParams groupParams) {
		throw new NotImplementedException();
	}

	public IPhysicsConstraint CreateFixedConstraint(IPhysicsObject pReferenceObject, IPhysicsObject pAttachedObject, IPhysicsConstraintGroup group, in ConstraintFixedParams fixedParams) {
		throw new NotImplementedException();
	}

	public IPhysicsFluidController CreateFluidController(IPhysicsObject pFluidObject, ref FluidParams fluidParams) {
		throw new NotImplementedException();
	}

	public IPhysicsConstraint CreateHingeConstraint(IPhysicsObject pReferenceObject, IPhysicsObject pAttachedObject, IPhysicsConstraintGroup group, in ConstraintHingeParams hinge) {
		throw new NotImplementedException();
	}

	public IPhysicsConstraint CreateLengthConstraint(IPhysicsObject pReferenceObject, IPhysicsObject pAttachedObject, IPhysicsConstraintGroup group, in ConstraintLengthParams length) {
		throw new NotImplementedException();
	}

	public IPhysicsMotionController CreateMotionController(IMotionEvent handler) {
		throw new NotImplementedException();
	}

	public IPhysicsPlayerController CreatePlayerController(IPhysicsObject obj) {
		throw new NotImplementedException();
	}

	public IPhysicsObject CreatePolyObject(PhysCollide pCollisionModel, int materialIndex, in Vector3 position, in QAngle angles, ref ObjectParams objParams) {
		throw new NotImplementedException();
	}

	public IPhysicsObject CreatePolyObjectStatic(PhysCollide pCollisionModel, int materialIndex, in Vector3 position, in QAngle angles, ref ObjectParams objParams) {
		throw new NotImplementedException();
	}

	public IPhysicsConstraint CreatePulleyConstraint(IPhysicsObject pReferenceObject, IPhysicsObject pAttachedObject, IPhysicsConstraintGroup group, in ConstraintPulleyParams pulley) {
		throw new NotImplementedException();
	}

	public IPhysicsConstraint CreateRagdollConstraint(IPhysicsObject pReferenceObject, IPhysicsObject pAttachedObject, IPhysicsConstraintGroup group, in ConstraintRagdollParams ragdoll) {
		throw new NotImplementedException();
	}

	public IPhysicsShadowController CreateShadowController(IPhysicsObject obj, bool allowTranslation, bool allowRotation) {
		throw new NotImplementedException();
	}

	public IPhysicsConstraint CreateSlidingConstraint(IPhysicsObject pReferenceObject, IPhysicsObject pAttachedObject, IPhysicsConstraintGroup group, in ConstraintSlidingParams sliding) {
		throw new NotImplementedException();
	}

	public IPhysicsObject CreateSphereObject(float radius, int materialIndex, in Vector3 position, in QAngle angles, ref ObjectParams objParams, bool isStatic) {
		throw new NotImplementedException();
	}

	public IPhysicsSpring CreateSpring(IPhysicsObject objStart, IPhysicsObject objEnd, ref SpringParams fluidParams) {
		throw new NotImplementedException();
	}

	public IPhysicsVehicleController CreateVehicleController(IPhysicsObject pVehicleBodyObject, in VehicleParams parms, VehicleType vehicleType, IPhysicsGameTrace gameTrace) {
		throw new NotImplementedException();
	}

	public void DestroyConstraint(IPhysicsConstraint constraint) {
		throw new NotImplementedException();
	}

	public void DestroyConstraintGroup(IPhysicsConstraintGroup group) {
		throw new NotImplementedException();
	}

	public void DestroyFluidController(IPhysicsFluidController fluidController) {
		throw new NotImplementedException();
	}

	public void DestroyMotionController(IPhysicsMotionController controller) {
		throw new NotImplementedException();
	}

	public void DestroyObject(IPhysicsObject obj) {
		throw new NotImplementedException();
	}

	public void DestroyPlayerController(IPhysicsPlayerController controller) {
		throw new NotImplementedException();
	}

	public void DestroyShadowController(IPhysicsShadowController controller) {
		throw new NotImplementedException();
	}

	public void DestroySpring(IPhysicsSpring spring) {
		throw new NotImplementedException();
	}

	public void DestroyVehicleController(IPhysicsVehicleController controller) {
		throw new NotImplementedException();
	}
}

public class ObjectPairHash : IPhysicsObjectPairHash
{
	readonly Dictionary<object, HashSet<object>> PairMap = [];

	public void AddObjectPair(object obj0, object obj1) {
		if (!PairMap.TryGetValue(obj0, out var set0)) {
			set0 = [];
			PairMap[obj0] = set0;
		}
		set0.Add(obj1);

		if (!PairMap.TryGetValue(obj1, out var set1)) {
			set1 = [];
			PairMap[obj1] = set1;
		}
		set1.Add(obj0);
	}

	public int GetPairCountForObject(object obj0) {
		if (PairMap.TryGetValue(obj0, out var set))
			return set.Count;
		return 0;
	}

	public int GetPairListForObject(object obj0, int maxCount, Span<object> objectList) {
		if (!PairMap.TryGetValue(obj0, out var set))
			return 0;

		int count = 0;
		foreach (var item in set) {
			if (count >= maxCount)
				break;
			objectList[count++] = item;
		}
		return count;
	}

	public bool IsObjectInHash(object obj0) {
		return PairMap.ContainsKey(obj0);
	}

	public bool IsObjectPairInHash(object obj0, object obj1) {
		return PairMap.TryGetValue(obj0, out var set) && set.Contains(obj1);
	}

	public void RemoveAllPairsForObject(object obj0) {
		if (!PairMap.TryGetValue(obj0, out var set))
			return;

		foreach (var other in set) {
			if (PairMap.TryGetValue(other, out var otherSet))
				otherSet.Remove(obj0);
		}
		PairMap.Remove(obj0);
	}

	public void RemoveObjectPair(object obj0, object obj1) {
		if (PairMap.TryGetValue(obj0, out var set0))
			set0.Remove(obj1);
		if (PairMap.TryGetValue(obj1, out var set1))
			set1.Remove(obj0);
	}
}
