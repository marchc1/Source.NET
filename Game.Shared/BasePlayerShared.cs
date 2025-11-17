#if CLIENT_DLL || GAME_DLL
#if CLIENT_DLL
global using BasePlayer = Game.Client.C_BasePlayer;
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
		throw new NotImplementedException();
	}

	public override Vector3 EyePosition() {
		return base.EyePosition();
	}

	public override ref readonly QAngle EyeAngles() {
		return ref base.EyeAngles();
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
