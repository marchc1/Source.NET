using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;

using FIELD = FIELD<EnvWind>;
using FIELD_EWS = FIELD<EnvWindShared>;
[NetworkName("CEnvWind")]
public partial class EnvWind : BaseEntity
{
	public static readonly SendTable DT_EnvWindShared = new(nameof(DT_EnvWindShared), [
		SendPropInt(EnvWindShared.NetworkVarFields.MinWind, 10, PropFlags.Unsigned),
		SendPropInt(EnvWindShared.NetworkVarFields.MaxWind, 10, PropFlags.Unsigned),
		SendPropInt(EnvWindShared.NetworkVarFields.MinGust, 10, PropFlags.Unsigned),
		SendPropInt(EnvWindShared.NetworkVarFields.MaxGust, 10, PropFlags.Unsigned),
		SendPropFloat(EnvWindShared.NetworkVarFields.MinGustDelay),
		SendPropFloat(EnvWindShared.NetworkVarFields.MaxGustDelay),
		SendPropInt(EnvWindShared.NetworkVarFields.GustDirChange, 9, PropFlags.Unsigned),
		SendPropInt(EnvWindShared.NetworkVarFields.WindSeed, 32, PropFlags.Unsigned),
		SendPropInt(EnvWindShared.NetworkVarFields.InitialWindDir, 9, PropFlags.Unsigned),
		SendPropFloat(EnvWindShared.NetworkVarFields.InitialWindSpeed),
		SendPropFloat(EnvWindShared.NetworkVarFields.StartTime),
		SendPropFloat(EnvWindShared.NetworkVarFields.GustDuration),
		SendPropInt(EnvWindShared.NetworkVarFields.WindRadius, 32, PropFlags.NoScale),
	]);

	public static readonly SendTable DT_EnvWind = new([
		SendPropDataTable("m_EnvWindShared", FIELD.OF(nameof(EnvWindShared)), DT_EnvWindShared),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_EnvWind);

	[NetworkName("m_EnvWindShared")]
	[NetworkVarEmbedded] public partial EnvWindShared EnvWindShared { get; }
}
