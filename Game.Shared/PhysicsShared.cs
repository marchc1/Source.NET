global using static Game.Shared.PhysicsSharedGlobals;

using Source;
using Source.Common;
using Source.Common.Physics;
namespace Game.Shared;

[EngineComponent]
public static class PhysicsSharedGlobals
{
	[Dependency] public static IPhysics physics { get; set; } = null!;
	[Dependency] public static IPhysicsCollision physcollision { get; set; } = null!;
	[Dependency] public static IPhysicsSurfaceProps physprops { get; set; } = null!;


	public static IPhysicsObject g_PhysWorldObject = null!;
	public static IPhysicsEnvironment physenv = null!;
#if PORTAL
	public static IPhysicsEnvironment physenv_main = null!;
#endif
	public static IPhysicsObjectPairHash g_EntityCollisionHash = null!;


	public static readonly ObjectParams g_PhysDefaultObjectParams = new() {
		MassCenterOverrideFn = null,
		Mass = 1.0f,
		Inertia = 1.0f,
		Damping = 0.1f,
		RotDamping = 0.1f,
		RotInertiaLimit = 0.05f,
		Name = "DEFAULT",
		GameData = null,
		Volume = 0f,
		DragCoefficient = 1.0f,
		EnableCollisions = true
	};

	static IGameSystem physicsGameSystem = null!;
	public static void SetPhysicsGameSystem(IGameSystem system) => physicsGameSystem = system;
	public static IGameSystem PhysicsGameSystem() => physicsGameSystem;

#if CLIENT_DLL || GAME_DLL
	public static IPhysicsObject? PhysCreateWorld_Shared(BaseEntity world, VCollide? worldCollide, in ObjectParams defaultParams) {
		Solid solid;
		Fluid fluid;

		if (physenv == null) return null;
		if (worldCollide == null) return null;

		int surfaceData = (int)physprops.GetSurfaceIndex("default");

		ObjectParams oparams = defaultParams;
		oparams.GameData = world;
		oparams.Name = "world";

		IPhysicsObject? worldPhysics = physenv.CreatePolyObjectStatic(worldCollide.Solids![0]!, surfaceData, vec3_origin, vec3_angle, ref oparams);

		return null;
	}
#endif
}

public enum PhysicsFlags : ushort
{
	///<summary> does slice damage, not just blunt damage </summary>
	DamageSlice = 0x0001,

	///<summary> object is constrained to the world, so it should behave like a static </summary>
	ConstraintStatic = 0x0002,

	///<summary> object is held by the player, so have a very inelastic collision response </summary>
	PlayerHeld = 0x0004,

	///<summary> object is part of a client or server ragdoll </summary>
	PartOfRagdoll = 0x0008,

	///<summary> object is part of a multi-object entity </summary>
	MultiObjectEntity = 0x0010,

	///<summary> HULK SMASH! (Do large damage even if the mass is small) </summary>
	HeavyObject = 0x0020,

	///<summary> This object is currently stuck inside another object </summary>
	Penetrating = 0x0040,

	///<summary> Player can't pick this up for some game rule reason </summary>
	NoPlayerPickup = 0x0080,

	///<summary> Player threw this object </summary>
	WasThrown = 0x0100,

	///<summary> does dissolve damage, not just blunt damage </summary>
	DamageDissolve = 0x0200,

	///<summary> don't do impact damage to anything </summary>
	NoImpactDamage = 0x0400,

	///<summary> Don't do impact damage to NPC's. This is temporary for NPC's shooting combine balls (sjb) </summary>
	NoNPCImpactDamage = 0x0800,

	///<summary> don't collide with other objects that are part of the same entity </summary>
	NoSelfCollisions = 0x8000, 

}
