using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<PointCamera>;
[NetworkName("CPointCamera")]
public partial class PointCamera : BaseEntity
{
	public static readonly SendTable DT_PointCamera = new(DT_BaseEntity, [
		SendPropFloat(NetworkVarFields.FOV, 0, PropFlags.NoScale),
		SendPropFloat(NetworkVarFields.Resolution, 0, PropFlags.NoScale),
		SendPropBool(NetworkVarFields.FogEnable),
		SendPropInt(NetworkVarFields.FogColor, 32, PropFlags.Unsigned),
		SendPropInt(NetworkVarFields.FogColorHDR, 32, PropFlags.Unsigned),
		SendPropFloat(NetworkVarFields.FogStart, 0, PropFlags.NoScale),
		SendPropFloat(NetworkVarFields.FogEnd, 0, PropFlags.NoScale),
		SendPropFloat(NetworkVarFields.FogMaxDensity, 0, PropFlags.NoScale),
		SendPropBool(NetworkVarFields.FogRadial),
		SendPropBool(NetworkVarFields.Active),
		SendPropBool(NetworkVarFields.UseScreenAspectRatio),
		SendPropBool(NetworkVarFields.GlobalOverride),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_PointCamera);

	[NetworkName("m_FOV")]
	[NetworkVar] public partial float FOV { get; set; }
	[NetworkName("m_Resolution")]
	[NetworkVar] public partial float Resolution { get; set; }
	[NetworkName("m_bFogEnable")]
	[NetworkVar] public partial bool FogEnable { get; set; }
	[NetworkName("m_FogColor")]
	[NetworkVar] public partial int FogColor { get; set; }
	[NetworkName("m_FogColorHDR")]
	[NetworkVar] public partial int FogColorHDR { get; set; }
	[NetworkName("m_flFogStart")]
	[NetworkVar] public partial float FogStart { get; set; }
	[NetworkName("m_flFogEnd")]
	[NetworkVar] public partial float FogEnd { get; set; }
	[NetworkName("m_flFogMaxDensity")]
	[NetworkVar] public partial float FogMaxDensity { get; set; }
	[NetworkName("m_bFogRadial")]
	[NetworkVar] public partial bool FogRadial { get; set; }
	[NetworkName("m_bActive")]
	[NetworkVar] public partial bool Active { get; set; }
	[NetworkName("m_bUseScreenAspectRatio")]
	[NetworkVar] public partial bool UseScreenAspectRatio { get; set; }
	[NetworkName("m_bGlobalOverride")]
	[NetworkVar] public partial bool GlobalOverride { get; set; }
}
