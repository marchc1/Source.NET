global using static Game.Client.PhysicsSystemHook;

using Game.Shared;

using Source;
using Source.Common.Engine;

using System;
using System.Collections.Generic;
using System.Text;

namespace Game.Client;

[EngineComponent]
public static class PhysicsSystemHook
{
	public static readonly PhysicsSystem g_PhysicsSystem = new();
	static PhysicsSystemHook() {
		SetPhysicsGameSystem(g_PhysicsSystem);
	}
}

public class PhysicsSystem : AutoGameSystemPerFrame
{
	public override bool Init() => true;

	public override void LevelInitPostEntity() {
		PhysicsLevelInit();
	}

	private void PhysicsLevelInit() {
		physenv = physics.CreateEnvironment();
		Assert(physenv != null);

		g_EntityCollisionHash = physics.CreateObjectPairHash();

		// TODO: need to get the right factory function here
		//physenv->SetDebugOverlay( appSystemFactory );
		physenv.SetGravity(new(0, 0, -GetCurrentGravity()));
		// 15 ms per tick
		// NOTE: Always run client physics at this rate - helps keep ragdolls stable
		physenv.SetSimulationTimestep(gpGlobals.IntervalPerTick);
		// TODO: physenv.SetCollisionEventHandler(g_Collisions);
		// TODO: physenv.SetCollisionSolver(g_Collisions);

		// TODO: g_PhysWorldObject = PhysCreateWorld_Shared(C_World.GetClientWorldEntity(), modelinfo.GetVCollide(1), g_PhysDefaultObjectParams);

		// TODO: staticpropmgr.CreateVPhysicsRepresentations(physenv, g_SolidSetup, NULL);
	}
}
