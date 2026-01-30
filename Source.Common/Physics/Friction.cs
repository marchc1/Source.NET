using System.Numerics;

namespace Source.Common.Physics;

public interface IPhysicsFrictionSnapshot {
	bool IsValid();

	// Object 0 is this object, Object 1 is the other object
	IPhysicsObject? GetObject(int index);
	int GetMaterial(int index);

	void GetContactPoint(out Vector3 vec);
	
	// points away from source object
	void GetSurfaceNormal(out Vector3 vec);
	float GetNormalForce();
	float GetEnergyAbsorbed();

	// recompute friction (useful if dynamically altering materials/mass)
	void RecomputeFriction();
	// clear all friction force at this contact point
	void ClearFrictionForce();

	void MarkContactForDelete();
	void DeleteAllMarkedContacts(bool wakeObjects);

	// Move to the next friction data for this object
	void NextFrictionData();
	float GetFrictionCoefficient();
}
