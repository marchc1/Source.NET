using Game.Shared;

using Source.Common;

using System.Numerics;

namespace Game.Client;
using FIELD = Source.FIELD<C_PropVehicleDriveable>;

[NetworkName("CPropVehicleDriveable")]
public class C_PropVehicleDriveable : C_BaseAnimating
{
	public static readonly RecvTable DT_PropVehicleDriveable = new(DT_BaseAnimating, [
		RecvPropEHandle(FIELD.OF(nameof(Player))),
		RecvPropInt(FIELD.OF(nameof(Speed))),
		RecvPropInt(FIELD.OF(nameof(RPM))),
		RecvPropFloat(FIELD.OF(nameof(Throttle))),
		RecvPropInt(FIELD.OF(nameof(BoostTimeLeft))),
		RecvPropBool(FIELD.OF(nameof(HasBoost))),
		RecvPropBool(FIELD.OF(nameof(EnterAnimOn))),
		RecvPropBool(FIELD.OF(nameof(ExitAnimOn))),
		RecvPropBool(FIELD.OF(nameof(UnableToFire))),
		RecvPropVector(FIELD.OF(nameof(EyeExitEndpoint))),
		RecvPropBool(FIELD.OF(nameof(HasGun))),
		RecvPropVector(FIELD.OF(nameof(GunCrosshair))),
		RecvPropBool(FIELD.OF(nameof(Locked))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass(DT_PropVehicleDriveable);

	[NetworkName("m_hPlayer")]
	public EHANDLE Player = new();
	[NetworkName("m_nRPM")]
	public int RPM;
	[NetworkName("m_flThrottle")]
	public float Throttle;
	[NetworkName("m_nBoostTimeLeft")]
	public int BoostTimeLeft;
	[NetworkName("m_nHasBoost")]
	public bool HasBoost;
	public bool ScannerDisabledWeapons;
	public bool ScannerDisabledVehicle;
	[NetworkName("m_bEnterAnimOn")]
	public bool EnterAnimOn;
	[NetworkName("m_bExitAnimOn")]
	public bool ExitAnimOn;
	[NetworkName("m_bUnableToFire")]
	public bool UnableToFire;
	[NetworkName("m_vecEyeExitEndpoint")]
	public Vector3 EyeExitEndpoint;
	[NetworkName("m_bHasGun")]
	public bool HasGun;
	[NetworkName("m_vecGunCrosshair")]
	public Vector3 GunCrosshair;
	[NetworkName("m_bLocked")]
	public bool Locked;
}
