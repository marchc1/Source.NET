namespace Game.Server;

using Game.Shared;

using Source.Common;

using FIELD = Source.FIELD<EnvTonemapController>;

[LinkEntityToClass("env_tonemap_controller")]
public class EnvTonemapController : PointEntity
{
	public static readonly SendTable DT_EnvTonemapController = new(DT_BaseEntity, [
		SendPropBool(FIELD.OF(nameof(UseCustomAutoExposureMin))),
		SendPropBool(FIELD.OF(nameof(UseCustomAutoExposureMax))),
		SendPropBool(FIELD.OF(nameof(UseCustomBloomScale))),
		SendPropFloat(FIELD.OF(nameof(CustomAutoExposureMin)), 0, PropFlags.NoScale, 0, 0),
		SendPropFloat(FIELD.OF(nameof(CustomAutoExposureMax)), 0, PropFlags.NoScale, 0, 0),
		SendPropFloat(FIELD.OF(nameof(CustomBloomScale)), 0, PropFlags.NoScale, 0, 0),
		SendPropFloat(FIELD.OF(nameof(CustomBloomScaleMinimum)), 0, PropFlags.NoScale, 0, 0),
	]); public static readonly new ServerClass ServerClass = new ServerClass("EnvTonemapController", DT_EnvTonemapController).WithManualClassID(StaticClassIndices.CEnvTonemapController);

	public bool UseCustomAutoExposureMin;
	public bool UseCustomAutoExposureMax;
	public bool UseCustomBloomScale;
	public float CustomAutoExposureMin;
	public float CustomAutoExposureMax;
	public float CustomBloomScale;
	public float CustomBloomScaleMinimum;
}