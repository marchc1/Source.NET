#if CLIENT_DLL || GAME_DLL
#if CLIENT_DLL
global using static Game.Client.BasePlayerGlobals;

global using BasePlayer = Game.Client.C_BasePlayer;

using Game.Shared;

#else
global using static Game.Server.BasePlayerGlobals;
global using BasePlayer = Game.Server.BasePlayer;

#endif
using Source.Common.Mathematics;

using System.Numerics;

#if CLIENT_DLL
namespace Game.Client;

#else
namespace Game.Server;
#endif

using Source.Common.Commands;

public static class BasePlayerGlobals {
	public static BasePlayer? ToBasePlayer(SharedBaseEntity? entity) {
		if (entity == null || !entity.IsPlayer())
			return null;

		return (BasePlayer?)entity;
	}

	public static BaseCombatCharacter? ToBaseCombatCharacter(SharedBaseEntity? entity) {
		if (entity == null || !entity.IsBaseCombatCharacter())
			return null;

		return (BaseCombatCharacter?)entity;
	}
}

public partial class
#if CLIENT_DLL
	C_BasePlayer
#elif GAME_DLL
	BasePlayer
#endif
{
	public virtual void CalcView(ref Vector3 eyeOrigin, ref  QAngle eyeAngles, ref float zNear, ref float zFar, ref float fov) {
		CalcPlayerView(ref eyeOrigin, ref eyeAngles, ref fov); // << TODO: There is a lot more logic here for observers, vehicles, etc!
	}

	public BaseCombatWeapon? GetActiveWeapon() {
		return null;
	}

	public override Vector3 EyePosition() {
		return base.EyePosition();
	}

	public ref readonly QAngle LocalEyeAngles() => ref pl.ViewingAngle;

	static QAngle angEyeWorld;
	public override ref readonly QAngle EyeAngles() {
		// NOTE: Viewangles are measured *relative* to the parent's coordinate system
		SharedBaseEntity? pMoveParent = null; //this.GetMoveParent();

		if (pMoveParent == null) 
			return ref pl.ViewingAngle;

		// FIXME: Cache off the angles?
		Matrix3x4 eyesToParent = default, eyesToWorld = default;
		MathLib.AngleMatrix(pl.ViewingAngle, ref eyesToParent);
		MathLib.ConcatTransforms(pMoveParent.EntityToWorldTransform(), eyesToParent, out eyesToWorld);

		MathLib.MatrixAngles(in eyesToWorld, out angEyeWorld);
		return ref angEyeWorld;
	}

	public virtual void CalcViewModelView(in Vector3 eyeOrigin, in QAngle eyeAngles) {
		for (int i = 0; i < MAX_VIEWMODELS; i++) {
			BaseViewModel? vm = GetViewModel(i);
			if (vm == null)
				continue;

			vm.CalcViewModelView(this, eyeOrigin, eyeAngles);
		}
	}
	private void CalcPlayerView(ref Vector3 eyeOrigin, ref QAngle eyeAngles, ref float fov) {
		eyeOrigin = EyePosition();
		eyeAngles = EyeAngles();
	}

	internal ReadOnlySpan<char> GetPlayerName() {
		throw new NotImplementedException();
	}

	static ConVar sv_suppress_viewpunch = new( "sv_suppress_viewpunch", "0", FCvar.Replicated | FCvar.Cheat | FCvar.DevelopmentOnly);

	public void ViewPunch(in QAngle angleOffset) {
		//See if we're suppressing the view punching
		if (sv_suppress_viewpunch.GetBool())
			return;

		// We don't allow view kicks in the vehicle
		if (IsInAVehicle())
			return;

		Local.PunchAngleVel += angleOffset * 20;
	}
	public void ViewPunchReset(float tolerance) {
		if (tolerance != 0) {
			tolerance *= tolerance; // square
			float check = Local.PunchAngleVel.LengthSqr() + Local.PunchAngle.LengthSqr();
			if (check > tolerance)
				return;
		}
		Local.PunchAngle = vec3_angle;
		Local.PunchAngleVel = vec3_angle;
	}
}
#endif
