using Game.Shared;

using Source.Common;

using System.Numerics;

namespace Game.Server;
using FIELD = Source.FIELD<PropVehicleDriveable>;

public class PropVehicleDriveable : BaseAnimating
{
	public static readonly SendTable DT_PropVehicleDriveable = new(DT_BaseAnimating, [
		SendPropEHandle(FIELD.OF(nameof(Player))),
		SendPropInt(FIELD.OF(nameof(Speed)), 8),
		SendPropInt(FIELD.OF(nameof(RPM)), 13),
		SendPropFloat(FIELD.OF(nameof(Throttle)), 0, PropFlags.NoScale),
		SendPropInt(FIELD.OF(nameof(BoostTimeLeft)), 8),
		SendPropBool(FIELD.OF(nameof(HasBoost))),
		SendPropBool(FIELD.OF(nameof(ScannerDisabledWeapons))),
		SendPropBool(FIELD.OF(nameof(ScannerDisabledVehicle))),
		SendPropBool(FIELD.OF(nameof(EnterAnimOn))),
		SendPropBool(FIELD.OF(nameof(ExitAnimOn))),
		SendPropBool(FIELD.OF(nameof(UnableToFire))),
		SendPropVector(FIELD.OF(nameof(EyeExitEndpoint)), 0, PropFlags.Coord),
		SendPropBool(FIELD.OF(nameof(HasGun))),
		SendPropVector(FIELD.OF(nameof(GunCrosshair)), 0, PropFlags.Coord)
	]);
	public static readonly new ServerClass ServerClass = new ServerClass("PropVehicleDriveable", DT_PropVehicleDriveable).WithManualClassID(StaticClassIndices.CPropVehicleDriveable);

	public readonly EHANDLE Player = new();
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
