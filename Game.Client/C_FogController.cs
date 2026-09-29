using Game.Client;
using Game.Shared;

using Source.Common;

namespace Game.Server;
using FIELD = Source.FIELD<C_FogController>;

[NetworkName("CFogController")]
public class C_FogController : C_BaseEntity
{
	[NetworkName("m_fog")]
	public FogParams Fog;
	public static readonly RecvTable DT_FogController = new([
		RecvPropInt(FIELD.OF("Fog.Enable")),
		RecvPropInt(FIELD.OF("Fog.Blend")),
		RecvPropInt(FIELD.OF("Fog.Radial")),
		RecvPropVector(FIELD.OF("Fog.DirPrimary")),
		RecvPropInt(FIELD.OF("Fog.ColorPrimary")),
		RecvPropInt(FIELD.OF("Fog.ColorSecondary")),
		RecvPropInt(FIELD.OF("Fog.ColorPrimaryHDR")),
		RecvPropInt(FIELD.OF("Fog.ColorSecondaryHDR")),
		RecvPropFloat(FIELD.OF("Fog.Start")),
		RecvPropFloat(FIELD.OF("Fog.End")),
		RecvPropFloat(FIELD.OF("Fog.MaxDensity")),
		RecvPropFloat(FIELD.OF("Fog.FarZ")),

		RecvPropInt(FIELD.OF("Fog.ColorPrimaryLerpTo")),
		RecvPropInt(FIELD.OF("Fog.ColorSecondaryLerpTo")),
		RecvPropFloat(FIELD.OF("Fog.StartLerpTo")),
		RecvPropFloat(FIELD.OF("Fog.EndLerpTo")),
		RecvPropFloat(FIELD.OF("Fog.MaxDensityLerpTo")),
		RecvPropFloat(FIELD.OF("Fog.LerpTime")),
		RecvPropFloat(FIELD.OF("Fog.Duration")),
		RecvPropFloat(FIELD.OF("Fog.HDRColorScale")),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass(DT_FogController);
}