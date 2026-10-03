using Game.Shared;
using Source.Common;
using Source;
using System.Numerics;

namespace Game.Server;

using FIELD = FIELD<EnvHeadcrabCanister>;

public class EnvHeadcrabCanisterShared
{
	[NetworkName("m_flFlightSpeed")]
	public float FlightSpeed;
	[NetworkName("m_flLaunchTime")]
	public TimeUnit_t LaunchTime;
	[NetworkName("m_vecParabolaDirection")]
	public Vector3 ParabolaDirection;
	[NetworkName("m_flFlightTime")]
	public float FlightTime;
	[NetworkName("m_flWorldEnterTime")]
	public float WorldEnterTime;
	[NetworkName("m_flInitialZSpeed")]
	public float InitialZSpeed;
	[NetworkName("m_flZAcceleration")]
	public float ZAcceleration;
	[NetworkName("m_flHorizSpeed")]
	public float HorizSpeed;
	[NetworkName("m_bLaunchedFromWithinWorld")]
	public bool LaunchedFromWithinWorld;
	[NetworkName("m_vecStartPosition")]
	public Vector3 StartPosition;
	[NetworkName("m_vecEnterWorldPosition")]
	public Vector3 EnterWorldPosition;
	[NetworkName("m_vecDirection")]
	public Vector3 Direction;
	[NetworkName("m_vecStartAngles")]
	public Vector3 StartAngles;
	[NetworkName("m_vecSkyboxOrigin")]
	public Vector3 SkyboxOrigin;
	[NetworkName("m_flSkyboxScale")]
	public float SkyboxScale;
	[NetworkName("m_bInSkybox")]
	public bool InSkybox;

	public static readonly SendTable DT_EnvHeadcrabCanisterShared = new("DT_EnvHeadcrabCanisterShared", [
		SendPropFloat(Source.FIELD<EnvHeadcrabCanisterShared>.OF(nameof(FlightSpeed)), 0, PropFlags.NoScale),
		SendPropTime64(Source.FIELD<EnvHeadcrabCanisterShared>.OF(nameof(LaunchTime))),
		SendPropVector(Source.FIELD<EnvHeadcrabCanisterShared>.OF(nameof(ParabolaDirection)), 0, PropFlags.NoScale),
		SendPropFloat(Source.FIELD<EnvHeadcrabCanisterShared>.OF(nameof(FlightTime)), 0, PropFlags.NoScale),
		SendPropFloat(Source.FIELD<EnvHeadcrabCanisterShared>.OF(nameof(WorldEnterTime)), 0, PropFlags.NoScale),
		SendPropFloat(Source.FIELD<EnvHeadcrabCanisterShared>.OF(nameof(InitialZSpeed)), 0, PropFlags.NoScale),
		SendPropFloat(Source.FIELD<EnvHeadcrabCanisterShared>.OF(nameof(ZAcceleration)), 0, PropFlags.NoScale),
		SendPropFloat(Source.FIELD<EnvHeadcrabCanisterShared>.OF(nameof(HorizSpeed)), 0, PropFlags.NoScale),
		SendPropBool(Source.FIELD<EnvHeadcrabCanisterShared>.OF(nameof(LaunchedFromWithinWorld))),
		SendPropVector(Source.FIELD<EnvHeadcrabCanisterShared>.OF(nameof(StartPosition)), 0, PropFlags.NoScale),
		SendPropVector(Source.FIELD<EnvHeadcrabCanisterShared>.OF(nameof(EnterWorldPosition)), 0, PropFlags.NoScale),
		SendPropVector(Source.FIELD<EnvHeadcrabCanisterShared>.OF(nameof(Direction)), 0, PropFlags.NoScale),
		SendPropVector(Source.FIELD<EnvHeadcrabCanisterShared>.OF(nameof(StartAngles)), 0, PropFlags.NoScale),
		SendPropVector(Source.FIELD<EnvHeadcrabCanisterShared>.OF(nameof(SkyboxOrigin)), 0, PropFlags.NoScale),
		SendPropFloat(Source.FIELD<EnvHeadcrabCanisterShared>.OF(nameof(SkyboxScale)), 0, PropFlags.NoScale),
		SendPropBool(Source.FIELD<EnvHeadcrabCanisterShared>.OF(nameof(InSkybox))),
	]);
}

[LinkEntityToClass("env_headcrabcanister")]
[NetworkName("CEnvHeadcrabCanister")]
public class EnvHeadcrabCanister : BaseAnimating
{
	[NetworkName("m_Shared")]
	public EnvHeadcrabCanisterShared Shared = new();
	[NetworkName("m_bLanded")]
	public bool Landed;

	public static readonly SendTable DT_EnvHeadcrabCanister = new(DT_BaseAnimating, [
		SendPropDataTable("m_Shared", FIELD.OF(nameof(Shared)), EnvHeadcrabCanisterShared.DT_EnvHeadcrabCanisterShared),
		SendPropBool(FIELD.OF(nameof(Landed))),
	]);
	public static new readonly ServerClass ServerClass = new ServerClass(DT_EnvHeadcrabCanister);
}
