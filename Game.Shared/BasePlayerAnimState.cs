#if CLIENT_DLL || GAME_DLL

global using static Game.Shared.PlayerAnimStateGlobals;

#if CLIENT_DLL
global using BaseAnimatingOverlay = Game.Client.C_BaseAnimatingOverlay;

#else
global using BaseAnimatingOverlay = Game.Server.BaseAnimatingOverlay;
#endif

using Source.Common.Commands;
using Source.Common.Mathematics;
using Source.Common;

using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

using Game.Server;
using CommunityToolkit.HighPerformance;
using Source;

namespace Game.Shared;

public static class PlayerAnimStateGlobals
{
#if CLIENT_DLL

	public static readonly ConVar cl_showanimstate = new("cl_showanimstate", "-1", FCvar.Cheat | FCvar.DevelopmentOnly, "Show the (client) animation state for the specified entity (-1 for none).");
	public static readonly ConVar showanimstate_log = new("cl_showanimstate_log", "0", FCvar.Cheat | FCvar.DevelopmentOnly, "1 to output cl_showanimstate to Msg(). 2 to store in AnimStateClient.log. 3 for both.");
#else
	public static readonly ConVar sv_showanimstate = new("sv_showanimstate", "-1", FCvar.Cheat | FCvar.DevelopmentOnly, "Show the (server) animation state for the specified entity (-1 for none).");
	public static readonly ConVar showanimstate_log = new("sv_showanimstate_log", "0", FCvar.Cheat | FCvar.DevelopmentOnly, "1 to output sv_showanimstate to Msg(). 2 to store in AnimStateServer.log. 3 for both.");
#endif

	// If a guy is moving slower than this, then he's considered to not be moving
	// (so he goes to his idle animation at full playback rate rather than his walk 
	// animation at low playback rate).
	public const float MOVING_MINIMUM_SPEED = 0.5f;
	public const int MAIN_IDLE_SEQUENCE_LAYER = 0;  // For 8-way blended models, this layer blends an idle on top of the run/walk animation to simulate a 9-way blend.
													// For 9-way blended models, we don't use this layer.
	public const int AIMSEQUENCE_LAYER = 1;         // Aim sequence uses layers 0 and 1 for the weapon idle animation (needs 2 layers so it can blend).
	public const int NUM_AIMSEQUENCE_LAYERS = 4;    // Then it uses layers 2 and 3 to blend in the weapon run/walk/crouchwalk animation.
													// Below this many degrees, slow down turning rate linearly
	public const float FADE_TURN_DEGREES = 45.0f;
	// After this, need to start turning feet
	public const float MAX_TORSO_ANGLE = 70.0f;
	// Below this amount, don't play a turning animation/perform IK
	public const float MIN_TURN_ANGLE_REQUIRING_TURN_ANIMATION = 15.0f;

	public static readonly ConVar mp_feetyawrate = new("mp_feetyawrate", "720", FCvar.Replicated | FCvar.DevelopmentOnly, "How many degrees per second that we can turn our feet or upper body.");
	public static readonly ConVar mp_facefronttime = new("mp_facefronttime", "3", FCvar.Replicated | FCvar.DevelopmentOnly, "After this amount of time of standing in place but aiming to one side, go ahead and move feet to face upper body.");
	public static readonly ConVar mp_ik = new("mp_ik", "1", FCvar.Replicated | FCvar.DevelopmentOnly, "Use IK on in-place turns.");

	// Pose parameters stored for debugging.
	internal static float g_flLastBodyPitch, g_flLastBodyYaw, m_flLastMoveYaw;
}

public enum TurnMode
{
	None,
	Left,
	Right
}

public struct ModAnimConfig
{
	public float MaxBodyYawDegrees;
	public LegAnimType LegAnimType;
	public bool UseAimSequences;
}

public abstract partial class BasePlayerAnimState : IPlayerAnimState
{
	public BasePlayerAnimState() {
		EyeYaw = 0.0f;
		EyePitch = 0.0f;
		CurrentFeetYawInitialized = false;
		CurrentTorsoYaw = 0.0f;
		TurningInPlace = TurnMode.None;
		MaxGroundSpeed = 0.0f;
		StoredCycle = 0.0f;

		GaitYaw = 0.0f;
		GoalFeetYaw = 0.0f;
		CurrentFeetYaw = 0.0f;
		LastYaw = 0.0f;
		LastTurnTime = 0.0;
		AngRender.Init();
		LastMovePose.Init();
		Current8WayIdleSequence = -1;
		Current8WayCrouchIdleSequence = -1;

		Outer = null;
		CurrentMainSequenceActivity = Activity.ACT_IDLE;
		LastAnimationStateClearTime = 0.0;
	}

	protected ModAnimConfig AnimConfig;
	protected BaseAnimatingOverlay? Outer;
	protected float EyeYaw;
	protected float EyePitch;
	protected float GoalFeetYaw;
	protected float CurrentFeetYaw;
	protected bool CurrentFeetYawInitialized;
	protected float CurrentTorsoYaw;
	protected float LastYaw;
	protected TimeUnit_t LastTurnTime;
	protected TurnMode TurningInPlace;
	protected QAngle AngRender;

	private float MaxGroundSpeed;
	private TimeUnit_t LastAnimationStateClearTime;
	private int Current8WayIdleSequence;
	private int Current8WayCrouchIdleSequence;
	private Activity CurrentMainSequenceActivity;
	private float GaitYaw;
	private float StoredCycle;
	private Vector2 LastMovePose;

	private readonly SequenceTransitioner IdleSequenceTransitioner = new();
	private readonly SequenceTransitioner SequenceTransitioner = new();

	public void Init(BaseAnimatingOverlay player, in ModAnimConfig config) {
		Outer = player;
		AnimConfig = config;
		ClearAnimationState();
	}

	public void Release() { }

	public void ClearAnimationState() {
		ClearAnimationLayers();
		CurrentFeetYawInitialized = false;
		LastAnimationStateClearTime = gpGlobals.CurTime;
	}

	public TimeUnit_t TimeSinceLastAnimationStateClear() => gpGlobals.CurTime - LastAnimationStateClearTime;

	public void Update(float eyeYaw, float eyePitch) {
		ClearAnimationLayers();

		// Some mods don't want to update the player's animation state if they're dead and ragdolled.
		if (!ShouldUpdateAnimState()) {
			ClearAnimationState();
			return;
		}

		StudioHdr studioHdr = GetOuter()!.GetModelPtr()!;
		// Store these. All the calculations are based on them.
		EyeYaw = MathLib.AngleNormalize(eyeYaw);
		EyePitch = MathLib.AngleNormalize(eyePitch);

		// Compute sequences for all the layers.
		ComputeSequences(studioHdr);

		// Compute all the pose params.
		ComputePoseParam_BodyPitch(studioHdr); // Look up/down.
		ComputePoseParam_BodyYaw();     // Torso rotation.
		ComputePoseParam_MoveYaw(studioHdr);       // What direction his legs are running in.

		ComputePlaybackRate();
	}

	public bool ShouldUpdateAnimState() => GetOuter()!.IsAlive();

	public virtual bool ShouldChangeSequences() => true;
	public void SetOuterPoseParameter(int param, float value) => GetOuter()!.SetPoseParameter(param, value);

	public void ClearAnimationLayers() {
		if (Outer == null)
			return;

		Outer.SetNumAnimOverlays(AIMSEQUENCE_LAYER + NUM_AIMSEQUENCE_LAYERS);
		for (int i = 0; i < Outer.GetNumAnimOverlays(); i++) {
			Outer.GetAnimOverlay(i).SetOrder(MAX_OVERLAYS);
#if !CLIENT_DLL
			Outer.GetAnimOverlay(i).Flags = 0;
#endif
		}
	}

	public void RestartMainSequence() {
		BaseAnimatingOverlay player = GetOuter()!;
		player.AnimTime = gpGlobals.CurTime;
		player.SetCycle(0);
	}

	public void ComputeSequences(StudioHdr studioHdr) {
		ComputeMainSequence();      // Lower body (walk/run/idle).
		UpdateInterpolators();      // The groundspeed interpolator uses the main sequence info.

		if (AnimConfig.UseAimSequences)
			ComputeAimSequence();       // Upper body, based on weapon type.
	}

	public virtual Activity TranslateActivity(Activity desired) => desired;

	public abstract Activity CalcMainActivity();
	public abstract float GetCurrentMaxGroundSpeed();
	public abstract int CalcAimLayerSequence(ref TimeUnit_t cycle, out float aimSequenceWeight, bool forceIdle);

	public void ResetGroundSpeed() => MaxGroundSpeed = GetCurrentMaxGroundSpeed();

	public void ComputeMainSequence() {
		BaseAnimatingOverlay player = GetOuter()!;

		// Have our class or the mod-specific class determine what the current activity is.
		Activity idealActivity = CalcMainActivity();

#if CLIENT_DLL
		Activity oldActivity = CurrentMainSequenceActivity;
#endif

		// Store our current activity so the aim and fire layers know what to do.
		CurrentMainSequenceActivity = idealActivity;

		// Export to our outer class..
		int animDesired = SelectWeightedSequence(TranslateActivity(idealActivity));

#if !HL1_CLIENT_DLL && !HL1_DLL
		if (!ShouldResetMainSequence(player.GetSequence(), animDesired))
			return;
#endif

		if (animDesired < 0)
			animDesired = 0;

		player.ResetSequence(animDesired);

#if CLIENT_DLL
		// If we went from idle to walk, reset the interpolation history.
		// Kind of hacky putting this here.. it might belong outside the base class.
		if ((oldActivity == Activity.ACT_CROUCHIDLE || oldActivity == Activity.ACT_IDLE) &&
			 (idealActivity == Activity.ACT_WALK || idealActivity == Activity.ACT_RUN_CROUCH)) {
			ResetGroundSpeed();
		}
#endif
	}

	public bool ShouldResetMainSequence(int currentSequence, int newSequence) {
		if (GetOuter() == null)
			return false;

		return GetOuter()!.GetSequenceActivity(currentSequence) != GetOuter()!.GetSequenceActivity(newSequence);
	}

	public void UpdateAimSequenceLayers(TimeUnit_t cycle, int firstLayer, bool forceIdle, SequenceTransitioner transitioner, float weightScale) {
		float aimSequenceWeight = 1;
		int aimSequence = CalcAimLayerSequence(ref cycle, out aimSequenceWeight, forceIdle);
		if (aimSequence == -1)
			aimSequence = 0;

		// Feed the current state of the animation parameters to the sequence transitioner.
		// It will hand back either 1 or 2 animations in the queue to set, depending on whether
		// it's transitioning or not. We just dump those into the animation layers.
		transitioner.CheckForSequenceChange(
			Outer!.GetModelPtr(),
			aimSequence,
			false,  // don't force transitions on the same anim
			true    // yes, interpolate when transitioning
		);

		transitioner.UpdateCurrent(
			Outer!.GetModelPtr(),
			aimSequence,
			cycle,
			GetOuter()!.GetPlaybackRate(),
			gpGlobals.CurTime
		);

		AnimationLayerRef pDest0 = Outer!.GetAnimOverlay(firstLayer);
		AnimationLayerRef pDest1 = Outer!.GetAnimOverlay(firstLayer + 1);

		if (transitioner.AnimationQueue.Count == 1) {
			// If only 1 animation, then blend it in fully.
			ref AnimationLayer pSource0 = ref transitioner.AnimationQueue.AsSpan()[0];
			pDest0.Struct = pSource0;

			pDest0.Weight = 1;
			pDest1.Weight = 0;
			pDest0.Order = firstLayer;

#if !CLIENT_DLL
			pDest0.Flags |= AnimLayerFlags.Active;
#endif
		}
		else if (transitioner.AnimationQueue.Count >= 2) {
			// The first one should be fading out. Fade in the new one inversely.
			ref AnimationLayer pSource0 = ref transitioner.AnimationQueue.AsSpan()[0];
			ref AnimationLayer pSource1 = ref transitioner.AnimationQueue.AsSpan()[1];

			pDest0.Struct = pSource0;
			pDest1.Struct = pSource1;
			Assert(pDest0.Weight >= 0.0f && pDest0.Weight <= 1.0f);
			pDest1.Weight = 1 - pDest0.Weight;    // This layer just mirrors the other layer's weight (one fades in while the other fades out).

			pDest0.Order = firstLayer;
			pDest1.Order = firstLayer + 1;

#if !CLIENT_DLL
			pDest0.Flags |= AnimLayerFlags.Active;
			pDest1.Flags |= AnimLayerFlags.Active;
#endif
		}

		pDest0.Weight *= weightScale * aimSequenceWeight;
		pDest0.Weight = Math.Clamp((float)pDest0.Weight, 0.0f, 1.0f);

		pDest1.Weight *= weightScale * aimSequenceWeight;
		pDest1.Weight = Math.Clamp((float)pDest1.Weight, 0.0f, 1.0f);

		pDest0.Cycle = pDest1.Cycle = cycle;
	}

	public void OptimizeLayerWeights(int firstLayer, int layers) {
		// Find the total weight of the blended layers, not including the idle layer (iFirstLayer)
		float totalWeight = 0.0f;
		for (int i = 1; i < layers; i++) {
			AnimationLayerRef layer = Outer!.GetAnimOverlay(firstLayer + i);
			if (layer.IsActive() && layer.Weight > 0.0f)
				totalWeight += layer.Weight;
		}

		// Set the idle layer's weight to be 1 minus the sum of other layer weights
		AnimationLayerRef layerFirst = Outer!.GetAnimOverlay(firstLayer);
		if (layerFirst.IsActive() && layerFirst.Weight > 0.0f) {
			layerFirst.Weight = 1.0f - totalWeight;
			layerFirst.Weight = Math.Max((float)layerFirst.Weight, 0.0f);
		}

		// This part is just an optimization. Since we have the walk/run animations weighted on top of 
		// the idle animations, all this does is disable the idle animations if the walk/runs are at
		// full weighting, which is whenever a guy is at full speed.
		//
		// So it saves us blending a couple animation layers whenever a guy is walking or running full speed.
		int iLastOne = -1;
		for (int i = 0; i < layers; i++) {
			AnimationLayerRef layer = Outer.GetAnimOverlay(firstLayer + i);
			if (layer.IsActive() && layer.Weight > 0.99)
				iLastOne = i;
		}

		if (iLastOne != -1) {
			for (int i = iLastOne - 1; i >= 0; i--) {
				AnimationLayerRef layer = Outer.GetAnimOverlay(firstLayer + i);
#if CLIENT_DLL
				layer.Order = MAX_OVERLAYS;
#else
			// FIXME: NEED TO DO pLayer.Order.Set( MAX_OVERLAYS );
			layer.Flags = 0;
			throw new NotImplementedException();
#endif
			}
		}
	}

	public bool ShouldBlendAimSequenceToIdle() {
		Activity act = GetCurrentMainSequenceActivity();

		return (act == Activity.ACT_RUN || act == Activity.ACT_WALK || act == Activity.ACT_RUNTOIDLE || act == Activity.ACT_RUN_CROUCH);
	}

	public void ComputeAimSequence() {
		TimeUnit_t flCycle = Outer!.GetCycle();

		// Figure out the new cycle time.
		UpdateAimSequenceLayers(flCycle, AIMSEQUENCE_LAYER, true, IdleSequenceTransitioner, 1);

		if (ShouldBlendAimSequenceToIdle()) {
			// What we do here is blend between the idle upper body animation (like where he's got the dual elites
			// held out in front of him but he's not moving) and his walk/run/crouchrun upper body animation,
			// weighting it based on how fast he's moving. That way, when he's moving slowly, his upper 
			// body doesn't jiggle all around.
			float flPlaybackRate = CalcMovementPlaybackRate(out bool isMoving);
			if (isMoving)
				UpdateAimSequenceLayers(flCycle, AIMSEQUENCE_LAYER + 2, false, SequenceTransitioner, flPlaybackRate);
		}

		OptimizeLayerWeights(AIMSEQUENCE_LAYER, NUM_AIMSEQUENCE_LAYERS);
	}

	static readonly HashSet<UtlSymId_t> dict = [];
	public int CalcSequenceIndex(ReadOnlySpan<char> baseName) {
		int iSequence = GetOuter().LookupSequence(baseName);

		// Show warnings if we can't find anything here.
		if (iSequence == -1) {
			UtlSymId_t strhash = baseName.Hash(invariant: false);
			if (!dict.Contains(strhash)) {
				dict.Add(strhash);
				Warning($"CalcSequenceIndex: can't find '{baseName}'.\n");
			}

			iSequence = 0;
		}

		return iSequence;
	}

	public void UpdateInterpolators() {
		float curMaxSpeed = GetCurrentMaxGroundSpeed();
		MaxGroundSpeed = curMaxSpeed;
	}

	public float GetInterpolatedGroundSpeed() {
		return MaxGroundSpeed;
	}

	public float CalcMovementPlaybackRate(out bool outIsMoving) {
		GetOuterAbsVelocity(out Vector3 vel);
		float speed = vel.Length2D();
		bool isMoving = (speed > MOVING_MINIMUM_SPEED);

		outIsMoving = false;
		float flReturnValue = 1;

		if (isMoving && CanThePlayerMove()) {
			float flGroundSpeed = GetInterpolatedGroundSpeed();
			if (flGroundSpeed < 0.001f)
				flReturnValue = 0.01f;
			else
				// Note this gets set back to 1.0 if sequence changes due to ResetSequenceInfo below
				flReturnValue = Math.Clamp(speed / flGroundSpeed, 0.01f, 10f);  // don't go nuts here.

			outIsMoving = true;
		}

		return flReturnValue;
	}

	public virtual bool CanThePlayerMove() => true;

	public void ComputePlaybackRate() {
		if (AnimConfig.LegAnimType != LegAnimType.Anim9Way && AnimConfig.LegAnimType != LegAnimType.Anim8Way) {
			// When using a 9-way blend, playback rate is always 1 and we just scale the pose params
			// to speed up or slow down the animation.
			float flRate = CalcMovementPlaybackRate(out bool isMoving);
			if (isMoving)
				GetOuter()!.SetPlaybackRate(flRate);
			else
				GetOuter()!.SetPlaybackRate(1);
		}
	}

	public BaseAnimatingOverlay? GetOuter() {
		return Outer;
	}

	public void EstimateYaw() {
		GetOuterAbsVelocity(out Vector3 est_velocity);

		float flLength = est_velocity.Length2D();
		if (flLength > MOVING_MINIMUM_SPEED) {
			GaitYaw = MathF.Atan2(est_velocity[1], est_velocity[0]);
			GaitYaw = MathLib.RAD2DEG(GaitYaw);
			GaitYaw = MathLib.AngleNormalize(GaitYaw);
		}
	}

	public void ComputePoseParam_MoveYaw(StudioHdr studioHdr) {
		if (AnimConfig.LegAnimType == LegAnimType.AnimGoldSrc)
#if !CLIENT_DLL
			GetOuter()!.SetLocalAngles(new QAngle(0, CurrentFeetYaw, 0));
#endif

			// If using goldsrc-style animations where he's moving in the direction that his feet are facing,
			// we don't use move yaw.
			if (AnimConfig.LegAnimType != LegAnimType.Anim9Way && AnimConfig.LegAnimType != LegAnimType.Anim8Way)
				return;

		// view direction relative to movement
		float flYaw;

		EstimateYaw();

		float ang = EyeYaw;
		if (ang > 180.0f)
			ang -= 360.0f;
		else if (ang < -180.0f)
			ang += 360.0f;

		// calc side to side turning
		flYaw = ang - GaitYaw;
		// Invert for mapping into 8way blend
		flYaw = -flYaw;
		flYaw = flYaw - (int)(flYaw / 360) * 360;

		if (flYaw < -180)
			flYaw = flYaw + 360;
		else if (flYaw > 180)
			flYaw = flYaw - 360;

		if (AnimConfig.LegAnimType == LegAnimType.Anim9Way) {
#if !CLIENT_DLL
			GetOuter()!.SetLocalAngles(new QAngle(0, CurrentFeetYaw, 0));
#endif

			int iMoveX = GetOuter().LookupPoseParameter(studioHdr, "move_x");
			int iMoveY = GetOuter().LookupPoseParameter(studioHdr, "move_y");
			if (iMoveX < 0 || iMoveY < 0)
				return;

			bool isMoving;
			float flPlaybackRate = CalcMovementPlaybackRate(out isMoving);

			// Setup the 9-way blend parameters based on our speed and direction.
			Vector2 vCurMovePose = new(0, 0);

			if (isMoving) {
				vCurMovePose.X = MathF.Cos(MathLib.DEG2RAD(flYaw)) * flPlaybackRate;
				vCurMovePose.Y = -MathF.Sin(MathLib.DEG2RAD(flYaw)) * flPlaybackRate;
			}

			GetOuter().SetPoseParameter(studioHdr, iMoveX, vCurMovePose.X);
			GetOuter().SetPoseParameter(studioHdr, iMoveY, vCurMovePose.Y);

			LastMovePose = vCurMovePose;
		}
		else {
			int iMoveYaw = GetOuter().LookupPoseParameter(studioHdr, "move_yaw");
			if (iMoveYaw >= 0) {
				GetOuter().SetPoseParameter(studioHdr, iMoveYaw, flYaw);
				m_flLastMoveYaw = flYaw;

				// Now blend in his idle animation.
				// This makes the 8-way blend act like a 9-way blend by blending to 
				// an idle sequence as he slows down.
#if CLIENT_DLL
				bool bIsMoving;
				AnimationLayerRef layer = Outer!.GetAnimOverlay(MAIN_IDLE_SEQUENCE_LAYER);

				layer.Weight = 1 - CalcMovementPlaybackRate(out bIsMoving);
				if (!bIsMoving)
					layer.Weight = 1;


				if (ShouldChangeSequences()) {
					// Whenever this layer stops blending, we can choose a new idle sequence to blend to, so he 
					// doesn't always use the same idle.
					if (layer.Weight < 0.02f || Current8WayIdleSequence == -1) {
						Current8WayIdleSequence = Outer.SelectWeightedSequence(Activity.ACT_IDLE);
						Current8WayCrouchIdleSequence = Outer.SelectWeightedSequence(Activity.ACT_CROUCHIDLE);
					}

					if (CurrentMainSequenceActivity == Activity.ACT_CROUCHIDLE || CurrentMainSequenceActivity == Activity.ACT_RUN_CROUCH)
						layer.Sequence = Current8WayCrouchIdleSequence;
					else
						layer.Sequence = Current8WayIdleSequence;
				}

				layer.PlaybackRate = 1;
				layer.Cycle += Outer!.GetSequenceCycleRate(studioHdr, layer.Sequence) * gpGlobals.FrameTime;
				layer.Cycle = MathLib.Fmod(layer.Cycle, 1);
				layer.Order = MAIN_IDLE_SEQUENCE_LAYER;
#endif
			}
		}
	}

	public void ComputePoseParam_BodyPitch(StudioHdr studioHdr) {
		// Get pitch from v_angle
		float flPitch = EyePitch;
		if (flPitch > 180.0f)
			flPitch -= 360.0f;

		flPitch = Math.Clamp(flPitch, -90f, 90f);

		// See if we have a blender for pitch
		int pitch = GetOuter()!.LookupPoseParameter(studioHdr, "body_pitch");
		if (pitch < 0)
			return;

		GetOuter()!.SetPoseParameter(studioHdr, pitch, flPitch);
		g_flLastBodyPitch = flPitch;
	}

	public TurnMode ConvergeAngles(float goal, float maxrate, float maxgap, TimeUnit_t dt, ref float current) {
		TurnMode direction = TurnMode.None;

		float anglediff = goal - current;
		anglediff = MathLib.AngleNormalize(anglediff);

		float anglediffabs = MathF.Abs(anglediff);

		float scale = 1.0f;
		if (anglediffabs <= FADE_TURN_DEGREES) {
			scale = anglediffabs / FADE_TURN_DEGREES;
			// Always do at least a bit of the turn ( 1% )
			scale = Math.Clamp(scale, 0.01f, 1.0f);
		}

		float maxmove = (float)(maxrate * dt * scale);

		if (anglediffabs > maxgap) {
			// gap is too big, jump
			maxmove = (anglediffabs - maxgap);
		}

		if (anglediffabs < maxmove) {
			// we are close enought, just set the final value
			current = goal;
		}
		else {
			// adjust value up or down
			if (anglediff > 0) {
				current += maxmove;
				direction = TurnMode.Left;
			}
			else {
				current -= maxmove;
				direction = TurnMode.Right;
			}
		}

		current = MathLib.AngleNormalize(current);

		return direction;
	}

	public void ComputePoseParam_BodyYaw() {
		GetOuterAbsVelocity(out Vector3 vel);
		bool bIsMoving = vel.Length2D() > MOVING_MINIMUM_SPEED;

		// If we just initialized this guy (maybe he just came into the PVS), then immediately
		// set his feet in the right direction, otherwise they'll spin around from 0 to the 
		// right direction every time someone switches spectator targets.
		if (!CurrentFeetYawInitialized) {
			CurrentFeetYawInitialized = true;
			GoalFeetYaw = CurrentFeetYaw = EyeYaw;
			LastTurnTime = 0.0f;
		}
		else if (bIsMoving) {
			// player is moving, feet yaw = aiming yaw
			if (AnimConfig.LegAnimType == LegAnimType.Anim9Way || AnimConfig.LegAnimType == LegAnimType.Anim8Way) {
				// His feet point in the direction his eyes are, but they can run in any direction.
				GoalFeetYaw = EyeYaw;
			}
			else {
				GoalFeetYaw = MathLib.RAD2DEG(MathF.Atan2(vel.Y, vel.X));

				// If he's running backwards, flip his feet backwards.
				Vector3 vEyeYaw = new(MathF.Cos(MathLib.DEG2RAD(EyeYaw)), MathF.Sin(MathLib.DEG2RAD(EyeYaw)), 0);
				Vector3 vFeetYaw = new(MathF.Cos(MathLib.DEG2RAD(GoalFeetYaw)), MathF.Sin(MathLib.DEG2RAD(GoalFeetYaw)), 0);
				if (vEyeYaw.Dot(vFeetYaw) < -0.01)
					GoalFeetYaw += 180;
			}

		}
		else if ((gpGlobals.CurTime - LastTurnTime) > mp_facefronttime.GetFloat()) {
			// player didn't move & turn for quite some time
			GoalFeetYaw = EyeYaw;
		}
		else {
			// If he's rotated his view further than the model can turn, make him face forward.
			float flDiff = MathLib.AngleNormalize(GoalFeetYaw - EyeYaw);

			if (MathF.Abs(flDiff) > AnimConfig.MaxBodyYawDegrees) {
				if (flDiff > 0)
					GoalFeetYaw -= AnimConfig.MaxBodyYawDegrees;
				else
					GoalFeetYaw += AnimConfig.MaxBodyYawDegrees;
			}
		}

		GoalFeetYaw = MathLib.AngleNormalize(GoalFeetYaw);

		if (CurrentFeetYaw != GoalFeetYaw) {
			ConvergeAngles(GoalFeetYaw, mp_feetyawrate.GetFloat(), AnimConfig.MaxBodyYawDegrees,
				 gpGlobals.FrameTime, ref CurrentFeetYaw);

			LastTurnTime = gpGlobals.CurTime;
		}

		float flCurrentTorsoYaw = MathLib.AngleNormalize(EyeYaw - CurrentFeetYaw);

		// Rotate entire body into position
		AngRender[YAW] = CurrentFeetYaw;
		AngRender[PITCH] = AngRender[ROLL] = 0;

		SetOuterBodyYaw(flCurrentTorsoYaw);
		g_flLastBodyYaw = flCurrentTorsoYaw;
	}

	public float SetOuterBodyYaw(float value) {
		int body_yaw = GetOuter()!.LookupPoseParameter("body_yaw");
		if (body_yaw < 0) 
			return 0;

		SetOuterPoseParameter(body_yaw, value);
		return value;
	}

	public Activity BodyYawTranslateActivity(Activity activity) {
		if (activity != Activity.ACT_IDLE)
			return activity;

		// Not turning
		switch (TurningInPlace) {
			default:
			case TurnMode.None:
				return activity;
			case TurnMode.Right:
			case TurnMode.Left:
				return mp_ik.GetBool() ? Activity.ACT_TURN : activity;
		}
	}

	public ref readonly QAngle GetRenderAngles() => ref AngRender;

	public void GetOuterAbsVelocity(out Vector3 vel) {
#if CLIENT_DLL
	GetOuter()!.EstimateAbsVelocity(out  vel );
#else
		vel = GetOuter()!.GetAbsVelocity();
#endif
	}

	public float GetOuterXYSpeed() {
		GetOuterAbsVelocity(out Vector3 vel);
		return vel.Length2D();
	}

	public int SelectWeightedSequence(Activity activity) => GetOuter()!.SelectWeightedSequence(activity);

	public Activity GetCurrentMainSequenceActivity() => CurrentMainSequenceActivity;
}

#endif
