#if CLIENT_DLL || GAME_DLL

#if CLIENT_DLL
using Game.Client;
#endif

using Source;
using Source.Common;
using Source.Common.Mathematics;

using System.Numerics;

namespace Game.Shared;

public class HL2MPPlayerAnimState : MultiPlayerAnimState
{
	HL2MP_Player? HL2MPPlayer;
	bool InAirWalk;
	TimeUnit_t HoldDeployedPoseUntilTime;

	public const float HL2MP_RUN_SPEED = 320.0f;
	public const float HL2MP_WALK_SPEED = 75.0f;
	public const float HL2MP_CROUCHWALK_SPEED = 110.0f;

	public static HL2MPPlayerAnimState CreateHL2MPPlayerAnimState(HL2MP_Player player) {
		MultiPlayerMovementData movementData = default;
		movementData.BodyYawRate = 720.0f;
		movementData.RunSpeed = HL2MP_RUN_SPEED;
		movementData.WalkSpeed = HL2MP_WALK_SPEED;
		movementData.SprintSpeed = -1.0f;

		HL2MPPlayerAnimState ret = new(player, ref movementData);

		ret.InitHL2MPAnimState(player);

		return ret;
	}

	public HL2MPPlayerAnimState() => HL2MPPlayer = null;

	public HL2MPPlayerAnimState(BasePlayer player, ref MultiPlayerMovementData movementData) : base(player, ref movementData) => HL2MPPlayer = null;

	public void InitHL2MPAnimState(HL2MP_Player player) => HL2MPPlayer = player;

	public override void ClearAnimationState() => base.ClearAnimationState();

	public override Activity TranslateActivity(Activity actDesired) {
		Activity translateActivity = actDesired;

		bool required = false;
		if (GetHL2MPPlayer().GetActiveWeapon() != null)
			translateActivity = GetHL2MPPlayer().GetActiveWeapon()!.ActivityOverride(translateActivity, ref required);

		return translateActivity;
	}

	public override void Update(float eyeYaw, float eyePitch) {
		HL2MP_Player? hl2mpPlayer = GetHL2MPPlayer();
		if (hl2mpPlayer == null)
			return;

		StudioHdr? studioHdr = hl2mpPlayer.GetModelPtr();
		if (studioHdr == null)
			return;

		if (!ShouldUpdateAnimState()) {
			ClearAnimationState();
			return;
		}

		EyeYaw = MathLib.AngleNormalize(eyeYaw);
		EyePitch = MathLib.AngleNormalize(eyePitch);

		ComputeSequences(studioHdr);

		if (SetupPoseParameters(studioHdr)) {
			ComputePoseParam_MoveYaw(studioHdr);
			ComputePoseParam_AimPitch(studioHdr);
			ComputePoseParam_AimYaw(studioHdr);
		}

#if CLIENT_DLL
		if (C_BasePlayer.ShouldDrawLocalPlayer())
			HL2MPPlayer!.SetPlaybackRate(1.0f);
#endif
	}
	public override void DoAnimationEvent(PlayerAnimEvent evt, int data = 0) => throw new NotImplementedException();
	protected override bool HandleSwimming(ref Activity idealActivity) {
		bool inWater = base.HandleSwimming(ref idealActivity);

		return inWater;
	}

	protected override bool HandleMoving(ref Activity idealActivity) => base.HandleMoving(ref idealActivity);

	protected override bool HandleDucking(ref Activity idealActivity) {
		if ((HL2MPPlayer!.GetFlags() & EntityFlags.Ducking) != 0) {
			if (GetOuterXYSpeed() < MOVING_MINIMUM_SPEED)
				idealActivity = Activity.ACT_MP_CROUCH_IDLE;
			else
				idealActivity = Activity.ACT_MP_CROUCHWALK;

			return true;
		}

		return false;
	}

	protected override bool HandleJumping(ref Activity idealActivity) {
		GetOuterAbsVelocity(out Vector3 vecVelocity);

		if (Jumping) {
			const bool newJump = false;

			if (FirstJumpFrame) {
				FirstJumpFrame = false;
				RestartMainSequence();
			}

			if (HL2MPPlayer!.GetWaterLevel() >= WaterLevel.Waist) {
				Jumping = false;
				RestartMainSequence();
			}
			else if (gpGlobals.CurTime - JumpStartTime > 0.2f) {
				if ((HL2MPPlayer.GetFlags() & EntityFlags.OnGround) != 0) {
					Jumping = false;
					RestartMainSequence();

					if (newJump)
						RestartGesture((int)GestureSlotIndex.Jump, Activity.ACT_MP_JUMP_LAND);
				}
			}

			if (Jumping) {
				if (newJump) {
					if (gpGlobals.CurTime - JumpStartTime > 0.5)
						idealActivity = Activity.ACT_MP_JUMP_FLOAT;
					else
						idealActivity = Activity.ACT_MP_JUMP_START;
				}
				else
					idealActivity = Activity.ACT_MP_JUMP;
			}
		}

		if (Jumping)
			return true;

		return false;
	}

	new bool SetupPoseParameters(StudioHdr? studioHdr) {
		if (PoseParameterInit)
			return true;

		if (studioHdr == null)
			return false;

		PoseParameterData.MoveX = GetBasePlayer().LookupPoseParameter(studioHdr, "move_yaw");
		PoseParameterData.MoveY = GetBasePlayer().LookupPoseParameter(studioHdr, "move_yaw");
		if ((PoseParameterData.MoveX < 0) || (PoseParameterData.MoveY < 0))
			return false;

		PoseParameterData.AimPitch = GetBasePlayer().LookupPoseParameter(studioHdr, "aim_pitch");
		if (PoseParameterData.AimPitch < 0)
			return false;

		PoseParameterData.AimYaw = GetBasePlayer().LookupPoseParameter(studioHdr, "aim_yaw");
		if (PoseParameterData.AimYaw < 0)
			return false;

		PoseParameterInit = true;

		return true;
	}
	protected override void EstimateYaw() {
		TimeUnit_t deltaTime = gpGlobals.FrameTime;
		if (deltaTime == 0.0)
			return;

		TimeUnit_t dt = gpGlobals.FrameTime;

		GetOuterAbsVelocity(out Vector3 vecEstVelocity);
		QAngle angles = GetBasePlayer().GetLocalAngles();

		if (vecEstVelocity.Y == 0 && vecEstVelocity.X == 0) {
			float yawDiff = angles[YAW] - PoseParameterData.EstimateYaw;
			yawDiff = yawDiff - (int)(yawDiff / 360) * 360;
			if (yawDiff > 180)
				yawDiff -= 360;
			if (yawDiff < -180)
				yawDiff += 360;

			if (dt < 0.25)
				yawDiff *= (float)(dt * 4);
			else
				yawDiff *= (float)dt;

			PoseParameterData.EstimateYaw += yawDiff;
			PoseParameterData.EstimateYaw = PoseParameterData.EstimateYaw - (int)(PoseParameterData.EstimateYaw / 360) * 360;
		}
		else {
			PoseParameterData.EstimateYaw = (float)(Math.Atan2(vecEstVelocity.Y, vecEstVelocity.X) * 180 / Math.PI);

			if (PoseParameterData.EstimateYaw > 180)
				PoseParameterData.EstimateYaw = 180;
			else if (PoseParameterData.EstimateYaw < -180)
				PoseParameterData.EstimateYaw = -180;
		}
	}
	protected override void ComputePoseParam_MoveYaw(StudioHdr? studioHdr) {
		EstimateYaw();

		float yaw;

		QAngle angles = GetBasePlayer().GetLocalAngles();
		float ang = angles[YAW];
		if (ang > 180.0f)
			ang -= 360.0f;
		else if (ang < -180.0f)
			ang += 360.0f;

		yaw = ang - PoseParameterData.EstimateYaw;
		yaw = -yaw;
		yaw = yaw - (int)(yaw / 360) * 360;

		if (yaw < -180)
			yaw = yaw + 360;
		else if (yaw > 180)
			yaw = yaw - 360;

		GetBasePlayer().SetPoseParameter(studioHdr, PoseParameterData.MoveY, yaw);
	}

	protected override void ComputePoseParam_AimPitch(StudioHdr? studioHdr) {
		float aimPitch = EyePitch;

		GetBasePlayer().SetPoseParameter(studioHdr, PoseParameterData.AimPitch, aimPitch);
		DebugAnimData.AimPitch = aimPitch;
	}

	protected override void ComputePoseParam_AimYaw(StudioHdr? studioHdr) {
		GetOuterAbsVelocity(out Vector3 vecVelocity);

		bool moving = vecVelocity.Length() > 1.0f;

		if (moving || ForceAimYaw)
			GoalFeetYaw = EyeYaw;
		else {
			if (PoseParameterData.LastAimTurnTime <= 0.0f) {
				GoalFeetYaw = EyeYaw;
				CurrentFeetYaw = EyeYaw;
				PoseParameterData.LastAimTurnTime = (float)gpGlobals.CurTime;
			}
			else {
				float yawDelta = MathLib.AngleNormalize(GoalFeetYaw - EyeYaw);

				if (MathF.Abs(yawDelta) > 45.0f) {
					float side = (yawDelta > 0.0f) ? -1.0f : 1.0f;
					GoalFeetYaw += (45.0f * side);
				}
			}
		}

		GoalFeetYaw = MathLib.AngleNormalize(GoalFeetYaw);
		if (GoalFeetYaw != CurrentFeetYaw) {
			if (ForceAimYaw)
				CurrentFeetYaw = GoalFeetYaw;
			else {
				ConvergeYawAngles(GoalFeetYaw, 720.0f, gpGlobals.FrameTime, ref CurrentFeetYaw);
				LastAimTurnTime = gpGlobals.CurTime;
			}
		}

		AngRender[YAW] = CurrentFeetYaw;

		float aimYaw = EyeYaw - CurrentFeetYaw;
		aimYaw = MathLib.AngleNormalize(aimYaw);

		GetBasePlayer().SetPoseParameter(studioHdr, PoseParameterData.AimYaw, aimYaw);
		DebugAnimData.AimYaw = aimYaw;

		ForceAimYaw = false;

#if !CLIENT_DLL
		QAngle angle = GetBasePlayer().GetAbsAngles();
		angle[YAW] = CurrentFeetYaw;

		GetBasePlayer().SetAbsAngles(angle);
#endif
	}

	protected override float GetCurrentMaxGroundSpeed() {
		StudioHdr? studioHdr = GetBasePlayer().GetModelPtr();

		if (studioHdr == null)
			return 1.0f;

		float prevY = GetBasePlayer().GetPoseParameter(PoseParameterData.MoveY);

		float d = MathF.Sqrt(prevY * prevY);
		float newY;
		if (d == 0.0)
			newY = 0.0f;
		else
			newY = prevY / d;

		GetBasePlayer().SetPoseParameter(studioHdr, PoseParameterData.MoveY, newY);

		float speed = (float)GetBasePlayer().GetSequenceGroundSpeed(GetBasePlayer().GetSequence());

		GetBasePlayer().SetPoseParameter(studioHdr, PoseParameterData.MoveY, prevY);

		return speed;
	}

	public HL2MP_Player GetHL2MPPlayer() => HL2MPPlayer!;
}

#endif
