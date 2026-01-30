using Source.Common.Physics;

namespace Source.Physics;

public class PhysicsInterface : IPhysics
{
	public IPhysicsEnvironment CreateEnvironment() {
		throw new NotImplementedException();
	}

	public IPhysicsObjectPairHash CreateObjectPairHash() {
		throw new NotImplementedException();
	}

	public void DestroyAllCollisionSets() {
		throw new NotImplementedException();
	}

	public void DestroyEnvironment(IPhysicsEnvironment? env) {
		throw new NotImplementedException();
	}

	public void DestroyObjectPairHash(IPhysicsObjectPairHash hash) {
		throw new NotImplementedException();
	}

	public IPhysicsCollisionSet FindCollisionSet(uint id) {
		throw new NotImplementedException();
	}

	public IPhysicsCollisionSet FindOrCreateCollisionSet(uint id, int maxElementCount) {
		throw new NotImplementedException();
	}

	public IPhysicsEnvironment? GetActiveEnvironmentByIndex(int index) {
		throw new NotImplementedException();
	}
}
