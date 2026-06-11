namespace Game.Server;

using Game.Shared;

using Source;
using Source.Common.Commands;
using Source.Common.Formats.BSP;
using Source.Common.Mathematics;
using Source.Engine;

using Steamworks;

using System.Numerics;

public partial class BaseEntity
{
	public void CheckStepSimulationChanged() {
		if (Physics.g_bTestMoveTypeStepSimulation != IsSimulatedEveryTick())
			SetSimulatedEveryTick(Physics.g_bTestMoveTypeStepSimulation);

		bool hadObject = HasDataObjectType(DataObjectType.StepSimulation);

		if (Physics.g_bTestMoveTypeStepSimulation) {
			if (!hadObject)
				CreateDataObject(DataObjectType.StepSimulation);
		}
		else {
			if (hadObject)
				DestroyDataObject(DataObjectType.StepSimulation);
		}
	}

	public void PhysicsStepRunTimestep(TimeUnit_t timestep) {
		bool wasonground;
		bool inwater;
		float speed, newspeed, control;
		float friction;

		PhysicsCheckVelocity();

		wasonground = (GetFlags() & EntityFlags.OnGround) != 0;

		inwater = PhysicsCheckWater();

		bool isfalling = false;

		if (!wasonground) {
			if ((GetFlags() & EntityFlags.Fly) == 0) {
				if (!((GetFlags() & EntityFlags.Swim) != 0 && (GetWaterLevel() > 0))) {
					if (!inwater) {
						PhysicsAddHalfGravity(timestep);
						isfalling = true;
					}
				}
			}
		}

		if ((GetFlags() & EntityFlags.StepMovement) == 0 && (!MathLib.VectorCompare(GetAbsVelocity(), vec3_origin) || !MathLib.VectorCompare(GetBaseVelocity(), vec3_origin))) {
			Vector3 vecAbsVelocity = GetAbsVelocity();

			SetGroundEntity(null);

			if (wasonground) {
				speed = MathLib.VectorLength(vecAbsVelocity);
				if (speed != 0) {
					friction = sv_friction.GetFloat() * GetFriction();

					control = speed < sv_stopspeed.GetFloat() ? sv_stopspeed.GetFloat() : speed;
					newspeed = (float)(speed - timestep * control * friction);

					if (newspeed < 0)
						newspeed = 0;
					newspeed /= speed;

					vecAbsVelocity[0] *= newspeed;
					vecAbsVelocity[1] *= newspeed;
				}
			}

			vecAbsVelocity += GetBaseVelocity();
			SetAbsVelocity(vecAbsVelocity);

			SimulateAngles(timestep);

			PhysicsCheckVelocity();

			PhysicsTryMove(timestep, null);

			PhysicsCheckVelocity();

			vecAbsVelocity = GetAbsVelocity();
			vecAbsVelocity -= GetBaseVelocity();
			SetAbsVelocity(vecAbsVelocity);

			PhysicsCheckVelocity();

			if ((GetFlags() & EntityFlags.OnGround) == 0)
				PhysicsStepRecheckGround();

			// PhysicsTouchTriggers();
		}

		if ((GetFlags() & EntityFlags.OnGround) == 0 && !isfalling)
			PhysicsAddHalfGravity(timestep);
	}

	private void PhysicsAddHalfGravity(double timestep) {
		throw new NotImplementedException();
	}

	private void PhysicsStepRecheckGround() {
		throw new NotImplementedException();
	}

	private void PhysicsTryMove(double timestep, object? value) {
		throw new NotImplementedException();
	}
}

public static class Physics
{
	public static bool g_bTestMoveTypeStepSimulation = true;
	static readonly ConVar sv_teststepsimulation = new("1", 0);
	public readonly static ConVar npc_vphysics = new("0", 0);

	const float PLAYER_PACKETS_STOPPED_SO_RETURN_TO_PHYSICS_TIME = 1.0f;

	public static void TraceEntity(BaseEntity entity, in Vector3 start, in Vector3 end, uint mask, out Trace tr) {
		throw new NotImplementedException();
	}

	static void SimulateEntity(BaseEntity entity) {
		if (entity.Edict() != null) {
			if (entity.IsPlayerSimulated()) {
				BasePlayer? simulatingPlayer = entity.GetSimulatingPlayer();
				if (simulatingPlayer != null && (simulatingPlayer.GetTimeBase() > gpGlobals.CurTime - PLAYER_PACKETS_STOPPED_SO_RETURN_TO_PHYSICS_TIME))
					return;

				entity.UnsetPlayerSimulated();
			}

			if (entity.PredictableId.IsActive()) {
				if (entity.GetOwnerEntity() is BasePlayer playerowner) {
					BasePlayer? pl = Util.PlayerByIndex(entity.PredictableId.GetPlayer() + 1);
					if (pl == playerowner)
						if (pl.IsPredictingWeapons())
							IPredictionSystem.SuppressHostEvents(playerowner);
				}

				entity.PhysicsSimulate();

				IPredictionSystem.SuppressHostEvents(null);
			}
			else
				entity.PhysicsSimulate();
		}
		else
			entity.PhysicsRunThink();
	}

	public static void RunThinkFunctions(bool simulating) {
		g_bTestMoveTypeStepSimulation = sv_teststepsimulation.GetBool();

		TimeUnit_t startTime = gpGlobals.CurTime;

		gEntList.CleanupDeleteList();

		if (!simulating) {
			for (int i = 1; i <= gpGlobals.MaxClients; i++) {
				BasePlayer? player = Util.PlayerByIndex(i);
				if (player != null) {
					gpGlobals.CurTime = startTime;
					player.ForceSimulation();
					SimulateEntity(player);
				}
			}
		}
		else {
			int listMax = SimThinkManager.g_SimThinkManager.ListCount();
			listMax = Math.Max(listMax, 1);
			BaseEntity[] list = new BaseEntity[listMax];

			int count = SimThinkManager.g_SimThinkManager.ListCopy(list, listMax);

			for (int i = 0; i < count; i++) {
				if (list[i] == null)
					continue;

				gpGlobals.CurTime = startTime;
				SimulateEntity(list[i]);
				list[i].NetworkStateChanged();
			}

			// Util.EnableRemoveImmediate();
		}

		gpGlobals.CurTime = startTime;
	}
}

public partial class BaseEntity
{
	void PhysicsStep() {
		// EVIL HACK: Force these to appear as if they've changed!!!
		// The underlying values don't actually change, but we need the network sendproxy on origin/angles
		//  to get triggered, and that only happens if NetworkStateChanged() appears to have occured.
		// Getting them for modify marks them as changed automagically.
		// Origin.GetForModify(); // TODO!
		// Rotation.GetForModify(); // TODO!

		SetSimulationTime(gpGlobals.CurTime);

		PhysicsRunThink(ThinkMethods.FireAllButBase);

		long thinkTick = GetNextThinkTick();

		TimeUnit_t thinkTime = thinkTick * TICK_INTERVAL;
		TimeUnit_t deltaThink = thinkTime - gpGlobals.CurTime;

		if (thinkTime <= 0 || deltaThink > 0.5) {
			PhysicsStepRunTimestep(gpGlobals.FrameTime);
			// PhysicsCheckWaterTransition();
			// SetLastThink(-1, gpGlobals.CurTime);
			// UpdatePhysicsShadowToCurrentPosition(gpGlobals.FrameTime);
			// PhysicsRelinkChildren(gpGlobals.FrameTime);
			return;
		}

		Vector3 oldOrigin = GetAbsOrigin();

		bool updateFromVPhysics = Physics.npc_vphysics.GetBool();
		if (HasDataObjectType(DataObjectType.VPhysicsUpdateAI)) {
			// todo
		}

		if (updateFromVPhysics && VPhysicsGetObject() != null && GetParent() == null) {
			VPhysicsGetObject()!.GetShadowPosition(out Vector3 position, out _);
			float delta = (GetAbsOrigin() - position).LengthSqr();
			if (delta < 1) {
				Physics.TraceEntity(this, GetAbsOrigin(), GetAbsOrigin(), (uint)Mask.Solid, out Trace tr); // PhysicsSolidMaskForEntity
				updateFromVPhysics = tr.StartSolid;
			}

			if (updateFromVPhysics) {
				SetAbsOrigin(position);
				// PhysicsTouchTriggers();
			}
		}

		if (thinkTick > gpGlobals.TickCount)
			return;

		if (thinkTime < gpGlobals.CurTime)
			thinkTime = gpGlobals.CurTime;

		float dt = (float)(thinkTime - 0);//GetLastThink(); todo

		StepSimulationThink(dt);

		// PhysicsCheckWaterTransition();

		if (VPhysicsGetObject() != null) {
			if (!MathLib.VectorCompare(oldOrigin, GetAbsOrigin()))
				VPhysicsGetObject()!.UpdateShadow(GetAbsOrigin(), vec3_angle, (GetFlags() & EntityFlags.Fly) != 0, dt);
		}

		// PhysicsRelinkChildren(dt);
	}

	void PhysicsPusher() { }

	void PhysicsNone() {
		PhysicsRunThink();
	}

	void PhysicsRigidChild() { }

	void PhysicsNoclip() {
		if (!PhysicsRunThink()) {
			return;
		}

		SimulateAngles(gpGlobals.FrameTime);

		MathLib.VectorMA(GetLocalOrigin(), gpGlobals.FrameCount, Velocity, out Vector3 origin);
		SetLocalOrigin(origin);
	}

	void PhysicsToss() { }

	void PhysicsCustom() { }

	void PerformPush(TimeUnit_t movetime) { }

	void StepSimulationThink(TimeUnit_t dt) {
		CheckStepSimulationChanged();

		StepSimulationData? stepObject = (StepSimulationData?)GetDataObject(DataObjectType.StepSimulation);
		if (stepObject == null) {
			PhysicsStepRunTimestep(dt);
			PhysicsRunThink(ThinkMethods.FireBaseOnly);
		}
		else {
			// StepSimulationData step = stepObject.Value; // fixme (?)
			// step.OriginActive = true;
			// step.AnglesActive = true;

			// step.LastProcessTickCount = -1;

			// step.NetworkOrigin.Init();
			// step.NetworkAngles.Init();

			// step.Previous2 = step.Previous;

			// step.Previous.TickCount = gpGlobals.TickCount;
			// step.Previous.Origin = GetStepOrigin();
			// QAngle stepAngles = GetStepAngles();
			// MathLib.AngleQuaternion(stepAngles, out step.Previous.Rotation);

			// PhysicsStepRunTimestep(dt);

			// PhysicsRunThink(ThinkMethods.FireBaseOnly);

			// if (GetBaseAnimating() != null)
			// 	GetBaseAnimating()!.UpdateStepOrigin();

			// step.Next.Origin = GetStepOrigin();
			// stepAngles = GetStepAngles();
			// MathLib.AngleQuaternion(stepAngles, out step.Next.Rotation);

			// step.AngNextRotation = GetStepAngles();
			// step.Next.TickCount = GetNextThinkTick();

			// if (IsSimulatingOnAlternateTicks())
			// 	++step.Next.TickCount;

			// if (dt > 0) {
			// 	Vector3 deltaOrigin = step.Next.Origin - step.Previous.Origin;
			// 	float velSq = (float)(deltaOrigin.LengthSquared() / (dt * dt));
			// 	if (velSq >= (4096.0f * 4096.0f) /*STEP_TELPORTATION_VEL_SQ*/)
			// 		step.OriginActive = step.AnglesActive = false;
			// }
		}
	}
}