using Game.Shared;

using Source.Common;

namespace Game.Server;
using FIELD = Source.FIELD<FogController>;

[LinkEntityToClass("env_fog_controller")]
[NetworkName("CFogController")]
public partial class FogController : BaseEntity
{
	[NetworkName("m_fog")]
	[NetworkVarEmbedded] public partial FogParams.NetworkVar Fog { get; }
	public static readonly SendTable DT_FogController = new([
		SendPropInt(NetworkVarFields.Fog_Enable, 1, PropFlags.Unsigned),
		SendPropInt(NetworkVarFields.Fog_Blend, 1, PropFlags.Unsigned),
		SendPropInt(NetworkVarFields.Fog_Radial, 1, PropFlags.Unsigned),
		SendPropVector(NetworkVarFields.Fog_DirPrimary, 0, PropFlags.Coord),
		SendPropInt(NetworkVarFields.Fog_ColorPrimary, 32, PropFlags.Unsigned),
		SendPropInt(NetworkVarFields.Fog_ColorSecondary, 32, PropFlags.Unsigned),
		SendPropInt(NetworkVarFields.Fog_ColorPrimaryHDR, 32, PropFlags.Unsigned),
		SendPropInt(NetworkVarFields.Fog_ColorSecondaryHDR, 32, PropFlags.Unsigned),
		SendPropFloat(NetworkVarFields.Fog_Start, 0, PropFlags.NoScale),
		SendPropFloat(NetworkVarFields.Fog_End, 0, PropFlags.NoScale),
		SendPropFloat(NetworkVarFields.Fog_MaxDensity, 0, PropFlags.NoScale),
		SendPropFloat(NetworkVarFields.Fog_FarZ, 0, PropFlags.NoScale),

		SendPropInt(NetworkVarFields.Fog_ColorPrimaryLerpTo, 32, PropFlags.Unsigned),
		SendPropInt(NetworkVarFields.Fog_ColorSecondaryLerpTo, 32, PropFlags.Unsigned),
		SendPropFloat(NetworkVarFields.Fog_StartLerpTo, 0, PropFlags.NoScale, 0, 0),
		SendPropFloat(NetworkVarFields.Fog_EndLerpTo, 0, PropFlags.NoScale, 0, 0),
		SendPropFloat(NetworkVarFields.Fog_MaxDensityLerpTo, 0, PropFlags.NoScale, 0, 0),
		SendPropFloat(NetworkVarFields.Fog_LerpTime, 0, PropFlags.NoScale, 0, 0),
		SendPropFloat(NetworkVarFields.Fog_Duration, 0, PropFlags.NoScale, 0, 0),
		SendPropFloat(NetworkVarFields.Fog_HDRColorScale, 0, PropFlags.NoScale, 0, 0),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_FogController);
}
