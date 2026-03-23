using Source.Common.Formats.BSP;
using Source.Common.Mathematics;

using System.Numerics;

namespace Source.Common.Engine;

public interface ICollideable
{
	IHandleEntity? GetEntityHandle();

	// These methods return the bounds of an OBB measured in "collision" space
	// which can be retreived through the CollisionToWorldTransform or
	// GetCollisionOrigin/GetCollisionAngles methods
	ref Vector3 OBBMinsPreScaled();
	ref Vector3 OBBMaxsPreScaled();
	ref Vector3 OBBMins();
	ref Vector3 OBBMaxs();

	// Returns the bounds of a world-space box used when the collideable is being traced
	// against as a trigger. It's only valid to call these methods if the solid flags
	// have the FSOLID_USE_TRIGGER_BOUNDS flag set.
	void WorldSpaceTriggerBounds(out Vector3 vecWorldMins, out Vector3 vecWorldMaxs);

	// custom collision test
	bool TestCollision( in Ray ray, Contents contentsMask, ref Trace tr );

	// Perform hitbox test, returns true *if hitboxes were tested at all*!!
	bool TestHitboxes(in Ray ray, Contents contentsMask, ref Trace tr );

	// Returns the BRUSH model index if this is a brush model. Otherwise, returns -1.
	int GetCollisionModelIndex();

	// Return the model, if it's a studio model.
	Model? GetCollisionModel();

	// Get angles and origin.
	ref readonly Vector3 GetCollisionOrigin();
	ref readonly QAngle GetCollisionAngles();
	ref readonly Matrix3x4 CollisionToWorldTransform();

	SolidType GetSolid();
	int GetSolidFlags();

	// Gets at the containing class...
	IClientUnknown? GetIClientUnknown();

	// We can filter out collisions based on collision group
	int GetCollisionGroup();

	// Returns a world-aligned box guaranteed to surround *everything* in the collision representation
	// Note that this will surround hitboxes, trigger bounds, physics.
	// It may or may not be a tight-fitting box and its volume may suddenly change
	void WorldSpaceSurroundingBounds(out Vector3 vecMins, out Vector3 vecMaxs);

	bool ShouldTouchTrigger(int triggerSolidFlags);

	// returns NULL unless this collideable has specified FSOLID_ROOT_PARENT_ALIGNED
	ref readonly Matrix3x4 GetRootParentToWorldTransform();
}
