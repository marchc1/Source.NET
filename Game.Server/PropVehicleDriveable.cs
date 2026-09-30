using Game.Shared;

using Source.Common;

using System.Numerics;

namespace Game.Server;
using FIELD = Source.FIELD<PropVehicleDriveable>;

[NetworkName("CPropVehicleDriveable")]
public partial class PropVehicleDriveable : BaseAnimating
{
	public static readonly SendTable DT_PropVehicleDriveable = new(DT_BaseAnimating, [
		SendPropEHandle(PropVehicleDriveable.NetworkVarFields.Player),
		SendPropInt(NetworkVarFields.Speed, 8),
		SendPropInt(NetworkVarFields.RPM, 13),
		SendPropFloat(NetworkVarFields.Throttle, 0, PropFlags.NoScale),
		SendPropInt(NetworkVarFields.BoostTimeLeft, 8),
		SendPropBool(NetworkVarFields.HasBoost),
		SendPropBool(NetworkVarFields.EnterAnimOn),
		SendPropBool(NetworkVarFields.ExitAnimOn),
		SendPropBool(NetworkVarFields.UnableToFire),
		SendPropVector(NetworkVarFields.EyeExitEndpoint, 0, PropFlags.Coord),
		SendPropBool(NetworkVarFields.HasGun),
		SendPropVector(NetworkVarFields.GunCrosshair, 0, PropFlags.Coord),
		SendPropBool(NetworkVarFields.Locked),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_PropVehicleDriveable);

	[NetworkName("m_hPlayer")]
	[NetworkVar] public partial NetworkHandle<BaseEntity> Player { get; }
	[NetworkName("m_nSpeed")]
	[NetworkVar] public new partial int Speed { get; set; }
	[NetworkName("m_nRPM")]
	[NetworkVar] public partial int RPM { get; set; }
	[NetworkName("m_flThrottle")]
	[NetworkVar] public partial float Throttle { get; set; }
	[NetworkName("m_nBoostTimeLeft")]
	[NetworkVar] public partial int BoostTimeLeft { get; set; }
	[NetworkName("m_nHasBoost")]
	[NetworkVar] public partial bool HasBoost { get; set; }
	public bool ScannerDisabledWeapons;
	public bool ScannerDisabledVehicle;
	[NetworkName("m_bEnterAnimOn")]
	[NetworkVar] public partial bool EnterAnimOn { get; set; }
	[NetworkName("m_bExitAnimOn")]
	[NetworkVar] public partial bool ExitAnimOn { get; set; }
	[NetworkName("m_bUnableToFire")]
	[NetworkVar] public partial bool UnableToFire { get; set; }
	[NetworkName("m_vecEyeExitEndpoint")]
	[NetworkVar] public partial Vector3 EyeExitEndpoint { get; set; }
	[NetworkName("m_bHasGun")]
	[NetworkVar] public partial bool HasGun { get; set; }
	[NetworkName("m_vecGunCrosshair")]
	[NetworkVar] public partial Vector3 GunCrosshair { get; set; }
	[NetworkName("m_bLocked")]
	[NetworkVar] public partial bool Locked { get; set; }
}
