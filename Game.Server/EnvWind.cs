using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;

using FIELD = FIELD<EnvWind>;
using FIELD_EWS = FIELD<EnvWindShared>;
public class EnvWind : BaseEntity
{
	public static readonly SendTable DT_EnvWindShared = new([
		SendPropInt(FIELD_EWS.OF(nameof(EnvWindShared.MinWind))),
		SendPropInt(FIELD_EWS.OF(nameof(EnvWindShared.MaxWind))),
		SendPropInt(FIELD_EWS.OF(nameof(EnvWindShared.MinGust))),
		SendPropInt(FIELD_EWS.OF(nameof(EnvWindShared.MaxGust))),
		SendPropFloat(FIELD_EWS.OF(nameof(EnvWindShared.MinGustDelay))),
		SendPropFloat(FIELD_EWS.OF(nameof(EnvWindShared.MaxGustDelay))),
		SendPropInt(FIELD_EWS.OF(nameof(EnvWindShared.GustDirChange))),
		SendPropInt(FIELD_EWS.OF(nameof(EnvWindShared.WindSeed))),
		SendPropInt(FIELD_EWS.OF(nameof(EnvWindShared.InitialWindDir))),
		SendPropFloat(FIELD_EWS.OF(nameof(EnvWindShared.InitialWindSpeed))),
		SendPropFloat(FIELD_EWS.OF(nameof(EnvWindShared.StartTime))),
		SendPropFloat(FIELD_EWS.OF(nameof(EnvWindShared.GustDuration))),
	]);
	public static readonly ServerClass CC_EnvWindShared = new("EnvWindShared", DT_EnvWindShared);

	public static readonly SendTable DT_EnvWind = new([
		SendPropDataTable(nameof(EnvWindShared), FIELD.OF(nameof(EnvWindShared)), DT_EnvWindShared),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass("EnvWind", DT_EnvWind).WithManualClassID(StaticClassIndices.CEnvWind);

	public readonly EnvWindShared EnvWindShared = new();
}
