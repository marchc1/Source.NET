global using static Source.Physics.PhysicsEnvironmentGlobals;

using Source.Common;
using Source.Common.Mathematics;
using Source.Common.Physics;

namespace Source.Physics;

internal static class PhysicsEnvironmentGlobals
{
	public static IPhysicsEnvironment CreatePhysicsEnvironment() => new PhysicsEnvironment();

	internal static IPhysicsObjectPairHash CreateObjectPairHash() => new ObjectPairHash();
}

internal class PhysicsEnvironment : IPhysicsEnvironment
{

	public void CleanupDeleteList() {
		throw new NotImplementedException();
	}

	public void ClearStats() {
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

	public IPhysicsObject CreatePolyObject(PhysCollide pCollisionModel, int materialIndex, in AngularImpulse position, in QAngle angles, ref ObjectParams objParams) {
		throw new NotImplementedException();
	}

	public IPhysicsObject CreatePolyObjectStatic(PhysCollide pCollisionModel, int materialIndex, in AngularImpulse position, in QAngle angles, ref ObjectParams objParams) {
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

	public IPhysicsObject CreateSphereObject(float radius, int materialIndex, in AngularImpulse position, in QAngle angles, ref ObjectParams objParams, bool isStatic) {
		throw new NotImplementedException();
	}

	public IPhysicsSpring CreateSpring(IPhysicsObject objStart, IPhysicsObject objEnd, ref SpringParams fluidParams) {
		throw new NotImplementedException();
	}

	public IPhysicsVehicleController CreateVehicleController(IPhysicsObject pVehicleBodyObject, in VehicleParams parms, VehicleType vehicleType, IPhysicsGameTrace gameTrace) {
		throw new NotImplementedException();
	}

	public void DebugCheckContacts() {
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

	public void EnableConstraintNotify(bool enable) {
		throw new NotImplementedException();
	}

	public void EnableDeleteQueue(bool enable) {
		throw new NotImplementedException();
	}

	public int GetActiveObjectCount() {
		throw new NotImplementedException();
	}

	public void GetActiveObjects(Span<IPhysicsObject> outputObjectList) {
		throw new NotImplementedException();
	}

	public float GetAirDensity() {
		throw new NotImplementedException();
	}

	public IVPhysicsDebugOverlay? GetDebugOverlay() {
		throw new NotImplementedException();
	}

	public void GetGravity(out AngularImpulse gravityVector) {
		throw new NotImplementedException();
	}

	public float GetNextFrameTime() {
		throw new NotImplementedException();
	}

	public ReadOnlySpan<IPhysicsObject> GetObjectList() {
		throw new NotImplementedException();
	}

	public uint GetObjectSerializeSize(IPhysicsObject obj) {
		throw new NotImplementedException();
	}

	public void GetPerformanceSettings(out PhysicsPerformanceParams output) {
		throw new NotImplementedException();
	}

	public float GetSimulationTime() {
		throw new NotImplementedException();
	}

	public float GetSimulationTimestep() {
		throw new NotImplementedException();
	}

	public bool IsCollisionModelUsed(PhysCollide collide) {
		throw new NotImplementedException();
	}

	public bool IsInSimulation() {
		throw new NotImplementedException();
	}

	public void PostRestore() {
		throw new NotImplementedException();
	}

	public void PreRestore(in PhysPreRestoreParams parms) {
		throw new NotImplementedException();
	}

	public void ReadStats(out PhysicsStats output) {
		throw new NotImplementedException();
	}

	public void ResetSimulationClock() {
		throw new NotImplementedException();
	}

	public bool Restore(in PhysRestoreParams parms) {
		throw new NotImplementedException();
	}

	public bool Save(in PhysSaveParams parms) {
		throw new NotImplementedException();
	}

	public void SerializeObjectToBuffer(IPhysicsObject obj, Span<byte> buffer) {
		throw new NotImplementedException();
	}

	public void SetAirDensity(float density) {
		throw new NotImplementedException();
	}

	public void SetCollisionEventHandler(IPhysicsCollisionEvent collisionEvents) {
		throw new NotImplementedException();
	}

	public void SetCollisionSolver(IPhysicsCollisionSolver solver) {
		throw new NotImplementedException();
	}

	public void SetConstraintEventHandler(IPhysicsConstraintEvent constraintEvents) {
		throw new NotImplementedException();
	}

	public void SetDebugOverlay(IServiceProvider debugOverlayFactory) {
		throw new NotImplementedException();
	}

	public void SetGravity(in AngularImpulse gravityVector) {

	}

	public void SetObjectEventHandler(IPhysicsObjectEvent objectEvents) {
		throw new NotImplementedException();
	}

	public void SetPerformanceSettings(in PhysicsPerformanceParams settings) {
		throw new NotImplementedException();
	}

	public void SetQuickDelete(bool quick) {
		throw new NotImplementedException();
	}

	public void SetSimulationTimestep(double timestep) {

	}

	public void Simulate(double deltaTime) {
		throw new NotImplementedException();
	}

	public void SweepCollideable<Filter>(PhysCollide collide, in AngularImpulse absStart, in AngularImpulse absEnd, in QAngle angles, uint fMask, in Filter traceFilter, out Trace trace) where Filter : IPhysicsTraceFilter {
		throw new NotImplementedException();
	}

	public void TraceRay<Filter>(in Ray ray, uint fMask, in Filter traceFilter, out Trace trace) where Filter : IPhysicsTraceFilter {
		throw new NotImplementedException();
	}

	public bool TransferObject(IPhysicsObject obj, IPhysicsEnvironment destinationEnvironment) {
		throw new NotImplementedException();
	}

	public IPhysicsObject UnserializeObjectFromBuffer(object gameData, ReadOnlySpan<byte> buffer, bool enableCollisions) {
		throw new NotImplementedException();
	}
}

public class ObjectPairHash : IPhysicsObjectPairHash
{
	public void AddObjectPair(object object0, object obj1) {
		throw new NotImplementedException();
	}

	public int GetPairCountForObject(object obj0) {
		throw new NotImplementedException();
	}

	public int GetPairListForObject(object obj0, int maxCount, Span<object> objectList) {
		throw new NotImplementedException();
	}

	public bool IsObjectInHash(object obj0) {
		throw new NotImplementedException();
	}

	public bool IsObjectPairInHash(object obj0, object obj1) {
		throw new NotImplementedException();
	}

	public void RemoveAllPairsForObject(object obj0) {
		throw new NotImplementedException();
	}

	public void RemoveObjectPair(object obj0, object obj1) {
		throw new NotImplementedException();
	}
}
