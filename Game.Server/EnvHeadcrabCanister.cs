using Source.Common;
using Source;
using System.Numerics;

namespace Game.Server;

using FIELD = FIELD<EnvHeadcrabCanister>;

public class EnvHeadcrabCanisterShared
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

	public static readonly SendTable DT_EnvHeadcrabCanisterShared = new([
		SendPropFloat(Source.FIELD<EnvHeadcrabCanisterShared>.OF(nameof(FlightSpeed)), 0, PropFlags.NoScale),
		SendPropTime(Source.FIELD<EnvHeadcrabCanisterShared>.OF(nameof(LaunchTime))),
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

public class EnvHeadcrabCanister : BaseAnimating
{
	public EnvHeadcrabCanisterShared Shared = new();
	public bool Landed;

	public static readonly SendTable DT_EnvHeadcrabCanister = new(DT_BaseAnimating, [
		SendPropDataTable(nameof(Shared), FIELD.OF(nameof(Shared)), EnvHeadcrabCanisterShared.DT_EnvHeadcrabCanisterShared),
		SendPropBool(FIELD.OF(nameof(Landed))),
	]);
	public static new readonly ServerClass ServerClass = new ServerClass("EnvHeadcrabCanister", DT_EnvHeadcrabCanister).WithManualClassID(Game.Shared.StaticClassIndices.CEnvHeadcrabCanister);
}
