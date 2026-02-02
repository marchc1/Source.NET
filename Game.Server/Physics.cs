using Game.Shared;

using Source.Common.Engine;
using Source.Common.Physics;

using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Game.Server;

public class PhysicsHook : BaseGameSystemPerFrame
{
	public readonly CollisionEvent g_Collisions = new();
	public static TimeUnit_t g_PhysAverageSimTime;

	public override ReadOnlySpan<char> Name() => "PhysicsHook";

	public bool Paused;

	public override bool Init() {
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
		// g_PhysWorldObject = PhysCreateWorld(GetWorldEntity());

		// g_pShadowEntities = new CEntityList;
		// PrecachePhysicsSounds();

		Paused = true;
	}
	public override void LevelInitPostEntity() {
		base.LevelInitPostEntity();
	}
	public override void LevelShutdownPreEntity() {
		base.LevelShutdownPreEntity();
	}
	public override void LevelShutdownPostEntity() {
		base.LevelShutdownPostEntity();
	}
	public override void FrameUpdatePostEntityThink() {
		base.FrameUpdatePostEntityThink();
	}
	public override void PreClientUpdate() {
		base.PreClientUpdate();
	}
}
