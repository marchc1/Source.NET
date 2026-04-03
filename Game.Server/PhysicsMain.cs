namespace Game.Server;

using Game.Shared;

using Source;
using Source.Common.Commands;
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
	static ConVar sv_teststepsimulation = new("1", 0);

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
			}

			// Util.EnableRemoveImmediate();
		}

		gpGlobals.CurTime = startTime;
	}
}