using Source.Common;
using Source;
using System.Numerics;

namespace Game.Server;

using FIELD = FIELD<EnvHeadcrabCanister>;

public partial class EnvHeadcrabCanisterShared
{
	[NetworkName("m_flFlightSpeed")]
	[NetworkVar] public partial float FlightSpeed { get; set; }
	[NetworkName("m_flLaunchTime")]
	[NetworkVar] public partial TimeUnit_t LaunchTime { get; set; }
	[NetworkName("m_vecParabolaDirection")]
	[NetworkVar] public partial Vector3 ParabolaDirection { get; set; }
	[NetworkName("m_flFlightTime")]
	[NetworkVar] public partial float FlightTime { get; set; }
	[NetworkName("m_flWorldEnterTime")]
	[NetworkVar] public partial float WorldEnterTime { get; set; }
	[NetworkName("m_flInitialZSpeed")]
	[NetworkVar] public partial float InitialZSpeed { get; set; }
	[NetworkName("m_flZAcceleration")]
	[NetworkVar] public partial float ZAcceleration { get; set; }
	[NetworkName("m_flHorizSpeed")]
	[NetworkVar] public partial float HorizSpeed { get; set; }
	[NetworkName("m_bLaunchedFromWithinWorld")]
	[NetworkVar] public partial bool LaunchedFromWithinWorld { get; set; }
	[NetworkName("m_vecStartPosition")]
	[NetworkVar] public partial Vector3 StartPosition { get; set; }
	[NetworkName("m_vecEnterWorldPosition")]
	[NetworkVar] public partial Vector3 EnterWorldPosition { get; set; }
	[NetworkName("m_vecDirection")]
	[NetworkVar] public partial Vector3 Direction { get; set; }
	[NetworkName("m_vecStartAngles")]
	[NetworkVar] public partial Vector3 StartAngles { get; set; }
	[NetworkName("m_vecSkyboxOrigin")]
	[NetworkVar] public partial Vector3 SkyboxOrigin { get; set; }
	[NetworkName("m_flSkyboxScale")]
	[NetworkVar] public partial float SkyboxScale { get; set; }
	[NetworkName("m_bInSkybox")]
	[NetworkVar] public partial bool InSkybox { get; set; }

	public static readonly SendTable DT_EnvHeadcrabCanisterShared = new("DT_EnvHeadcrabCanisterShared", [
		SendPropFloat(EnvHeadcrabCanisterShared.NetworkVarFields.FlightSpeed, 0, PropFlags.NoScale),
		SendPropTime64(EnvHeadcrabCanisterShared.NetworkVarFields.LaunchTime),
		SendPropVector(EnvHeadcrabCanisterShared.NetworkVarFields.ParabolaDirection, 0, PropFlags.NoScale),
		SendPropFloat(EnvHeadcrabCanisterShared.NetworkVarFields.FlightTime, 0, PropFlags.NoScale),
		SendPropFloat(EnvHeadcrabCanisterShared.NetworkVarFields.WorldEnterTime, 0, PropFlags.NoScale),
		SendPropFloat(EnvHeadcrabCanisterShared.NetworkVarFields.InitialZSpeed, 0, PropFlags.NoScale),
		SendPropFloat(EnvHeadcrabCanisterShared.NetworkVarFields.ZAcceleration, 0, PropFlags.NoScale),
		SendPropFloat(EnvHeadcrabCanisterShared.NetworkVarFields.HorizSpeed, 0, PropFlags.NoScale),
		SendPropBool(EnvHeadcrabCanisterShared.NetworkVarFields.LaunchedFromWithinWorld),
		SendPropVector(EnvHeadcrabCanisterShared.NetworkVarFields.StartPosition, 0, PropFlags.NoScale),
		SendPropVector(EnvHeadcrabCanisterShared.NetworkVarFields.EnterWorldPosition, 0, PropFlags.NoScale),
		SendPropVector(EnvHeadcrabCanisterShared.NetworkVarFields.Direction, 0, PropFlags.NoScale),
		SendPropVector(EnvHeadcrabCanisterShared.NetworkVarFields.StartAngles, 0, PropFlags.NoScale),
		SendPropVector(EnvHeadcrabCanisterShared.NetworkVarFields.SkyboxOrigin, 0, PropFlags.NoScale),
		SendPropFloat(EnvHeadcrabCanisterShared.NetworkVarFields.SkyboxScale, 0, PropFlags.NoScale),
		SendPropBool(EnvHeadcrabCanisterShared.NetworkVarFields.InSkybox),
	]);
}

[NetworkName("CEnvHeadcrabCanister")]
public partial class EnvHeadcrabCanister : BaseAnimating
{
	[NetworkName("m_Shared")]
	[NetworkVarEmbedded] public partial EnvHeadcrabCanisterShared Shared { get; }
	[NetworkName("m_bLanded")]
	[NetworkVar] public partial bool Landed { get; set; }

	public static readonly SendTable DT_EnvHeadcrabCanister = new(DT_BaseAnimating, [
		SendPropDataTable("m_Shared", FIELD.OF(nameof(Shared)), EnvHeadcrabCanisterShared.DT_EnvHeadcrabCanisterShared),
		SendPropBool(NetworkVarFields.Landed),
	]);
	public static new readonly ServerClass ServerClass = new ServerClass(DT_EnvHeadcrabCanister);
}
