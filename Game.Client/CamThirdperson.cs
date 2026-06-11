global using static Game.Client.ThirdPersonManager;

using Source;
using Source.Common.Commands;
using Source.Common.Formats.BSP;
using Source.Common.Mathematics;

using System.Numerics;
namespace Game.Client;

[EngineComponent]
public class ThirdPersonManager
{
	const int DIST_FORWARD = 0;
	const int DIST_RIGHT = 1;
	const int DIST_UP = 2;
	public const float CAM_MIN_DIST = 30.0f;
	public const float CAM_ANGLE_MOVE = 0.5f;
	const double MAX_ANGLE_DIFF = 10.0;
	const double PITCH_MAX = 90.0;
	const int PITCH_MIN = 0;
	const double YAW_MAX = 135.0;
	const double YAW_MIN = -135.0;
	public const int DIST = 2;
	const float CAM_HULL_OFFSET = 14.0f;
	const float CAMERA_UP_OFFSET = 25.0f;
	const float CAMERA_OFFSET_LERP_TIME = 0.5f;
	const float CAMERA_UP_OFFSET_LERP_TIME = 0.25f;

	public static ThirdPersonManager g_ThirdPersonManager = new();

	static Vector3 CAM_HULL_MIN = new(-CAM_HULL_OFFSET, -CAM_HULL_OFFSET, -CAM_HULL_OFFSET);
	static Vector3 CAM_HULL_MAX = new(CAM_HULL_OFFSET, CAM_HULL_OFFSET, CAM_HULL_OFFSET);

	Vector3 CameraOffset;
	Vector3 DesiredCameraOffset;
	Vector3 CameraOrigin;
	bool _UseCameraOffsets;
	QAngle ViewAngles;

	float Fraction;
	float UpFraction;

	float TargetFraction;
	float TargetUpFraction;

	bool OverrideThirdPerson;
	bool Forced;

	float UpOffset;

	TimeUnit_t LerpTime;
	TimeUnit_t UpLerpTime;

	public static readonly ConVar cl_thirdperson = new("cl_thirdperson", "0", FCvar.NotConnected | FCvar.UserInfo | FCvar.Archive | FCvar.DevelopmentOnly, "Enables/Disables third person", callback: ThirdPersonChange);

	private static void ThirdPersonChange(IConVar var, in ConVarChangeContext ctx) {
		ConVarRef v = new(var);
		ToggleThirdPerson(v.GetBool());
	}

	private static void ToggleThirdPerson(bool value) {
		if (value)
			input.CAM_ToThirdPerson();
		else
			input.CAM_ToFirstPerson();
	}

	public void SetCameraOffsetAngles(Vector3 offset) => CameraOffset = offset;

	public Vector3 GetCameraOffsetAngles() => CameraOffset;

	public void SetDesiredCameraOffset(Vector3 offset) => DesiredCameraOffset = offset;

	Vector3 GetDesiredCameraOffset() => DesiredCameraOffset;

	public Vector3 GetFinalCameraOffset() {
		Vector3 desired = GetDesiredCameraOffset();

		if (UpFraction != 1.0f)
			desired.Z += UpOffset;

		return desired;
	}

	void SetCameraOrigin(Vector3 offset) => CameraOrigin = offset;

	Vector3 GetCameraOrigin() => CameraOrigin;

	[CvarIgnore] ConVar? sv_cheats;
	public void Update() {
		sv_cheats ??= cvar.FindVar("sv_cheats");

		if (sv_cheats != null && !sv_cheats.GetBool() /*&& GameRules().AllowThirdPersonCamera() == true*/) {
			if (input.CAM_IsThirdPerson() == true)
				input.CAM_ToFirstPerson();
			return;
		}

		if (IsOverridingThirdPerson() == false) {
			if (input.CAM_IsThirdPerson() != (cl_thirdperson.GetBool() || Forced) /*&& GameRules().AllowThirdPersonCamera() == true*/)
				ToggleThirdPerson(Forced || cl_thirdperson.GetBool());
		}
	}

	public void PositionCamera(BasePlayer? player, QAngle angles) {
		if (player != null) {
			Vector3 origin = player.GetLocalOrigin();
			origin += player.GetViewOffset();

			MathLib.AngleVectors(angles, out Vector3 camForward, out Vector3 camRight, out Vector3 camUp);

			Vector3 endPos = origin;

			Vector3 vecCamOffset = endPos + (camForward * -GetDesiredCameraOffset()[DIST_FORWARD]) + (camRight * GetDesiredCameraOffset()[DIST_RIGHT]) + (camUp * GetDesiredCameraOffset()[DIST_UP]);

			TraceFilterSimple traceFilter = new(player, CollisionGroup.None);
			Util.TraceHull(endPos, vecCamOffset, CAM_HULL_MIN, CAM_HULL_MAX, Mask.Solid & ~(Mask)Contents.Monster, ref traceFilter, out Trace trace);

			if (trace.Fraction != TargetFraction)
				LerpTime = gpGlobals.CurTime;

			TargetFraction = trace.Fraction;
			TargetUpFraction = 1.0f;

			if (TargetFraction < Fraction) {
				Fraction = TargetFraction;
				LerpTime = gpGlobals.CurTime;
			}

			if (trace.Fraction < 1.0) {
				CameraOffset[DIST] *= trace.Fraction;

				Util.TraceHull(endPos, endPos + (camForward * -GetDesiredCameraOffset()[DIST_FORWARD]), CAM_HULL_MIN, CAM_HULL_MAX, Mask.Solid & ~(Mask)Contents.Monster, ref traceFilter, out trace);

				if (trace.Fraction != 1.0f) {
					if (trace.Fraction != TargetUpFraction) {
						UpLerpTime = gpGlobals.CurTime;
					}

					TargetUpFraction = trace.Fraction;

					if (TargetUpFraction < UpFraction) {
						UpFraction = trace.Fraction;
						UpLerpTime = gpGlobals.CurTime;
					}
				}
			}
		}
	}

	void UseCameraOffsets(bool use) => _UseCameraOffsets = use;

	bool UsingCameraOffsets() => _UseCameraOffsets;

	QAngle GetCameraViewAngles() => ViewAngles;

	public Vector3 GetDistanceFraction() {
		if (IsOverridingThirdPerson() == true)
			return new(TargetFraction, TargetFraction, TargetFraction);

		double frac = MathLib.RemapValClamped(gpGlobals.CurTime - LerpTime, 0, CAMERA_OFFSET_LERP_TIME, 0, 1);

		float fraction = MathLib.Lerp((float)frac, Fraction, TargetFraction);

		if (frac == 1.0f)
			Fraction = TargetFraction;

		frac = MathLib.RemapValClamped(gpGlobals.CurTime - UpLerpTime, 0, CAMERA_UP_OFFSET_LERP_TIME, 0, 1);

		float upFraction = 1.0f - MathLib.Lerp((float)frac, UpFraction, TargetUpFraction);
		if (frac == 1.0f)
			UpFraction = TargetUpFraction;

		return new(fraction, fraction, upFraction);
	}

	bool WantToUseGameThirdPerson() => cl_thirdperson.GetBool() /*&& g_pGameRules.AllowThirdPersonCamera()*/ && IsOverridingThirdPerson() == false;

	public void SetOverridingThirdPerson(bool bOverride) => OverrideThirdPerson = bOverride;

	public bool IsOverridingThirdPerson() => OverrideThirdPerson;

	void Init() {
		OverrideThirdPerson = false;
		Forced = false;
		UpFraction = 0.0f;
		Fraction = 1.0f;

		UpLerpTime = 0.0f;
		LerpTime = 0.0f;

		UpOffset = CAMERA_UP_OFFSET;

		input.CAM_SetCameraThirdData(null, vec3_angle);
	}

	void SetForcedThirdPerson(bool bForced) => Forced = bForced;

	bool GetForcedThirdPerson() => Forced;
}
