namespace Source.Common.Physics;

// Barebones interfaces for where they're needed

public interface IPhysicsCollisionSet {
	void EnableCollisions(int index0, int index1);
	void DisableCollisions(int index0, int index1);
	bool ShouldCollide(int index0, int index1);
}
