using Source.Common.Physics;

namespace Game.Server;

public class CollisionEvent : IPhysicsCollisionEvent, IPhysicsCollisionSolver, IPhysicsObjectEvent
{
	public int AdditionalCollisionChecksThisTick(int currentChecksDone) {
		throw new NotImplementedException();
	}

	public void EndTouch(IPhysicsObject obj1, IPhysicsObject obj2, IPhysicsCollisionData toichData) {
		throw new NotImplementedException();
	}

	public void FluidEndTouch(IPhysicsObject obj, IPhysicsFluidController fluid) {
		throw new NotImplementedException();
	}

	public void FluidStartTouch(IPhysicsObject obj, IPhysicsFluidController fluid) {
		throw new NotImplementedException();
	}

	public void Friction(IPhysicsObject obj, float energy, int surfaceProps, int surfacePropsHit, IPhysicsCollisionData data) {
		throw new NotImplementedException();
	}

	public void ObjectSleep(IPhysicsObject obj) {
		throw new NotImplementedException();
	}

	public void ObjectWake(IPhysicsObject obj) {
		throw new NotImplementedException();
	}

	public void PostCollision(ref VCollisionEvent ev) {
		throw new NotImplementedException();
	}

	public void PostSimulationFrame() {
		throw new NotImplementedException();
	}

	public void PreCollision(ref VCollisionEvent ev) {
		throw new NotImplementedException();
	}

	public int ShouldCollide(IPhysicsObject obj0, IPhysicsObject obj1, object gameData0, object gameData1) {
		throw new NotImplementedException();
	}

	public bool ShouldFreezeContacts(Span<IPhysicsObject> objectList) {
		throw new NotImplementedException();
	}

	public bool ShouldFreezeObject(IPhysicsObject obj) {
		throw new NotImplementedException();
	}

	public int ShouldSolvePenetration(IPhysicsObject obj0, IPhysicsObject obj1, object gameData0, object gameData1, double dt) {
		throw new NotImplementedException();
	}

	public void StartTouch(IPhysicsObject obj1, IPhysicsObject obj2, IPhysicsCollisionData touchData) {
		throw new NotImplementedException();
	}
}
