using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;

using FIELD = FIELD<EnvWind>;
using FIELD_EWS = FIELD<EnvWindShared>;
[LinkEntityToClass("env_wind")]
[NetworkName("CEnvWind")]
public class EnvWind : BaseEntity
{
	public static readonly SendTable DT_EnvWindShared = new(nameof(DT_EnvWindShared), [
		SendPropInt(FIELD_EWS.OF(nameof(EnvWindShared.MinWind)), 10, PropFlags.Unsigned),
		SendPropInt(FIELD_EWS.OF(nameof(EnvWindShared.MaxWind)), 10, PropFlags.Unsigned),
		SendPropInt(FIELD_EWS.OF(nameof(EnvWindShared.MinGust)), 10, PropFlags.Unsigned),
		SendPropInt(FIELD_EWS.OF(nameof(EnvWindShared.MaxGust)), 10, PropFlags.Unsigned),
		SendPropFloat(FIELD_EWS.OF(nameof(EnvWindShared.MinGustDelay))),
		SendPropFloat(FIELD_EWS.OF(nameof(EnvWindShared.MaxGustDelay))),
		SendPropInt(FIELD_EWS.OF(nameof(EnvWindShared.GustDirChange)), 9, PropFlags.Unsigned),
		SendPropInt(FIELD_EWS.OF(nameof(EnvWindShared.WindSeed)), 32, PropFlags.Unsigned),
		SendPropInt(FIELD_EWS.OF(nameof(EnvWindShared.InitialWindDir)), 9, PropFlags.Unsigned),
		SendPropFloat(FIELD_EWS.OF(nameof(EnvWindShared.InitialWindSpeed))),
		SendPropFloat(FIELD_EWS.OF(nameof(EnvWindShared.StartTime))),
		SendPropFloat(FIELD_EWS.OF(nameof(EnvWindShared.GustDuration))),
		SendPropInt(FIELD_EWS.OF(nameof(EnvWindShared.WindRadius)), 32, PropFlags.NoScale),
	]);

	public static readonly SendTable DT_EnvWind = new([
		SendPropDataTable("m_EnvWindShared", FIELD.OF(nameof(EnvWindShared)), DT_EnvWindShared),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_EnvWind);

	[NetworkName("m_EnvWindShared")]
	public readonly EnvWindShared EnvWindShared = new();
}
