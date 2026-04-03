#if CLIENT_DLL || GAME_DLL

#if CLIENT_DLL
global using static Game.Client.HL2MP.HL2MPPlayerSharedGlobals;

global using HL2MP_Player = Game.Client.HL2MP.C_HL2MP_Player;

#else
global using static Game.Server.HL2MP.HL2MPPlayerSharedGlobals;

global using HL2MP_Player = Game.Server.HL2MP.HL2MP_Player;

#endif
using Source.Common.Mathematics;

using Game.Shared;

using System.Numerics;

#if CLIENT_DLL
namespace Game.Client.HL2MP;

#else
namespace Game.Server.HL2MP;
#endif

using Source.Common.Commands;
using Source.Common.Physics;
using Source;
using Source.Common;

using System.Runtime.CompilerServices;

public static class HL2MPPlayerSharedGlobals
{
	public static HL2MP_Player? ToHL2MPPlayer(BaseEntity? entity) {
		if (entity == null || !entity.IsPlayer())
			return null;

		return (HL2MP_Player?)entity;
	}
}

public partial class
#if CLIENT_DLL
	C_HL2MP_Player
#elif GAME_DLL
	HL2MP_Player
#endif
{
	public new Vector3 GetAttackSpread(BaseCombatWeapon? weapon, BaseEntity? target = null) {
		if (weapon != null)
			return weapon.GetBulletSpread(WeaponProficiency.Perfect);
		return VECTOR_CONE_15DEGREES;
	}

	public void UpdateLookAt() {
		if (HeadYawPoseParam < 0 || HeadPitchPoseParam < 0)
			return;

		// orient eyes
		ViewTarget = LookAtTarget;

		// Figure out where we want to look in world space.
		Vector3 to = LookAtTarget - EyePosition();
		MathLib.VectorAngles(to, out QAngle desiredAngles);

		// Figure out where our body is facing in world space.
		QAngle bodyAngles = new(0, 0, 0);
		bodyAngles[YAW] = GetLocalAngles()[YAW];

		float flBodyYawDiff = bodyAngles[YAW] - LastBodyYaw;
		LastBodyYaw = bodyAngles[YAW];

		// Set the head's yaw.
		float desired = MathLib.AngleNormalize(desiredAngles[YAW] - bodyAngles[YAW]);
		desired = Math.Clamp(desired, HeadYawMin, HeadYawMax);
		CurrentHeadYaw = MathLib.ApproachAngle(desired, CurrentHeadYaw, 130 * (float)gpGlobals.FrameTime);

		// Counterrotate the head from the body rotation so it doesn't rotate past its target.
		CurrentHeadYaw = MathLib.AngleNormalize(CurrentHeadYaw - flBodyYawDiff);
		desired = Math.Clamp(desired, HeadYawMin, HeadYawMax);

		SetPoseParameter(HeadYawPoseParam, CurrentHeadYaw);


		// Set the head's yaw.
		desired = MathLib.AngleNormalize(desiredAngles[PITCH]);
		desired = Math.Clamp(desired, HeadPitchMin, HeadPitchMax);

		CurrentHeadPitch = MathLib.ApproachAngle(desired, CurrentHeadPitch, 130 * (float)gpGlobals.FrameTime);
		CurrentHeadPitch = MathLib.AngleNormalize(CurrentHeadPitch);
		SetPoseParameter(HeadPitchPoseParam, CurrentHeadPitch);
	}
	Vector3 ViewTarget;
	Vector3 LookAtTarget;

	int HeadYawPoseParam;
	int HeadPitchPoseParam;
	float LastBodyYaw;
	float CurrentHeadYaw;
	float CurrentHeadPitch;
	float HeadYawMin;
	float HeadYawMax;
	float HeadPitchMin;
	float HeadPitchMax;

	internal QAngle GetAnimEyeAngles() => AngEyeAngles;
}

public class PlayerAnimState
{
	public PlayerAnimState(HL2MP_Player outer) {
		m_pOuter = outer;

		m_flGaitYaw = 0.0f;
		m_flGoalFeetYaw = 0.0f;
		m_flCurrentFeetYaw = 0.0f;
		m_flCurrentTorsoYaw = 0.0f;
		m_flLastYaw = 0.0f;
		m_flLastTurnTime = 0.0f;
		m_flTurnCorrectionTime = 0.0f;
	}

	public Activity BodyYawTranslateActivity(Activity activity) {
		if (activity != Activity.ACT_IDLE)
			return activity;

		// Not turning
		switch (m_nTurningInPlace) {
			default:
			case TurnMode.None:
				return activity;
			case TurnMode.Right:
			case TurnMode.Left:
				return mp_ik.GetBool() ? Activity.ACT_TURN : activity;
		}
	}

	public void Update() {
		m_angRender = GetOuter().GetLocalAngles();
		m_angRender[PITCH] = m_angRender[ROLL] = 0.0f;

		ComputePoseParam_BodyYaw();
		ComputePoseParam_BodyPitch(GetOuter().GetModelPtr());
		ComputePoseParam_BodyLookYaw();

		ComputePlaybackRate();

#if CLIENT_DLL
		GetOuter().UpdateLookAt();
#endif
	}

	public ref readonly QAngle GetRenderAngles() => ref m_angRender;


	public HL2MP_Player GetOuter() => m_pOuter!;


	void GetOuterAbsVelocity(out Vector3 vel) {
#if CLIENT_DLL
		GetOuter().EstimateAbsVelocity(out vel);
#else
		vel = GetOuter().GetAbsVelocity();
#endif
	}

	TurnMode ConvergeAngles(float goal, float maxrate, float dt, ref float current) {
		TurnMode direction = TurnMode.None;

		float anglediff = goal - current;
		float anglediffabs = MathF.Abs(anglediff);

		anglediff = MathLib.AngleNormalize(anglediff);

		float scale = 1.0f;
		if (anglediffabs <= FADE_TURN_DEGREES) {
			scale = anglediffabs / FADE_TURN_DEGREES;
			// Always do at least a bit of the turn ( 1% )
			scale = Math.Clamp(scale, 0.01f, 1.0f);
		}

		float maxmove = maxrate * dt * scale;

		if (MathF.Abs(anglediff) < maxmove) {
			current = goal;
		}
		else {
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

	void EstimateYaw() {
		float dt = (float)gpGlobals.FrameTime;

		if (0 == dt)
			return;

		Vector3 est_velocity;
		QAngle angles;

		GetOuterAbsVelocity(out est_velocity);

		angles = GetOuter().GetLocalAngles();

		if (est_velocity[1] == 0 && est_velocity[0] == 0) {
			float flYawDiff = angles[YAW] - m_flGaitYaw;
			flYawDiff = flYawDiff - (int)(flYawDiff / 360) * 360;
			if (flYawDiff > 180)
				flYawDiff -= 360;
			if (flYawDiff < -180)
				flYawDiff += 360;

			if (dt < 0.25)
				flYawDiff *= dt * 4;
			else
				flYawDiff *= dt;

			m_flGaitYaw += flYawDiff;
			m_flGaitYaw = m_flGaitYaw - (int)(m_flGaitYaw / 360) * 360;
		}
		else {
			m_flGaitYaw = (MathF.Atan2(est_velocity[1], est_velocity[0]) * 180 / MathF.PI);

			if (m_flGaitYaw > 180)
				m_flGaitYaw = 180;
			else if (m_flGaitYaw < -180)
				m_flGaitYaw = -180;
		}
	}
	void ComputePoseParam_BodyYaw() {
		int iYaw = GetOuter().LookupPoseParameter("move_yaw");
		if (iYaw < 0)
			return;

		// view direction relative to movement
		float flYaw;

		EstimateYaw();

		QAngle angles = GetOuter().GetLocalAngles();
		float ang = angles[YAW];
		if (ang > 180.0f) 
			ang -= 360.0f;
		else if (ang < -180.0f) 
			ang += 360.0f;

		// calc side to side turning
		flYaw = ang - m_flGaitYaw;
		// Invert for mapping into 8way blend
		flYaw = -flYaw;
		flYaw = flYaw - (int)(flYaw / 360) * 360;

		if (flYaw < -180) 
			flYaw = flYaw + 360;
		else if (flYaw > 180) 
			flYaw = flYaw - 360;

		GetOuter().SetPoseParameter(iYaw, flYaw);

#if !CLIENT_DLL
		// todo: GetOuter().SetLocalAngles(QAngle(GetOuter().GetAnimEyeAngles().X, m_flCurrentFeetYaw, 0));
#endif
	}
	void ComputePoseParam_BodyPitch(StudioHdr? studioHdr) {
		float flPitch = GetOuter().GetLocalAngles()[PITCH];

		if (flPitch > 180.0f) 
			flPitch -= 360.0f;
		
		flPitch = Math.Clamp(flPitch, -90, 90);

		QAngle absangles = GetOuter().GetAbsAngles();
		absangles.X = 0.0f;
		m_angRender = absangles;
		m_angRender[PITCH] = m_angRender[ROLL] = 0.0f;

		// See if we have a blender for pitch
		GetOuter().SetPoseParameter(studioHdr, "aim_pitch", flPitch);
	}
	void ComputePoseParam_BodyLookYaw() {
		QAngle absangles = GetOuter().GetAbsAngles();
		absangles.Y = MathLib.AngleNormalize(absangles.Y);
		m_angRender = absangles;
		m_angRender[PITCH] = m_angRender[ROLL] = 0.0f;

		// See if we even have a blender for pitch
		int upper_body_yaw = GetOuter().LookupPoseParameter("aim_yaw");
		if (upper_body_yaw < 0) 
			return;
		
		// Assume upper and lower bodies are aligned and that we're not turning
		float flGoalTorsoYaw = 0.0f;
		TurnMode turning = TurnMode.None;
		float turnrate = 360.0f;

		GetOuterAbsVelocity(out Vector3 vel);

		bool isMoving = (vel.Length() > 1.0f) ? true : false;

		if (!isMoving) {
			// Just stopped moving, try and clamp feet
			if (m_flLastTurnTime <= 0.0f) {
				m_flLastTurnTime = gpGlobals.CurTime;
				m_flLastYaw = GetOuter().GetAnimEyeAngles().Y;
				// Snap feet to be perfectly aligned with torso/eyes
				m_flGoalFeetYaw = GetOuter().GetAnimEyeAngles().Y;
				m_flCurrentFeetYaw = m_flGoalFeetYaw;
				m_nTurningInPlace = TurnMode.None;
			}

			// If rotating in place, update stasis timer
			if (m_flLastYaw != GetOuter().GetAnimEyeAngles().Y) {
				m_flLastTurnTime = gpGlobals.CurTime;
				m_flLastYaw = GetOuter().GetAnimEyeAngles().Y;
			}

			if (m_flGoalFeetYaw != m_flCurrentFeetYaw) 
				m_flLastTurnTime = gpGlobals.CurTime;
			

			turning = ConvergeAngles(m_flGoalFeetYaw, turnrate, (float)gpGlobals.FrameTime, ref m_flCurrentFeetYaw);

			QAngle eyeAngles = GetOuter().GetAnimEyeAngles();
			QAngle vAngle = GetOuter().GetLocalAngles();

			// See how far off current feetyaw is from true yaw
			float yawdelta = GetOuter().GetAnimEyeAngles().Y - m_flCurrentFeetYaw;
			yawdelta = MathLib.AngleNormalize(yawdelta);

			bool rotated_too_far = false;

			float yawmagnitude = MathF.Abs(yawdelta);

			// If too far, then need to turn in place
			if (yawmagnitude > 45) 
				rotated_too_far = true;
		
			// Standing still for a while, rotate feet around to face forward
			// Or rotated too far
			// FIXME:  Play an in place turning animation
			if (rotated_too_far ||
				(gpGlobals.CurTime> m_flLastTurnTime + mp_facefronttime.GetFloat())) {
				m_flGoalFeetYaw = GetOuter().GetAnimEyeAngles().Y;
				m_flLastTurnTime = gpGlobals.CurTime;

			}

			// Snap upper body into position since the delta is already smoothed for the feet
			flGoalTorsoYaw = yawdelta;
			m_flCurrentTorsoYaw = flGoalTorsoYaw;
		}
		else {
			m_flLastTurnTime = 0.0f;
			m_nTurningInPlace = TurnMode.None;
			m_flCurrentFeetYaw = m_flGoalFeetYaw = GetOuter().GetAnimEyeAngles().Y;
			flGoalTorsoYaw = 0.0f;
			m_flCurrentTorsoYaw = GetOuter().GetAnimEyeAngles().Y - m_flCurrentFeetYaw;
		}


		if (turning == TurnMode.None) 
			m_nTurningInPlace = turning;
		

		if (m_nTurningInPlace != TurnMode.None) 
			// If we're close to finishing the turn, then turn off the turning animation
			if (MathF.Abs(m_flCurrentFeetYaw - m_flGoalFeetYaw) < MIN_TURN_ANGLE_REQUIRING_TURN_ANIMATION) 
				m_nTurningInPlace = TurnMode.None;

		// Rotate entire body into position
		absangles = GetOuter().GetAbsAngles();
		absangles.Y = m_flCurrentFeetYaw;
		m_angRender = absangles;
		m_angRender[PITCH] = m_angRender[ROLL] = 0.0f;

		GetOuter().SetPoseParameter(upper_body_yaw, Math.Clamp(m_flCurrentTorsoYaw, -60.0f, 60.0f));
	}

	void ComputePlaybackRate() {
		GetOuterAbsVelocity(out Vector3 vel);
		float speed = vel.Length2D();

		bool isMoving = (speed > 0.5f) ? true : false;

		double maxspeed = GetOuter().GetSequenceGroundSpeed(GetOuter().GetSequence());

		if (isMoving && (maxspeed > 0.0f)) {
			float flFactor = 1.0f;
			GetOuter().SetPlaybackRate((speed * flFactor) / maxspeed);
		}
		else 
			GetOuter().SetPlaybackRate(1.0f);
	}

	HL2MP_Player? m_pOuter;

	float m_flGaitYaw;
	float m_flStoredCycle;

	// The following variables are used for tweaking the yaw of the upper body when standing still and
	//  making sure that it smoothly blends in and out once the player starts moving
	// Direction feet were facing when we stopped moving
	float m_flGoalFeetYaw;
	float m_flCurrentFeetYaw;

	float m_flCurrentTorsoYaw;

	// To check if they are rotating in place
	float m_flLastYaw;
	// Time when we stopped moving
	TimeUnit_t m_flLastTurnTime;

	// One of the above enums
	TurnMode m_nTurningInPlace;

	QAngle m_angRender;

	float m_flTurnCorrectionTime;
}

#endif
