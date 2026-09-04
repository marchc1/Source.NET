using Game.Shared;

using Source.Common;

namespace Game.Server;
using FIELD = Source.FIELD<FogController>;

[LinkEntityToClass("env_fog_controller")]
public class FogController : BaseEntity
{
	public FogParams Fog;
	public static readonly SendTable DT_FogController = new([
		SendPropInt(FIELD.OF("Fog.Enable"), 1, PropFlags.Unsigned),
		SendPropInt(FIELD.OF("Fog.Blend"), 1, PropFlags.Unsigned),
		SendPropInt(FIELD.OF("Fog.Radial"), 1, PropFlags.Unsigned),
		SendPropVector(FIELD.OF("Fog.DirPrimary"), 0, PropFlags.Coord),
		SendPropInt(FIELD.OF("Fog.ColorPrimary"), 32, PropFlags.Unsigned),
		SendPropInt(FIELD.OF("Fog.ColorSecondary"), 32, PropFlags.Unsigned),
		SendPropInt(FIELD.OF("Fog.ColorPrimaryHDR"), 32, PropFlags.Unsigned),
		SendPropInt(FIELD.OF("Fog.ColorSecondaryHDR"), 32, PropFlags.Unsigned),
		SendPropFloat(FIELD.OF("Fog.Start"), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF("Fog.End"), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF("Fog.MaxDensity"), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF("Fog.FarZ"), 0, PropFlags.NoScale),

		SendPropInt(FIELD.OF("Fog.ColorPrimaryLerpTo"), 32, PropFlags.Unsigned),
		SendPropInt(FIELD.OF("Fog.ColorSecondaryLerpTo"), 32, PropFlags.Unsigned),
		SendPropFloat(FIELD.OF("Fog.StartLerpTo"), 0, PropFlags.NoScale, 0, 0),
		SendPropFloat(FIELD.OF("Fog.EndLerpTo"), 0, PropFlags.NoScale, 0, 0),
		SendPropFloat(FIELD.OF("Fog.MaxDensityLerpTo"), 0, PropFlags.NoScale, 0, 0),
		SendPropFloat(FIELD.OF("Fog.LerpTime"), 0, PropFlags.NoScale, 0, 0),
		SendPropFloat(FIELD.OF("Fog.Duration"), 0, PropFlags.NoScale, 0, 0),
		SendPropFloat(FIELD.OF("Fog.HDRColorScale"), 0, PropFlags.NoScale, 0, 0),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass("FogController", DT_FogController).WithManualClassID(StaticClassIndices.CFogController);
}
