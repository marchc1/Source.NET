using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;

using FIELD = FIELD<C_EnvWind>;
using FIELD_EWS = FIELD<EnvWindShared>;
public class C_EnvWind : C_BaseEntity
{
	public static readonly RecvTable DT_EnvWindShared = new([
		RecvPropInt(FIELD_EWS.OF(nameof(EnvWindShared.MinWind))),
		RecvPropInt(FIELD_EWS.OF(nameof(EnvWindShared.MaxWind))),
		RecvPropInt(FIELD_EWS.OF(nameof(EnvWindShared.MinGust))),
		RecvPropInt(FIELD_EWS.OF(nameof(EnvWindShared.MaxGust))),
		RecvPropFloat(FIELD_EWS.OF(nameof(EnvWindShared.MinGustDelay))),
		RecvPropFloat(FIELD_EWS.OF(nameof(EnvWindShared.MaxGustDelay))),
		RecvPropInt(FIELD_EWS.OF(nameof(EnvWindShared.GustDirChange))),
		RecvPropInt(FIELD_EWS.OF(nameof(EnvWindShared.WindSeed))),
		RecvPropInt(FIELD_EWS.OF(nameof(EnvWindShared.InitialWindDir))),
		RecvPropFloat(FIELD_EWS.OF(nameof(EnvWindShared.InitialWindSpeed))),
		RecvPropFloat(FIELD_EWS.OF(nameof(EnvWindShared.StartTime))),
		RecvPropFloat(FIELD_EWS.OF(nameof(EnvWindShared.GustDuration))),
	]);
	public static readonly ClientClass CC_EnvWindShared = new("EnvWindShared", DT_EnvWindShared);

	public static readonly RecvTable DT_EnvWind = new([
		RecvPropDataTable(nameof(EnvWindShared), FIELD.OF(nameof(EnvWindShared)), DT_EnvWindShared, 0, RECV_GET_OBJECT_AT_FIELD(FIELD.OF(nameof(EnvWindShared))))
	]);
	public static readonly new ClientClass ClientClass = new ClientClass("EnvWind", DT_EnvWind).WithManualClassID(StaticClassIndices.CEnvWind);

	public readonly EnvWindShared EnvWindShared = new();
}
