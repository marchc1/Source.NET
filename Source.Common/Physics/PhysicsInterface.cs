namespace Source.Common.Physics;

// Barebones interfaces for where they're needed

public static class PhysicsConversions
{
	public const float POUNDS_PER_KG = 2.2f;
	public const float KG_PER_POUND = 1.0f / POUNDS_PER_KG;

	public static float lbs2kg(float x) => x * KG_PER_POUND;
	public static float kg2lbs(float x) => x * POUNDS_PER_KG;
}

public interface IVPhysicsDebugOverlay;
public interface IPhysics;
public interface IPhysicsCollision;
public interface ICollisionQuery;
public interface IPhysicsGameTrace;
public interface IConvexInfo;
public interface IPhysicsCollisionData;
public interface IPhysicsCollisionEvent;
public interface IPhysicsShadowController;
public interface IPhysicsMotionController;
public interface IPhysicsCollisionSolver;
public interface IPhysicsTraceFilter;
public interface IPhysicsEnvironment;
public interface IPhysicsObject;
public interface IPhysicsSpring;
public interface IPhysicsSurfaceProps;
public interface IPhysicsFluidController;
