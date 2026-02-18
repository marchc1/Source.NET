using Game.Shared;

using Source.Common;

using System.Numerics;

namespace Game.Client;
using FIELD = Source.FIELD<C_PropVehicleDriveable>;

public class C_PropVehicleDriveable : C_BaseAnimating
{
	public static readonly RecvTable DT_PropVehicleDriveable = new(DT_BaseAnimating, [
		RecvPropEHandle(FIELD.OF(nameof(Player))),
		RecvPropInt(FIELD.OF(nameof(Speed))),
		RecvPropInt(FIELD.OF(nameof(RPM))),
		RecvPropFloat(FIELD.OF(nameof(Throttle))),
		RecvPropInt(FIELD.OF(nameof(BoostTimeLeft))),
		RecvPropBool(FIELD.OF(nameof(HasBoost))),
		RecvPropBool(FIELD.OF(nameof(ScannerDisabledWeapons))),
		RecvPropBool(FIELD.OF(nameof(ScannerDisabledVehicle))),
		RecvPropBool(FIELD.OF(nameof(EnterAnimOn))),
		RecvPropBool(FIELD.OF(nameof(ExitAnimOn))),
		RecvPropBool(FIELD.OF(nameof(UnableToFire))),
		RecvPropVector(FIELD.OF(nameof(EyeExitEndpoint))),
		RecvPropBool(FIELD.OF(nameof(HasGun))),
		RecvPropVector(FIELD.OF(nameof(GunCrosshair)))
	]);
	public static readonly new ClientClass ClientClass = new ClientClass("PropVehicleDriveable", DT_PropVehicleDriveable).WithManualClassID(StaticClassIndices.CPropVehicleDriveable);

	public EHANDLE Player = new();
	public int RPM;
	public float Throttle;
	public int BoostTimeLeft;
	public bool HasBoost;
	public bool ScannerDisabledWeapons;
	public bool ScannerDisabledVehicle;
	public bool EnterAnimOn;
	public bool ExitAnimOn;
	public bool UnableToFire;
	public Vector3 EyeExitEndpoint;
	public bool HasGun;
	public Vector3 GunCrosshair;
}
