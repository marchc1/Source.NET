namespace Source.Common.Physics;

public static class PhysicsPerformanceConstants
{
	public const float k_flMaxVelocity = 2000.0f;
	public const float k_flMaxAngularVelocity = 360.0f * 10.0f;
	public const float DEFAULT_MIN_FRICTION_MASS = 10.0f;
	public const float DEFAULT_MAX_FRICTION_MASS = 2500.0f;
}

public struct PhysicsPerformanceParams
{
	public int MaxCollisionsPerObjectPerTimestep;      // object will be frozen after this many collisions (visual hitching vs. CPU cost)
	public int MaxCollisionChecksPerTimestep;          // objects may penetrate after this many collision checks (can be extended in AdditionalCollisionChecksThisTick)
	public float MaxVelocity;                          // limit world space linear velocity to this (in / s)
	public float MaxAngularVelocity;                   // limit world space angular velocity to this (degrees / s)
	public float LookAheadTimeObjectsVsWorld;          // predict collisions this far (seconds) into the future
	public float LookAheadTimeObjectsVsObject;         // predict collisions this far (seconds) into the future
	public float MinFrictionMass;                      // min mass for friction solves (constrains dynamic range of mass to improve stability)
	public float MaxFrictionMass;                      // mas mass for friction solves

	public void Defaults() {
		MaxCollisionsPerObjectPerTimestep = 6;
		MaxCollisionChecksPerTimestep = 250;
		MaxVelocity = k_flMaxVelocity;
		MaxAngularVelocity = k_flMaxAngularVelocity;
		LookAheadTimeObjectsVsWorld = 1.0f;
		LookAheadTimeObjectsVsObject = 0.5f;
		MinFrictionMass = DEFAULT_MIN_FRICTION_MASS;
		MaxFrictionMass = DEFAULT_MAX_FRICTION_MASS;
	}
}
