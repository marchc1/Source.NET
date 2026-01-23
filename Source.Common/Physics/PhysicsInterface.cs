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

public struct SurfacePhysicsParams{
	public float Friction;
	public float Elasticity;
	public float Density;
	public float Thickness;
	public float Dampening;
}

public struct SurfaceAudioParams
{
	public float Reflectivity;
	public float HardnessFactor;
	public float RoughnessFactor;
	public float RoughThreshold;
	public float HardThreshold;
	public float HardVelocityThreshold;
}

public struct SurfaceSoundNames{
	public ushort StepLeft;
	public ushort StepRight;
	public ushort ImpactSoft;
	public ushort ImpactHard;
	public ushort ScrapeSmooth;
	public ushort ScrapeRough;
	public ushort BulletImpact;
	public ushort Rolling;
	public ushort BreakSound;
	public ushort StrainSound;
}

public struct SurfaceGameProps{
	public float MaxSpeedFactor;
	public float JumpFactor;
	public ushort Material;
	public byte Climbable;
	public byte Pad;
}

public struct SurfaceSoundHandles{
	public short StepLeft;
	public short StepRight;
	public short ImpactSoft;
	public short ImpactHard;
	public short ScrapeSmooth;
	public short ScrapeRough;
	public short BulletImpact;
	public short Rolling;
	public short BreakSound;
	public short StrainSound;
}

public struct SurfaceData {
	public SurfacePhysicsParams Physics;
	public SurfaceAudioParams Audio;
	public SurfaceSoundNames Sounds;
	public SurfaceGameProps Game;
	public SurfaceSoundHandles SoundHandles;
}

public class SurfaceData_ptr : ReusableBox<SurfaceData>{
	public ref SurfacePhysicsParams Physics => ref Struct.Physics;
	public ref SurfaceAudioParams Audio => ref Struct.Audio;
	public ref SurfaceSoundNames Sounds => ref Struct.Sounds;
	public ref SurfaceGameProps Game => ref Struct.Game;
	public ref SurfaceSoundHandles SoundHandles => ref Struct.SoundHandles;
}
