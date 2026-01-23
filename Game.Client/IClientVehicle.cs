using Game.Shared;

namespace Game.Client;

public interface IClientVehicle : IVehicle
{
	void GetVehicleFOV(out float fov );
	void UpdateViewAngles(C_BasePlayer? localPlayer, ref UserCmd cmd);
	void DrawHudElements();
	bool IsPredicted();
	C_BaseEntity? GetVehicleEnt();
	void GetVehicleClipPlanes(out float zNear, out float zFar );
	int GetJoystickResponseCurve();

#if HL2_DLL
	int GetPrimaryAmmoType() ;
	int GetPrimaryAmmoClip();
	bool PrimaryAmmoUsesClips();
	int GetPrimaryAmmoCount();
#endif
}
