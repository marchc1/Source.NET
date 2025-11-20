#if CLIENT_DLL || GAME_DLL
#if CLIENT_DLL
global using BasePlayer = Game.Client.C_BasePlayer;

using Game.Shared;

#else
global using BasePlayer = Game.Server.BasePlayer;

#endif
using Source.Common.Mathematics;

using System.Numerics;

#if CLIENT_DLL
namespace Game.Client;

#else
namespace Game.Server;
#endif

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
}
#endif
