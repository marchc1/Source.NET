#if CLIENT_DLL || GAME_DLL

using Source;
using Source.Common;
using Source.Common.Commands;
using Source.Common.Engine;
using Source.Common.Formats.BSP;
using Source.Common.Mathematics;
using Source.Common.Physics;
using Source.Engine;

using System.Diagnostics;
using System.Numerics;
using System.Text.RegularExpressions;

namespace Game.Shared;


public enum SpeedCropped
{
	Reset,
	Duck,
	Weapon
}
public class GameMovement : IGameMovement
{
	static bool g_bMovementOptimizations = true;
	public void FinishTrackPredictionErrors(BasePlayer player) {

	}

	public Vector3 GetPlayerMaxs(bool ducked) {
		return ducked ? VEC_DUCK_HULL_MAX_SCALED(Player) : VEC_HULL_MAX_SCALED(Player);
	}

	public Vector3 GetPlayerMins(bool ducked) {
		return ducked ? VEC_DUCK_HULL_MIN_SCALED(Player) : VEC_HULL_MIN_SCALED(Player);
	}

	public Vector3 GetPlayerViewOffset(bool ducked) {
		return ducked ? VEC_DUCK_VIEW_SCALED(Player) : VEC_VIEW_SCALED(Player);
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

		// CheckV( Player.CurrentCommandNumber(), "EndPos", mv->GetAbsOrigin() );

		//This is probably not needed, but just in case.
		gpGlobals.FrameTime = storeFrametime;
	}

	public void StartTrackPredictionErrors(BasePlayer player) {
		Player = player;
	}

	protected MoveData? mv;

	protected WaterLevel OldWaterLevel;
	protected TimeUnit_t WaterEntryTime;
	protected int iOnLadder;

	protected Vector3 Forward;
	protected Vector3 Right;
	protected Vector3 Up;
	public static readonly ConVar sv_optimizedmovement = new("sv_optimizedmovement", "1", FCvar.Replicated | FCvar.DevelopmentOnly);



	protected virtual void PlayerMove() {
		CheckParameters();

		// clear output applied velocity
		mv!.WishVel.Init();
		mv!.JumpVel.Init();

		MoveHelper().ResetTouchList();                    // Assume we don't touch anything

		ReduceTimers();

		MathLib.AngleVectors(mv.ViewAngles, out Forward, out Right, out Up);  // Determine movement angles

		// Always try and unstick us unless we are using a couple of the movement modes
		if (Player.GetMoveType() != MoveType.Noclip &&
			 Player.GetMoveType() != MoveType.None &&
			 Player.GetMoveType() != MoveType.Isometric &&
			 Player.GetMoveType() != MoveType.Observer &&
			 !Player.pl.DeadFlag) {
			if (CheckInterval(IntervalType.Stuck)) {
				if (CheckStuck()) {
					// Can't move, we're stuck
					return;
				}
			}
		}

		// Now that we are "unstuck", see where we are (Player.GetWaterLevel() and type, Player.GetGroundEntity()).
		if (Player.GetMoveType() != MoveType.Walk ||
			mv.GameCodeMovedPlayer ||
			!sv_optimizedmovement.GetBool()) {
			CategorizePosition();
		}
		else {
			if (mv.Velocity.Z > 250.0f)
				SetGroundEntity(ref Trace.NULL);
		}

		// Store off the starting water level
		OldWaterLevel = Player.GetWaterLevel();

		// If we are not on ground, store off how fast we are moving down
		if (Player.GetGroundEntity() == null)
			Player.Local.FallVelocity = -mv.Velocity[2];

		iOnLadder = 0;

		Player.UpdateStepSound(Player.SurfaceData, mv.GetAbsOrigin(), mv.Velocity);

		UpdateDuckJumpEyeOffset();
		Duck();

		// Don't run ladder code if dead on on a train
		if (!Player.pl.DeadFlag && 0 == (Player.GetFlags() & EntityFlags.OnTrain)) {
			// If was not on a ladder now, but was on one before, 
			//  get off of the ladder
			if (!LadderMove() && (Player.GetMoveType() == MoveType.Ladder)) {
				Player.SetMoveType(MoveType.Walk);
				Player.SetMoveCollide(MoveCollide.Default);
			}
		}

		// Handle movement modes.
		switch (Player.GetMoveType()) {
			case MoveType.None:
				break;

			case MoveType.Noclip:
				FullNoClipMove(sv_noclipspeed.GetFloat(), sv_noclipaccelerate.GetFloat());
				break;

			case MoveType.Fly:
			case MoveType.FlyGravity:
				FullTossMove();
				break;

			case MoveType.Ladder:
				FullLadderMove();
				break;

			case MoveType.Walk:
				FullWalkMove();
				break;

			case MoveType.Isometric:
				//IsometricMove();
				// Could also try:  FullTossMove();
				FullWalkMove();
				break;

			case MoveType.Observer:
				FullObserverMove(); // clips against world&players
				break;

			default:
				DevMsg(1, $"Bogus pmove player movetype {Player.GetMoveType()} on ({(Player.IsServer() ? '1' : '0')}) 0=cl 1=sv\n");
				break;
		}
	}
	protected void FinishMove() {
		mv!.OldButtons = mv.Buttons;
		mv!.OldForwardMove = mv.ForwardMove;
	}
	protected virtual float CalcRoll(in QAngle angles, in Vector3 velocity, float rollangle, float rollspeed) {
		float sign;
		float side;
		float value;
		Vector3 forward, right, up;

		MathLib.AngleVectors(angles, out forward, out right, out up);

		side = MathLib.DotProduct(velocity, right);
		sign = side < 0 ? -1 : 1;
		side = MathF.Abs(side);
		value = rollangle;
		if (side < rollspeed) 
			side = side * value / rollspeed;
		else 
			side = value;
		
		return side * sign;
	}

	public const float PUNCH_DAMPING = 9.0f;            // bigger number makes the response more damped, smaller is less damped
														// currently the system will overshoot, with larger damping values it won't
	public const float PUNCH_SPRING_CONSTANT = 65.0f;   // bigger number increases the speed at which the view corrects

	protected virtual void DecayPunchAngle() {
		if (Player.Local.PunchAngle.LengthSqr() > 0.001 || Player.Local.PunchAngleVel.LengthSqr() > 0.001) {
			Player.Local.PunchAngle += Player.Local.PunchAngleVel * (float)gpGlobals.FrameTime;
			float damping = 1 - (PUNCH_DAMPING * (float)gpGlobals.FrameTime);

			if (damping < 0) 
				damping = 0;
			
			Player.Local.PunchAngleVel *= damping;

			// torsional spring
			// UNDONE: Per-axis spring constant?
			float springForceMagnitude = PUNCH_SPRING_CONSTANT * (float)gpGlobals.FrameTime;
			springForceMagnitude = Math.Clamp(springForceMagnitude, 0f, 2f);
			Player.Local.PunchAngleVel -= Player.Local.PunchAngle * springForceMagnitude;

			// don't wrap around
			Player.Local.PunchAngle.Init(
				Math.Clamp(Player.Local.PunchAngle.X, -89f, 89f),
				Math.Clamp(Player.Local.PunchAngle.Y, -179f, 179f),
				Math.Clamp(Player.Local.PunchAngle.Z, -89f, 89f));
		}
		else {
			Player.Local.PunchAngle.Init(0, 0, 0);
			Player.Local.PunchAngleVel.Init(0, 0, 0);
		}
	}

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

	// Only used by players.  Moves along the ground when player is a MoveType.Walk.
	protected virtual void WalkMove() { throw new NotImplementedException(); }

	// Try to keep a walking player on the ground when running down slopes etc
	protected void StayOnGround() { throw new NotImplementedException(); }

	// Handle MoveType.Walk.
	protected virtual void FullWalkMove() { throw new NotImplementedException(); }

	// allow overridden versions to respond to jumping
	protected virtual void OnJump(float fImpulse) { }
	protected virtual void OnLand(float fVelocity) { }

	// Implement this if you want to know when the player collides during OnPlayerMove
	protected virtual void OnTryPlayerMoveCollision(ref Trace tr) { }

	protected virtual Vector3 GetPlayerMins() {
		if (Player.IsObserver()) 
			return VEC_OBS_HULL_MIN_SCALED(Player);
		else 
			return Player.Local.Ducked ? VEC_DUCK_HULL_MIN_SCALED(Player) : VEC_HULL_MIN_SCALED(Player);
	}
	protected virtual Vector3 GetPlayerMaxs() {
		if (Player.IsObserver()) 
			return VEC_OBS_HULL_MAX_SCALED(Player);
		else 
			return Player.Local.Ducked ? VEC_DUCK_HULL_MAX_SCALED(Player) : VEC_HULL_MAX_SCALED(Player);
	} 

	protected enum IntervalType
	{
		Ground = 0,
		Stuck,
		Ladder
	}


	// Roughly how often we want to update the info about the ground surface we're on.
	// We don't need to do this very often.
	public const float CATEGORIZE_GROUND_SURFACE_INTERVAL = 0.3f;
	public static int CATEGORIZE_GROUND_SURFACE_TICK_INTERVAL => ((int)(CATEGORIZE_GROUND_SURFACE_INTERVAL / TICK_INTERVAL));


	public const float CHECK_STUCK_INTERVAL = 1.0f;
	public static int CHECK_STUCK_TICK_INTERVAL => ((int)(CHECK_STUCK_INTERVAL / TICK_INTERVAL));


	public const float CHECK_STUCK_INTERVAL_SP = 0.2f;
	public static int CHECK_STUCK_TICK_INTERVAL_SP => ((int)(CHECK_STUCK_INTERVAL_SP / TICK_INTERVAL));


	public const float CHECK_LADDER_INTERVAL = 0.2f;
	public static int CHECK_LADDER_TICK_INTERVAL => ((int)(CHECK_LADDER_INTERVAL / TICK_INTERVAL));


	// Useful for things that happen periodically. This lets things happen on the specified interval, but
	// spaces the events onto different frames for different players so they don't all hit their spikes
	// simultaneously.
	protected bool CheckInterval(IntervalType type) {
		int tickInterval = GetCheckInterval(type);

		if (g_bMovementOptimizations) 
			return (Player!.CurrentCommandNumber() + Player.EntIndex()) % tickInterval == 0;
		else 
			return true;
	}

	protected virtual int GetCheckInterval(IntervalType type) {
		int tickInterval = 1;
		switch (type) {
			default:
				tickInterval = 1;
				break;
			case IntervalType.Ground:
				tickInterval = CATEGORIZE_GROUND_SURFACE_TICK_INTERVAL;
				break;
			case IntervalType.Stuck:
				// If we are in the process of being "stuck", then try a new position every command tick until m_StuckLast gets reset back down to zero
				if (Player!.StuckLast != 0) {
					tickInterval = 1;
				}
				else {
					if (gpGlobals.MaxClients == 1) {
						tickInterval = CHECK_STUCK_TICK_INTERVAL_SP;
					}
					else {
						tickInterval = CHECK_STUCK_TICK_INTERVAL;
					}
				}
				break;
			case IntervalType.Ladder:
				tickInterval = CHECK_LADDER_TICK_INTERVAL;
				break;
		}
		return tickInterval;
	}


	// Decompoosed gravity
	protected void StartGravity() { throw new NotImplementedException(); }
	protected void FinishGravity() { throw new NotImplementedException(); }

	// Apply normal ( undecomposed ) gravity
	protected void AddGravity() { throw new NotImplementedException(); }

	// Handle movement in noclip mode.
	protected void FullNoClipMove(float factor, float maxacceleration) {  }

	// Returns true if he started a jump (ie: should he play the jump animation)?
	protected virtual bool CheckJumpButton() { throw new NotImplementedException(); }    // Overridden by each game.

	// Dead player flying through air., e.g.
	protected virtual void FullTossMove() { throw new NotImplementedException(); }

	// Player is a Observer chasing another player
	protected void FullObserverMove() { throw new NotImplementedException(); }

	// Handle movement when in MoveType.Ladder mode.
	protected virtual void FullLadderMove() { throw new NotImplementedException(); }

	// The basic solid body movement clip that slides along multiple planes
	protected virtual int TryPlayerMove(ref Vector3 firstDest, ref Trace firstTrace) { throw new NotImplementedException(); }

	protected virtual bool LadderMove() {
		Trace pm;
		bool onFloor;
		Vector3 floor = default;
		Vector3 wishdir = default;
		Vector3 end = default;

		if (Player.GetMoveType() == MoveType.Noclip)
			return false;

		if (!GameHasLadders())
			return false;

		// If I'm already moving on a ladder, use the previous ladder direction
		if (Player.GetMoveType() == MoveType.Ladder) 
			wishdir = -Player.LadderNormal;
		else {
			// otherwise, use the direction player is attempting to move
			if (mv!.ForwardMove != 0 || mv.SideMove != 0) {
				for (int i = 0; i < 3; i++)       // Determine x and y parts of velocity
					wishdir[i] = Forward[i] * mv.ForwardMove + Right[i] * mv.SideMove;

				MathLib.VectorNormalize(ref wishdir);
			}
			else {
				// Player is not attempting to move, no ladder behavior
				return false;
			}
		}

		return false; // todo
	}
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
	protected virtual bool CheckStuck() { return false; /* TODO */ }

	// Check if the point is in water.
	// Sets refWaterLevel and refWaterType appropriately.
	// If in water, applies current to baseVelocity, and returns true.
	protected virtual bool CheckWater() {
		Vector3 point = default;
		Contents cont;

		Vector3 vPlayerMins = GetPlayerMins();
		Vector3 vPlayerMaxs = GetPlayerMaxs();

		// Pick a spot just above the players feet.
		point[0] = mv!.GetAbsOrigin()[0] + (vPlayerMins[0] + vPlayerMaxs[0]) * 0.5f;
		point[1] = mv!.GetAbsOrigin()[1] + (vPlayerMins[1] + vPlayerMaxs[1]) * 0.5f;
		point[2] = mv!.GetAbsOrigin()[2] + vPlayerMins[2] + 1;

		// Assume that we are not in water at all.
		Player.SetWaterLevel(WaterLevel.NotInWater);
		Player.SetWaterType(Contents.Empty);

		// Grab point contents.
		cont = GetPointContentsCached(point, 0);

		// Are we under water? (not solid and not empty?)
		if ((cont & (Contents)Mask.Water) != 0) {
			// Set water type
			Player.SetWaterType(cont);

			// We are at least at level one
			Player.SetWaterLevel(WaterLevel.Feet);

			// Now check a point that is at the player hull midpoint.
			point[2] = mv.GetAbsOrigin()[2] + (vPlayerMins[2] + vPlayerMaxs[2]) * 0.5f;
			cont = GetPointContentsCached(point, 1);
			// If that point is also under water...
			if ((cont & (Contents)Mask.Water) != 0) {
				// Set a higher water level.
				Player.SetWaterLevel(WaterLevel.Waist);

				// Now check the eye position.  (view_ofs is relative to the origin)
				point[2] = Player.GetAbsOrigin()[2] + Player.GetViewOffset()[2];
				cont = GetPointContentsCached(point, 2);
				if ((cont & (Contents)Mask.Water) != 0)
					Player.SetWaterLevel(WaterLevel.Eyes);  // In over our eyes
			}

			// Adjust velocity based on water current, if any.
			if ((cont & (Contents)Mask.Current) != 0) {
				Vector3 v = default;
				if ((cont & Contents.Current0) != 0)
					v[0] += 1;
				if ((cont & Contents.Current90) != 0)
					v[1] += 1;
				if ((cont & Contents.Current180) != 0)
					v[0] -= 1;
				if ((cont & Contents.Current270) != 0)
					v[1] -= 1;
				if ((cont & Contents.CurrentUp) != 0)
					v[2] += 1;
				if ((cont & Contents.CurrentDown) != 0)
					v[2] -= 1;

				// BUGBUG -- this depends on the value of an unspecified enumerated type
				// The deeper we are, the stronger the current.
				MathLib.VectorMA(Player.GetBaseVelocity(), 50.0f * (int)Player.GetWaterLevel(), v, out Vector3 temp);
				Player.SetBaseVelocity(temp);
			}
		}

		// if we just transitioned from not in water to in water, record the time it happened
		if ((WaterLevel.NotInWater == OldWaterLevel) && (Player.GetWaterLevel() > WaterLevel.NotInWater)) 
			WaterEntryTime = gpGlobals.CurTime;

		return (Player.GetWaterLevel() > WaterLevel.Feet);
	}

	// Determine if player is in water, on ground, etc.
	protected virtual void CategorizePosition() {
		Vector3 point = default;
		Trace pm;

		// Reset this each time we-recategorize, otherwise we have bogus friction when we jump into water and plunge downward really quickly
		Player.SurfaceFriction = 1.0f;

		// if the player hull point one unit down is solid, the player
		// is on ground

		// see if standing on something solid	

		// Doing this before we move may introduce a potential latency in water detection, but
		// doing it after can get us stuck on the bottom in water if the amount we move up
		// is less than the 1 pixel 'threshold' we're about to snap to.	Also, we'll call
		// this several times per frame, so we really need to avoid sticking to the bottom of
		// water on each call, and the converse case will correct itself if called twice.
		CheckWater();

		// observers don't have a ground entity
		if (Player.IsObserver())
			return;

		float flOffset = 2.0f;

		point[0] = mv!.GetAbsOrigin()[0];
		point[1] = mv!.GetAbsOrigin()[1];
		point[2] = mv!.GetAbsOrigin()[2] - flOffset;

		Vector3 bumpOrigin = mv!.GetAbsOrigin();

		// Shooting up really fast.  Definitely not on ground.
		// On ladder moving up, so not on ground either
		// NOTE: 145 is a jump.
		const float NON_JUMP_VELOCITY = 140.0f;

		float zvel = mv.Velocity[2];
		bool bMovingUp = zvel > 0.0f;
		bool bMovingUpRapidly = zvel > NON_JUMP_VELOCITY;
		float flGroundEntityVelZ = 0.0f;
		if (bMovingUpRapidly) {
			// Tracker 73219, 75878:  ywb 8/2/07
			// After save/restore (and maybe at other times), we can get a case where we were saved on a lift and 
			//  after restore we'll have a high local velocity due to the lift making our abs velocity appear high.  
			// We need to account for standing on a moving ground object in that case in order to determine if we really 
			//  are moving away from the object we are standing on at too rapid a speed.  Note that CheckJump already sets
			//  ground entity to NULL, so this wouldn't have any effect unless we are moving up rapidly not from the jump button.
			SharedBaseEntity? ground = Player.GetGroundEntity();
			if (ground != null) {
				flGroundEntityVelZ = ground.GetAbsVelocity().Z;
				bMovingUpRapidly = (zvel - flGroundEntityVelZ) > NON_JUMP_VELOCITY;
			}
		}

		// Was on ground, but now suddenly am not
		if (bMovingUpRapidly ||
			(bMovingUp && Player.GetMoveType() == MoveType.Ladder)) {
			SetGroundEntity(ref Trace.NULL);
		}
		else {
			// Try and move down.
			// TryTouchGround(bumpOrigin, point, GetPlayerMins(), GetPlayerMaxs(), Mask.PlayerSolid, CollisionGroup.PlayerMovement, out pm);
			// todo

#if !CLIENT_DLL

			//Adrian: vehicle code handles for us.
			if (Player.IsInAVehicle() == false) {
				// todo
			}
#endif
		}
	}

	protected virtual void CheckParameters() {
		ArgumentNullException.ThrowIfNull(mv);
		QAngle v_angle;

		if (Player.GetMoveType() != MoveType.Isometric &&
			 Player.GetMoveType() != MoveType.Noclip &&
			 Player.GetMoveType() != MoveType.Observer) {
			float spd;
			float maxspeed;

			spd = (mv.ForwardMove * mv.ForwardMove) +
				  (mv.SideMove * mv.SideMove) +
				  (mv.UpMove * mv.UpMove);

			maxspeed = mv.ClientMaxSpeed;
			if (maxspeed != 0.0)
				mv.MaxSpeed = Math.Min(maxspeed, mv.MaxSpeed);

			// Slow down by the speed factor
			float speedFactor = 1.0f;
			if (Player.SurfaceData != null)
				speedFactor = Player.SurfaceData.Game.MaxSpeedFactor;

			// If we have a constraint, slow down because of that too.
			float constraintSpeedFactor = ComputeConstraintSpeedFactor();
			if (constraintSpeedFactor < speedFactor)
				speedFactor = constraintSpeedFactor;

			mv.MaxSpeed *= speedFactor;

			if (g_bMovementOptimizations) {
				// Same thing but only do the sqrt if we have to.
				if ((spd != 0.0) && (spd > mv.MaxSpeed * mv.MaxSpeed)) {
					float fRatio = mv.MaxSpeed / MathF.Sqrt(spd);
					mv.ForwardMove *= fRatio;
					mv.SideMove *= fRatio;
					mv.UpMove *= fRatio;
				}
			}
			else {
				spd = MathF.Sqrt(spd);
				if ((spd != 0.0) && (spd > mv.MaxSpeed)) {
					float fRatio = mv.MaxSpeed / spd;
					mv.ForwardMove *= fRatio;
					mv.SideMove *= fRatio;
					mv.UpMove *= fRatio;
				}
			}
		}

		if ((Player.GetFlags() & EntityFlags.OnTrain) != 0 ||
			 (Player.GetFlags() & EntityFlags.OnTrain) != 0 ||
			 IsDead()) {
			mv.ForwardMove = 0;
			mv.SideMove = 0;
			mv.UpMove = 0;
		}

		DecayPunchAngle();

		// Take angles from command.
		if (!IsDead()) {
			v_angle = mv.Angles;
			v_angle = v_angle + Player.Local.PunchAngle;

			// Now adjust roll angle
			if (Player.GetMoveType() != MoveType.Isometric &&
				 Player.GetMoveType() != MoveType.Noclip) {
				mv.Angles[ROLL] = CalcRoll(v_angle, mv.Velocity, sv_rollangle.GetFloat(), sv_rollspeed.GetFloat());
			}
			else {
				mv.Angles[ROLL] = 0.0f; // v_angle[ ROLL ];
			}
			mv.Angles[PITCH] = v_angle[PITCH];
			mv.Angles[YAW] = v_angle[YAW];
		}
		else {
			mv.Angles = mv.OldAngles;
		}

		// Set dead player view_offset
		if (IsDead())
			Player.SetViewOffset(VEC_DEAD_VIEWHEIGHT_SCALED(Player));


		// Adjust client view angles to match values used on server.
		if (mv.Angles[YAW] > 180.0f)
			mv.Angles[YAW] -= 360.0f;
	}

	protected virtual void ReduceTimers() {
		float frame_msec = 1000.0f * (float)gpGlobals.FrameTime;

		if (Player.Local.DuckTime > 0) {
			Player.Local.DuckTime -= frame_msec;
			if (Player.Local.DuckTime < 0) {
				Player.Local.DuckTime = 0;
			}
		}
		if (Player.Local.DuckJumpTime > 0) {
			Player.Local.DuckJumpTime -= frame_msec;
			if (Player.Local.DuckJumpTime < 0) {
				Player.Local.DuckJumpTime = 0;
			}
		}
		if (Player.Local.JumpTime > 0) {
			Player.Local.JumpTime -= frame_msec;
			if (Player.Local.JumpTime < 0) {
				Player.Local.JumpTime = 0;
			}
		}
		if (Player.SwimSoundTime > 0) {
			Player.SwimSoundTime -= frame_msec;
			if (Player.SwimSoundTime < 0) 
				Player.SwimSoundTime = 0;
		}
	}

	protected virtual void CheckFalling() { throw new NotImplementedException(); }

	protected virtual void PlayerRoughLandingEffects(float fvol) { throw new NotImplementedException(); }

	protected void PlayerWaterSounds() { throw new NotImplementedException(); }

	protected void ResetGetPointContentsCache() {
		for (int slot = 0; slot < MAX_PC_CACHE_SLOTS; ++slot)
			for (int i = 0; i < Constants.MAX_PLAYERS; ++i)
				CachedGetPointContents[i, slot] = -9999;
	}
	protected Contents GetPointContentsCached(in Vector3 point, int slot) { return Contents.Empty; /* todo */ }


	public const float GAMEMOVEMENT_DUCK_TIME = 1000.0f;    // ms
	public const float GAMEMOVEMENT_JUMP_TIME = 510.0f; // ms approx - based on the 21 unit height jump
	public const float GAMEMOVEMENT_JUMP_HEIGHT = 21.0f;  // units
	public const float GAMEMOVEMENT_TIME_TO_UNDUCK = (TIME_TO_UNDUCK * 1000.0f);     // ms
	public const float GAMEMOVEMENT_TIME_TO_UNDUCK_INV = (GAMEMOVEMENT_DUCK_TIME - GAMEMOVEMENT_TIME_TO_UNDUCK);


	// Ducking
	protected virtual void Duck() {
		InButtons buttonsChanged = (mv!.OldButtons ^ mv.Buttons);  // These buttons have changed this frame
		InButtons buttonsPressed = buttonsChanged & mv.Buttons;           // The changed ones still down are "pressed"
		InButtons buttonsReleased = buttonsChanged & mv.OldButtons;       // The changed ones which were previously down are "released"

		// Check to see if we are in the air.
		bool bInAir = (Player.GetGroundEntity() == null);
		bool bInDuck = (Player.GetFlags() & EntityFlags.Ducking) != 0;
		bool bDuckJump = (Player.Local.JumpTime > 0.0f);
		bool bDuckJumpTime = (Player.Local.DuckJumpTime > 0.0f);

		if ((mv.Buttons & InButtons.Duck) != 0) 
			mv.OldButtons |= InButtons.Duck;
		else 
			mv.OldButtons &= ~InButtons.Duck;
		
		// Handle death.
		if (IsDead())
			return;

		// Slow down ducked players.
		HandleDuckingSpeedCrop();

		// If the player is holding down the duck button, the player is in duck transition, ducking, or duck-jumping.
		if ((mv.Buttons & InButtons.Duck) != 0 || Player.Local.Ducking || bInDuck || bDuckJump) {
			// DUCK
			if ((mv.Buttons & InButtons.Duck) != 0 || bDuckJump) {
				// Have the duck button pressed, but the player currently isn't in the duck position.
				if ((buttonsPressed & InButtons.Duck) != 0 && !bInDuck && !bDuckJump && !bDuckJumpTime) {
					Player.Local.DuckTime = GAMEMOVEMENT_DUCK_TIME;
					Player.Local.Ducking = true;
				}

				// The player is in duck transition and not duck-jumping.
				if (Player.Local.Ducking && !bDuckJump && !bDuckJumpTime) {
					float flDuckMilliseconds = Math.Max(0.0f, GAMEMOVEMENT_DUCK_TIME - (float)Player.Local.DuckTime);
					float flDuckSeconds = flDuckMilliseconds * 0.001f;

					// Finish in duck transition when transition time is over, in "duck", in air.
					if ((flDuckSeconds > TIME_TO_DUCK) || bInDuck || bInAir) {
						FinishDuck();
					}
					else {
						// Calc parametric time
						float flDuckFraction = MathLib.SimpleSpline(flDuckSeconds / TIME_TO_DUCK);
						SetDuckedEyeOffset(flDuckFraction);
					}
				}

				if (bDuckJump) {
					// Make the bounding box small immediately.
					if (!bInDuck) {
						StartUnDuckJump();
					}
					else {
						// Check for a crouch override.
						if ((mv.Buttons & InButtons.Duck) == 0) {
							Trace trace = default;
							if (CanUnDuckJump(ref trace)) {
								FinishUnDuckJump(ref trace);
								Player.Local.DuckJumpTime = (GAMEMOVEMENT_TIME_TO_UNDUCK * (1.0f - trace.Fraction)) + GAMEMOVEMENT_TIME_TO_UNDUCK_INV;
							}
						}
					}
				}
			}
			// UNDUCK (or attempt to...)
			else {
				if (Player.Local.InDuckJump) {
					// Check for a crouch override.
					if ((mv.Buttons & InButtons.Duck) == 0) {
						Trace trace = default;
						if (CanUnDuckJump(ref trace)) {
							FinishUnDuckJump(ref trace);

							if (trace.Fraction < 1.0f) 
								Player.Local.DuckJumpTime = (GAMEMOVEMENT_TIME_TO_UNDUCK * (1.0f - trace.Fraction)) + GAMEMOVEMENT_TIME_TO_UNDUCK_INV;
						}
					}
					else {
						Player.Local.InDuckJump = false;
					}
				}

				if (bDuckJumpTime)
					return;

				// Try to unduck unless automovement is not allowed
				// NOTE: When not onground, you can always unduck
				if (Player.Local.AllowAutoMovement || bInAir || Player.Local.Ducking) {
					// We released the duck button, we aren't in "duck" and we are not in the air - start unduck transition.
					if ((buttonsReleased & InButtons.Duck) != 0) {
						if (bInDuck && !bDuckJump) {
							Player.Local.DuckTime = GAMEMOVEMENT_DUCK_TIME;
						}
						else if (Player.Local.Ducking && !Player.Local.Ducked) {
							// Invert time if release before fully ducked!!!
							float unduckMilliseconds = 1000.0f * TIME_TO_UNDUCK;
							float duckMilliseconds = 1000.0f * TIME_TO_DUCK;
							float elapsedMilliseconds = (float)(GAMEMOVEMENT_DUCK_TIME - Player.Local.DuckTime);

							float fracDucked = elapsedMilliseconds / duckMilliseconds;
							float remainingUnduckMilliseconds = fracDucked * unduckMilliseconds;

							Player.Local.DuckTime = GAMEMOVEMENT_DUCK_TIME - unduckMilliseconds + remainingUnduckMilliseconds;
						}
					}


					// Check to see if we are capable of unducking.
					if (CanUnduck()) {
						// or unducking
						if ((Player.Local.Ducking || Player.Local.Ducked)) {
							float flDuckMilliseconds = Math.Max(0.0f, GAMEMOVEMENT_DUCK_TIME - (float)Player.Local.DuckTime);
							float flDuckSeconds = flDuckMilliseconds * 0.001f;

							// Finish ducking immediately if duck time is over or not on ground
							if (flDuckSeconds > TIME_TO_UNDUCK || (bInAir && !bDuckJump)) {
								FinishUnDuck();
							}
							else {
								// Calc parametric time
								float flDuckFraction = MathLib.SimpleSpline(1.0f - (flDuckSeconds / TIME_TO_UNDUCK));
								SetDuckedEyeOffset(flDuckFraction);
								Player.Local.Ducking = true;
							}
						}
					}
					else {
						// Still under something where we can't unduck, so make sure we reset this timer so
						//  that we'll unduck once we exit the tunnel, etc.
						if (Player.Local.DuckTime != GAMEMOVEMENT_DUCK_TIME) {
							SetDuckedEyeOffset(1.0f);
							Player.Local.DuckTime = GAMEMOVEMENT_DUCK_TIME;
							Player.Local.Ducked = true;
							Player.Local.Ducking = false;
							Player.AddFlag(EntityFlags.Ducking);
						}
					}
				}
			}
		}
		// HACK: (jimd 5/25/2006) we have a reoccuring bug (#50063 in Tracker) where the player's
		// view height gets left at the ducked height while the player is standing, but we haven't
		// been  able to repro it to find the cause.  It may be fixed now due to a change I'm
		// also making in UpdateDuckJumpEyeOffset but just in case, this code will sense the 
		// problem and restore the eye to the proper position.  It doesn't smooth the transition,
		// but it is preferable to leaving the player's view too low.
		//
		// If the player is still alive and not an observer, check to make sure that
		// his view height is at the standing height.
		else if (!IsDead() && !Player.IsObserver() && !Player.IsInAVehicle()) {
			if ((Player.Local.DuckJumpTime == 0.0f) && (MathF.Abs(Player.GetViewOffset().Z - GetPlayerViewOffset(false).Z) > 0.1)) {
				// we should rarely ever get here, so assert so a coder knows when it happens
				Assert(false);
				DevMsg(1, "Restoring player view height\n");

				// set the eye height to the non-ducked height
				SetDuckedEyeOffset(0.0f);
			}
		}
	}
	protected virtual void HandleDuckingSpeedCrop() {
		if ((SpeedCropped & SpeedCropped.Duck) == 0 && (Player.GetFlags() & EntityFlags.Ducking) != 0 && (Player.GetGroundEntity() != null)) {
			float frac = 0.33333333f;
			mv.ForwardMove *= frac;
			mv.SideMove *= frac;
			mv.UpMove *= frac;
			SpeedCropped |= SpeedCropped.Duck;
		}
	}
	protected virtual void FinishUnDuck() {
		int i;
		Trace trace;
		Vector3 newOrigin = mv!.GetAbsOrigin();

		if (Player.GetGroundEntity() != null) 
			for (i = 0; i < 3; i++) 
				newOrigin[i] += (VEC_DUCK_HULL_MIN_SCALED(Player)[i] - VEC_HULL_MIN_SCALED(Player)[i]);
		else {
			// If in air an letting go of crouch, make sure we can offset origin to make
			//  up for uncrouching
			Vector3 hullSizeNormal = VEC_HULL_MAX_SCALED(Player) - VEC_HULL_MIN_SCALED(Player);
			Vector3 hullSizeCrouch = VEC_DUCK_HULL_MAX_SCALED(Player) - VEC_DUCK_HULL_MIN_SCALED(Player);
			Vector3 viewDelta = (hullSizeNormal - hullSizeCrouch);
			viewDelta = Vector3.Negate(viewDelta);
			MathLib.VectorAdd(newOrigin, viewDelta, out newOrigin);
		}

		Player.Local.Ducked = false;
		Player.RemoveFlag(EntityFlags.Ducking);
		Player.Local.Ducking = false;
		Player.Local.InDuckJump = false;
		Player.SetViewOffset(GetPlayerViewOffset(false));
		Player.Local.DuckTime = 0;

		mv.SetAbsOrigin(newOrigin);

#if CLIENT_DLL

		Player.ResetLatched();
#endif // CLIENT_DLL

		// Recategorize position since ducking can change origin
		CategorizePosition();
	}
	protected virtual void FinishDuck() {
		if ((Player.GetFlags() & EntityFlags.Ducking) != 0)
			return;

		Player.AddFlag(EntityFlags.Ducking);
		Player.Local.Ducked = true;
		Player.Local.Ducking = false;

		Player.SetViewOffset(GetPlayerViewOffset(true));

		// HACKHACK - Fudge for collision bug - no time to fix this properly
		if (Player.GetGroundEntity() != null) {
			for (int i = 0; i < 3; i++) {
				Vector3 org = mv!.GetAbsOrigin();
				org[i] -= (VEC_DUCK_HULL_MIN_SCALED(Player)[i] - VEC_HULL_MIN_SCALED(Player)[i]);
				mv.SetAbsOrigin(org);
			}
		}
		else {
			Vector3 hullSizeNormal = VEC_HULL_MAX_SCALED(Player) - VEC_HULL_MIN_SCALED(Player);
			Vector3 hullSizeCrouch = VEC_DUCK_HULL_MAX_SCALED(Player) - VEC_DUCK_HULL_MIN_SCALED(Player);
			Vector3 viewDelta = (hullSizeNormal - hullSizeCrouch);
			Vector3 @out;
			MathLib.VectorAdd(mv!.GetAbsOrigin(), viewDelta, out @out);
			mv.SetAbsOrigin(@out );

#if CLIENT_DLL
		Player.ResetLatched();
#endif 
		}

		// See if we are stuck?
		FixPlayerCrouchStuck(true);

		// Recategorize position since ducking can change origin
		CategorizePosition();
	}
	protected virtual bool CanUnduck() {
		// todo
		return true;
	}
	protected void UpdateDuckJumpEyeOffset() {
		if (Player.Local.DuckJumpTime != 0.0f) {
			float flDuckMilliseconds = Math.Max(0.0f, GAMEMOVEMENT_DUCK_TIME - (float)Player.Local.DuckJumpTime);
			float flDuckSeconds = flDuckMilliseconds / GAMEMOVEMENT_DUCK_TIME;
			if (flDuckSeconds > TIME_TO_UNDUCK) {
				Player.Local.DuckJumpTime = 0.0f;
				SetDuckedEyeOffset(0.0f);
			}
			else {
				float flDuckFraction = MathLib.SimpleSpline(1.0f - (flDuckSeconds / TIME_TO_UNDUCK));
				SetDuckedEyeOffset(flDuckFraction);
			}
		}
	}
	protected bool CanUnDuckJump(ref Trace trace) { return false; }
	protected void StartUnDuckJump() { throw new NotImplementedException(); }
	protected void FinishUnDuckJump(ref Trace trace) { throw new NotImplementedException(); }
	protected void SetDuckedEyeOffset(float duckFraction) {
		Vector3 vDuckHullMin = GetPlayerMins(true);
		Vector3 vStandHullMin = GetPlayerMins(false);

		float fMore = (vDuckHullMin.Z - vStandHullMin.Z);

		Vector3 vecDuckViewOffset = GetPlayerViewOffset(true);
		Vector3 vecStandViewOffset = GetPlayerViewOffset(false);
		Vector3 temp = Player.GetViewOffset();
		temp.Z = ((vecDuckViewOffset.Z - fMore) * duckFraction) +
					(vecStandViewOffset.Z * (1 - duckFraction));
		Player.SetViewOffset(temp);
	}
	protected void FixPlayerCrouchStuck(bool moveup) {
		// todo
	}

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
