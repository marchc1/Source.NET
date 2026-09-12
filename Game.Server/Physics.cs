global using static Game.Server.PhysicsHookGlobals;

using Game.Shared;

using Source;
using Source.Common;
using Source.Common.Engine;
using Source.Common.Physics;
using Source.Engine;

using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Game.Server;

[EngineComponent]
public static class PhysicsHookGlobals {
	public static readonly PhysicsHook g_PhysicsHook = new();
	public static readonly CollisionEvent g_Collisions = new();
	public static EntityList? g_ShadowEntities = null;
	public static TimeUnit_t g_PhysAverageSimTime;
	public static readonly ConcurrentQueue<Action> g_PostSimulationQueue = new();

	public const float VPHYSICS_LARGE_OBJECT_MASS = 500.0f;

	static PhysicsHookGlobals(){
		SetPhysicsGameSystem(g_PhysicsHook);
	}

	public static bool PhysIsInCallback(){
		return (physenv != null && physenv.IsInSimulation()) || g_Collisions.IsInCallback();
	}

	public static void PhysAddShadow(BaseEntity entity) => g_ShadowEntities!.AddEntity(entity);
	public static void PhysRemoveShadow(BaseEntity entity) => g_ShadowEntities!.DeleteEntity(entity);

	public static void PhysCallbackRemove(IServerNetworkable remove){
		if (PhysIsInCallback()) 
			g_Collisions.AddRemoveObject(remove);
		else 
			Util.Remove(remove);
	}

	public static void PhysCallbackDamage(BaseEntity entity, in TakeDamageInfo info){
		if (PhysIsInCallback()) {
			BaseEntity inflictor = info.GetInflictor();
			IPhysicsObject? inflictorPhysics = inflictor?.VPhysicsGetObject();
			g_Collisions.AddDamageEvent(entity, info, inflictorPhysics, false, vec3_origin, vec3_origin);
			if (entity != null && info.GetInflictor() != null) 
				DevMsg(2, $"Warning: Physics damage event with no recovery info!\nObjects: {entity.GetClassname()}, {info.GetInflictor()!.GetClassname()}\n");
		}
		else 
			entity.TakeDamage(info);
	}
}

public class PhysicsHook : BaseGameSystemPerFrame
{
	public static TimeUnit_t g_PhysAverageSimTime;

	public override ReadOnlySpan<char> Name() => "PhysicsHook";

	public bool Paused;

	public override bool Init() {
		PhysParseSurfaceData(physprops, filesystem);

		return base.Init();
	}
	public override void LevelInitPreEntity() {
		physenv = physics.CreateEnvironment();
		PhysicsPerformanceParams parms = default;
		parms.Defaults();
		parms.MaxCollisionsPerObjectPerTimestep = 10;
		physenv.SetPerformanceSettings(in parms);

#if PORTAL
		physenv_main = physenv;
#endif
		g_EntityCollisionHash = physics.CreateObjectPairHash();
		physenv.SetDebugOverlay(Singleton<IEngineAPI>());
		physenv.EnableDeleteQueue(true);

		physenv.SetCollisionSolver(g_Collisions);
		physenv.SetCollisionEventHandler(g_Collisions);
		physenv.SetConstraintEventHandler(g_pConstraintEvents);
		physenv.EnableConstraintNotify(true); // callback when an object gets deleted that is attached to a constraint

		physenv.SetObjectEventHandler(g_Collisions);

		physenv.SetSimulationTimestep(gpGlobals.IntervalPerTick); 
																	
		physenv.SetGravity(new Vector3(0, 0, -GetCurrentGravity()));
		g_PhysAverageSimTime = 0;

		// todo
		g_PhysWorldObject = PhysCreateWorld(GetWorldEntity());

		g_ShadowEntities = new EntityList();
		PrecachePhysicsSounds();

		Paused = true;
	}
	public static IPhysicsObject? PhysCreateWorld(BaseEntity world){
		// todo staticpropmgr
		VCollide? worldCollide = modelinfo.GetVCollide(1);
		return PhysCreateWorld_Shared(world, worldCollide, g_PhysDefaultObjectParams);
	}
	public static HSOUNDSCRIPTHANDLE PrecachePhysicsSoundByStringIndex(UtlSymId_t idx) => idx != 0 ? BaseEntity.PrecacheScriptSound(physprops.GetString(idx)) : SOUNDEMITTER_INVALID_HANDLE;
	public void PrecachePhysicsSounds(){
		// precache the surface prop sounds
		for (nint i = 0; i < physprops.SurfacePropCount(); i++) {
			var prop = physprops.GetSurfaceData(i);
			Assert(prop != null);

			prop.SoundHandles.StepLeft = PrecachePhysicsSoundByStringIndex(prop.Sounds.StepLeft);
			prop.SoundHandles.StepRight = PrecachePhysicsSoundByStringIndex(prop.Sounds.StepRight);
			prop.SoundHandles.ImpactSoft = PrecachePhysicsSoundByStringIndex(prop.Sounds.ImpactSoft);
			prop.SoundHandles.ImpactHard = PrecachePhysicsSoundByStringIndex(prop.Sounds.ImpactHard);
			prop.SoundHandles.ScrapeSmooth = PrecachePhysicsSoundByStringIndex(prop.Sounds.ScrapeSmooth);
			prop.SoundHandles.ScrapeRough = PrecachePhysicsSoundByStringIndex(prop.Sounds.ScrapeRough);
			prop.SoundHandles.BulletImpact = PrecachePhysicsSoundByStringIndex(prop.Sounds.BulletImpact);
			prop.SoundHandles.Rolling = PrecachePhysicsSoundByStringIndex(prop.Sounds.Rolling);
			prop.SoundHandles.BreakSound = PrecachePhysicsSoundByStringIndex(prop.Sounds.BreakSound);
			prop.SoundHandles.StrainSound = PrecachePhysicsSoundByStringIndex(prop.Sounds.StrainSound);
		}
	}
	public override void LevelInitPostEntity() {
		base.LevelInitPostEntity();
		Paused = false;
	}

	public bool ShouldSimulate() => physenv != null && !Paused;

	void PhysFrame(TimeUnit_t deltaTime) {
		if (!ShouldSimulate())
			return;

		// Trap interrupts and clock changes
		if (deltaTime > 1.0 || deltaTime < 0.0)
			deltaTime = 0;
		else if (deltaTime > 0.1)
			deltaTime = 0.1; // limit incoming time to 100ms

		physenv!.DebugCheckContacts();
		physenv.Simulate(deltaTime);

		int activeCount = physenv.GetActiveObjectCount();
		if (activeCount != 0) {
			IPhysicsObject?[] activeList = ArrayPool<IPhysicsObject>.Shared.Rent(activeCount);
			physenv.GetActiveObjects(activeList);

			for (int i = 0; i < activeCount; i++) {
				BaseEntity? entity = (BaseEntity?)activeList[i]?.GetGameData();
				if (entity != null)
					entity.VPhysicsUpdate(activeList[i]!);
			}

			ArrayPool<IPhysicsObject>.Shared.Return(activeList, true);
		}
	}
	public override void LevelShutdownPreEntity() {
		base.LevelShutdownPreEntity();
	}
	public override void LevelShutdownPostEntity() {
		base.LevelShutdownPostEntity();
	}
	public override void FrameUpdatePostEntityThink() {
		base.FrameUpdatePostEntityThink();
		PhysFrame(gpGlobals.FrameTime);
	}
	public override void PreClientUpdate() {
		base.PreClientUpdate();
	}
}
