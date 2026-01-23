#if CLIENT_DLL || GAME_DLL

using Source;
using Source.Common;
using Source.Common.Formats.BSP;
using Source.Common.Mathematics;
using Source.Engine;

using System.Diagnostics;
using System.Numerics;

namespace Game.Shared;


public enum SpeedCropped
{
	Reset,
	Duck,
	Weapon
}
public class GameMovement : IGameMovement
{
	public void FinishTrackPredictionErrors(BasePlayer player) {

	}

	public Vector3 GetPlayerMaxs(bool ducked) {
		throw new NotImplementedException();
	}

	public Vector3 GetPlayerMins(bool ducked) {
		throw new NotImplementedException();
	}

	public Vector3 GetPlayerViewOffset(bool ducked) {
		throw new NotImplementedException();
	}

	SpeedCropped SpeedCropped;

	public void ProcessMovement(BasePlayer player, MoveData pMove) {
		TimeUnit_t storeFrametime = gpGlobals.FrameTime;

		//!!HACK HACK: Adrian - slow down all player movement by this factor.
		//!!Blame Yahn for this one.
		gpGlobals.FrameTime *= player.GetLaggedMovementValue();

		ResetGetPointContentsCache();

		// Cropping movement speed scales mv->m_fForwardSpeed etc. globally
		// Once we crop, we don't want to recursively crop again, so we set the crop
		//  flag globally here once per usercmd cycle.
		SpeedCropped = SpeedCropped.Reset;

		// StartTrackPredictionErrors should have set this
		Assert(Player == player);
		Player = player;
		mv = pMove;

		mv.MaxSpeed = player.GetPlayerMaxSpeed();

		// Run the command.
		PlayerMove();

		FinishMove();

		// CheckV( player->CurrentCommandNumber(), "EndPos", mv->GetAbsOrigin() );

		//This is probably not needed, but just in case.
		gpGlobals.FrameTime = storeFrametime;
	}

	public void StartTrackPredictionErrors(BasePlayer player) {
		Player = player;
	}

	protected MoveData? mv;

	protected int OldWaterLevel;
	protected float WaterEntryTime;
	protected int iOnLadder;

	protected Vector3 Forward;
	protected Vector3 Right;
	protected Vector3 Up;

	protected virtual void PlayerMove() { throw new NotImplementedException(); }
	protected void FinishMove() { throw new NotImplementedException(); }
	protected virtual float CalcRoll(in QAngle angles, in Vector3 velocity, float rollangle, float rollspeed) { throw new NotImplementedException(); }

	protected virtual void DecayPunchAngle() { throw new NotImplementedException(); }

	protected virtual void CheckWaterJump() { throw new NotImplementedException(); }

	protected virtual void WaterMove() { throw new NotImplementedException(); }

	protected void WaterJump() { throw new NotImplementedException(); }

	// Handles both ground friction and water friction
	protected void Friction() { throw new NotImplementedException(); }

	protected virtual void AirAccelerate(ref Vector3 wishdir, float wishspeed, float accel) { throw new NotImplementedException(); }

	protected virtual void AirMove() { throw new NotImplementedException(); }
	protected virtual float GetAirSpeedCap() { return 30f; }

	protected virtual bool CanAccelerate() { throw new NotImplementedException(); }
	protected virtual void Accelerate(ref Vector3 wishdir, float wishspeed, float accel) { throw new NotImplementedException(); }

	// Only used by players.  Moves along the ground when player is a MOVETYPE_WALK.
	protected virtual void WalkMove() { throw new NotImplementedException(); }

	// Try to keep a walking player on the ground when running down slopes etc
	protected void StayOnGround() { throw new NotImplementedException(); }

	// Handle MOVETYPE_WALK.
	protected virtual void FullWalkMove() { throw new NotImplementedException(); }

	// allow overridden versions to respond to jumping
	protected virtual void OnJump(float fImpulse) { }
	protected virtual void OnLand(float fVelocity) { }

	// Implement this if you want to know when the player collides during OnPlayerMove
	protected virtual void OnTryPlayerMoveCollision(ref Trace tr) { }

	protected virtual Vector3 GetPlayerMins() { throw new NotImplementedException(); } // uses local player
	protected virtual Vector3 GetPlayerMaxs() { throw new NotImplementedException(); } // uses local player

	protected enum IntervalType
	{
		GROUND = 0,
		STUCK,
		LADDER
	}

	protected virtual int GetCheckInterval(IntervalType type) { throw new NotImplementedException(); }

	// Useful for things that happen periodically. This lets things happen on the specified interval, but
	// spaces the events onto different frames for different players so they don't all hit their spikes
	// simultaneously.
	protected bool CheckInterval(IntervalType type) { throw new NotImplementedException(); }


	// Decompoosed gravity
	protected void StartGravity() { throw new NotImplementedException(); }
	protected void FinishGravity() { throw new NotImplementedException(); }

	// Apply normal ( undecomposed ) gravity
	protected void AddGravity() { throw new NotImplementedException(); }

	// Handle movement in noclip mode.
	protected void FullNoClipMove(float factor, float maxacceleration) { throw new NotImplementedException(); }

	// Returns true if he started a jump (ie: should he play the jump animation)?
	protected virtual bool CheckJumpButton() { throw new NotImplementedException(); }    // Overridden by each game.

	// Dead player flying through air., e.g.
	protected virtual void FullTossMove() { throw new NotImplementedException(); }

	// Player is a Observer chasing another player
	protected void FullObserverMove() { throw new NotImplementedException(); }

	// Handle movement when in MOVETYPE_LADDER mode.
	protected virtual void FullLadderMove() { throw new NotImplementedException(); }

	// The basic solid body movement clip that slides along multiple planes
	protected virtual int TryPlayerMove(ref Vector3 firstDest, ref Trace firstTrace) { throw new NotImplementedException(); }

	protected virtual bool LadderMove() { throw new NotImplementedException(); }
	protected virtual bool OnLadder(ref Trace trace) { throw new NotImplementedException(); }
	protected virtual float LadderDistance() { return 2.0f; }   ///< Returns the distance a player can be from a ladder and still attach to it
	protected virtual Mask LadderMask() { return Mask.PlayerSolid; }
	protected virtual float ClimbSpeed() { return MAX_CLIMB_SPEED; }
	protected virtual float LadderLateralMultiplier() { return 1.0f; }

	// See if the player has a bogus velocity value.
	protected void CheckVelocity() { throw new NotImplementedException(); }

	// Does not change the entities velocity at all
	protected void PushEntity(ref Vector3 push, ref Trace trace) { throw new NotImplementedException(); }

	// Slide off of the impacting object
	// returns the blocked flags:
	// 0x01 == floor
	// 0x02 == step / wall
	protected int ClipVelocity(in Vector3 inVec, in Vector3 normal, out Vector3 outVec, float overbounce) { throw new NotImplementedException(); }

	// If pmove.origin is in a solid position,
	// try nudging slightly on all axis to
	// allow for the cut precision of the net coordinates
	protected virtual int CheckStuck() { throw new NotImplementedException(); }

	// Check if the point is in water.
	// Sets refWaterLevel and refWaterType appropriately.
	// If in water, applies current to baseVelocity, and returns true.
	protected virtual bool CheckWater() { throw new NotImplementedException(); }

	// Determine if player is in water, on ground, etc.
	protected virtual void CategorizePosition() { throw new NotImplementedException(); }

	protected virtual void CheckParameters() { throw new NotImplementedException(); }

	protected virtual void ReduceTimers() { throw new NotImplementedException(); }

	protected virtual void CheckFalling() { throw new NotImplementedException(); }

	protected virtual void PlayerRoughLandingEffects(float fvol) { throw new NotImplementedException(); }

	protected void PlayerWaterSounds() { throw new NotImplementedException(); }

	protected void ResetGetPointContentsCache() { throw new NotImplementedException(); }
	protected int GetPointContentsCached(in Vector3 point, int slot) { throw new NotImplementedException(); }

	// Ducking
	protected virtual void Duck() { throw new NotImplementedException(); }
	protected virtual void HandleDuckingSpeedCrop() { throw new NotImplementedException(); }
	protected virtual void FinishUnDuck() { throw new NotImplementedException(); }
	protected virtual void FinishDuck() { throw new NotImplementedException(); }
	protected virtual bool CanUnduck() { throw new NotImplementedException(); }
	protected void UpdateDuckJumpEyeOffset() { throw new NotImplementedException(); }
	protected bool CanUnDuckJump(ref Trace trace) { throw new NotImplementedException(); }
	protected void StartUnDuckJump() { throw new NotImplementedException(); }
	protected void FinishUnDuckJump(ref Trace trace) { throw new NotImplementedException(); }
	protected void SetDuckedEyeOffset(float duckFraction) { throw new NotImplementedException(); }
	protected void FixPlayerCrouchStuck(bool moveup) { throw new NotImplementedException(); }

	protected float SplineFraction(float value, float scale) { throw new NotImplementedException(); }

	protected void CategorizeGroundSurface(ref Trace pm) { throw new NotImplementedException(); }

	protected bool InWater() { throw new NotImplementedException(); }

	// Commander view movement
	protected void IsometricMove() { throw new NotImplementedException(); }

	// Traces the player bbox as it is swept from start to end
	protected virtual BaseHandle TestPlayerPosition(in Vector3 pos, int collisionGroup, ref Trace pm) { throw new NotImplementedException(); }

	// Checks to see if we should actually jump 
	protected void PlaySwimSound() { throw new NotImplementedException(); }
	protected bool IsDead() => Player.Health <= 0 && !Player.IsAlive();
	// Figures out how the constraint should slow us down
	protected float ComputeConstraintSpeedFactor() {
		if (mv == null || mv.ConstraintRadius == 0.0f)
			return 1.0f;

		float flDistSq = mv.GetAbsOrigin().DistToSqr(mv.ConstraintCenter);

		float flOuterRadiusSq = mv.ConstraintRadius * mv.ConstraintRadius;
		float flInnerRadiusSq = mv.ConstraintRadius - mv.ConstraintWidth;
		flInnerRadiusSq *= flInnerRadiusSq;

		// Only slow us down if we're inside the constraint ring
		if ((flDistSq <= flInnerRadiusSq) || (flDistSq >= flOuterRadiusSq))
			return 1.0f;

		// Only slow us down if we're running away from the center
		Vector3 vecDesired;
		MathLib.VectorMultiply(Forward, mv.ForwardMove, out vecDesired);
		MathLib.VectorMA(vecDesired, mv.SideMove, Right, out vecDesired);
		MathLib.VectorMA(vecDesired, mv.UpMove, Up, out vecDesired);

		Vector3 vecDelta;
		MathLib.VectorSubtract(mv.GetAbsOrigin(), mv.ConstraintCenter, out vecDelta);
		MathLib.VectorNormalize(ref vecDelta);
		MathLib.VectorNormalize(ref vecDesired);
		if (MathLib.DotProduct(vecDelta, vecDesired) < 0.0f)
			return 1.0f;

		float flFrac = (MathF.Sqrt(flDistSq) - (mv.ConstraintRadius - mv.ConstraintWidth)) / mv.ConstraintWidth;

		float flSpeedFactor = float.Lerp(flFrac, 1.0f, mv.ConstraintSpeedFactor);
		return flSpeedFactor;
	}
	protected virtual void SetGroundEntity(ref Trace pm) { }
	protected virtual void StepMove(ref Vector3 Destination, ref Trace trace) { }
	// when we step on ground that's too steep, search to see if there's any ground nearby that isn't too steep
	protected void TryTouchGroundInQuadrants(in Vector3 start, in Vector3 end, Mask mask, CollisionGroup collisionGroup, ref Trace pm) { }
	protected void PerformFlyCollisionResolution(ref Trace pm, ref Vector3 move) { }
	protected virtual bool GameHasLadders() => true;


	protected const int MAX_PC_CACHE_SLOTS = 3;

	// Cache used to remove redundant calls to GetPointContents().
	protected readonly int[,] CachedGetPointContents = new int[Constants.MAX_PLAYERS, MAX_PC_CACHE_SLOTS];
	protected readonly Vector3[,] CachedGetPointContentsPoint = new Vector3[Constants.MAX_PLAYERS, MAX_PC_CACHE_SLOTS];

	protected Vector3 ProximityMins;      // Used to be globals in sv_user.cpp.
	protected Vector3 ProximityMaxs;

	protected float FrameTime;

	public BasePlayer Player = null!;
	public MoveData? GetMoveData() => mv;
}
#endif
