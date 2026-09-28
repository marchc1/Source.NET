using Source.Common;
using Source;
using System.Numerics;

namespace Game.Client;

using FIELD = FIELD<C_EnvHeadcrabCanister>;

public class C_EnvHeadcrabCanisterShared
{
	public float FlightSpeed;
	public float LaunchTime;
	public Vector3 ParabolaDirection;
	public float FlightTime;
	public float WorldEnterTime;
	public float InitialZSpeed;
	public float ZAcceleration;
	public float HorizSpeed;
	public bool LaunchedFromWithinWorld;
	public Vector3 StartPosition;
	public Vector3 EnterWorldPosition;
	public Vector3 Direction;
	public Vector3 StartAngles;
	public Vector3 SkyboxOrigin;
	public float SkyboxScale;
	public bool InSkybox;

	public static readonly RecvTable DT_EnvHeadcrabCanisterShared = new("DT_EnvHeadcrabCanisterShared", [
		RecvPropFloat(Source.FIELD<C_EnvHeadcrabCanisterShared>.OF(nameof(FlightSpeed))),
		RecvPropTime(Source.FIELD<C_EnvHeadcrabCanisterShared>.OF(nameof(LaunchTime))),
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

public class C_EnvHeadcrabCanister : C_BaseAnimating
{
	public C_EnvHeadcrabCanisterShared Shared = new();
	public bool Landed;

	public static readonly RecvTable DT_EnvHeadcrabCanister = new(DT_BaseAnimating, [
		RecvPropDataTable(nameof(Shared), FIELD.OF(nameof(Shared)), C_EnvHeadcrabCanisterShared.DT_EnvHeadcrabCanisterShared),
		RecvPropBool(FIELD.OF(nameof(Landed))),
	]);
	public static new readonly ClientClass ClientClass = new ClientClass("EnvHeadcrabCanister", DT_EnvHeadcrabCanister).WithManualClassID(Game.Shared.StaticClassIndices.CEnvHeadcrabCanister);
}
