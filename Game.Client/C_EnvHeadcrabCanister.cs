using Source.Common;
using Source;
using System.Numerics;

namespace Game.Client;

using FIELD = FIELD<C_EnvHeadcrabCanister>;

public class C_EnvHeadcrabCanisterShared
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

	public static readonly RecvTable DT_EnvHeadcrabCanisterShared = new("DT_EnvHeadcrabCanisterShared", [
		RecvPropFloat(Source.FIELD<C_EnvHeadcrabCanisterShared>.OF(nameof(FlightSpeed))),
		RecvPropTime64(Source.FIELD<C_EnvHeadcrabCanisterShared>.OF(nameof(LaunchTime))),
		RecvPropVector(Source.FIELD<C_EnvHeadcrabCanisterShared>.OF(nameof(ParabolaDirection))),
		RecvPropFloat(Source.FIELD<C_EnvHeadcrabCanisterShared>.OF(nameof(FlightTime))),
		RecvPropFloat(Source.FIELD<C_EnvHeadcrabCanisterShared>.OF(nameof(WorldEnterTime))),
		RecvPropFloat(Source.FIELD<C_EnvHeadcrabCanisterShared>.OF(nameof(InitialZSpeed))),
		RecvPropFloat(Source.FIELD<C_EnvHeadcrabCanisterShared>.OF(nameof(ZAcceleration))),
		RecvPropFloat(Source.FIELD<C_EnvHeadcrabCanisterShared>.OF(nameof(HorizSpeed))),
		RecvPropBool(Source.FIELD<C_EnvHeadcrabCanisterShared>.OF(nameof(LaunchedFromWithinWorld))),
		RecvPropVector(Source.FIELD<C_EnvHeadcrabCanisterShared>.OF(nameof(StartPosition))),
		RecvPropVector(Source.FIELD<C_EnvHeadcrabCanisterShared>.OF(nameof(EnterWorldPosition))),
		RecvPropVector(Source.FIELD<C_EnvHeadcrabCanisterShared>.OF(nameof(Direction))),
		RecvPropVector(Source.FIELD<C_EnvHeadcrabCanisterShared>.OF(nameof(StartAngles))),
		RecvPropVector(Source.FIELD<C_EnvHeadcrabCanisterShared>.OF(nameof(SkyboxOrigin))),
		RecvPropFloat(Source.FIELD<C_EnvHeadcrabCanisterShared>.OF(nameof(SkyboxScale))),
		RecvPropBool(Source.FIELD<C_EnvHeadcrabCanisterShared>.OF(nameof(InSkybox))),
	]);
}

[NetworkName("CEnvHeadcrabCanister")]
public class C_EnvHeadcrabCanister : C_BaseAnimating
{
	[NetworkName("m_Shared")]
	public C_EnvHeadcrabCanisterShared Shared = new();
	[NetworkName("m_bLanded")]
	public bool Landed;

	public static readonly RecvTable DT_EnvHeadcrabCanister = new(DT_BaseAnimating, [
		RecvPropDataTable("m_Shared", FIELD.OF(nameof(Shared)), C_EnvHeadcrabCanisterShared.DT_EnvHeadcrabCanisterShared),
		RecvPropBool(FIELD.OF(nameof(Landed))),
	]);
	public static new readonly ClientClass ClientClass = new ClientClass(DT_EnvHeadcrabCanister);
}
