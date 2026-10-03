using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<PointCamera>;
[LinkEntityToClass("point_camera")]
[NetworkName("CPointCamera")]
public class PointCamera : BaseEntity
{
	public static readonly SendTable DT_PointCamera = new(DT_BaseEntity, [
		SendPropFloat(FIELD.OF(nameof(FOV)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(Resolution)), 0, PropFlags.NoScale),
		SendPropBool(FIELD.OF(nameof(FogEnable))),
		SendPropInt(FIELD.OF(nameof(FogColor)), 32, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(FogColorHDR)), 32, PropFlags.Unsigned),
		SendPropFloat(FIELD.OF(nameof(FogStart)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(FogEnd)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(FogMaxDensity)), 0, PropFlags.NoScale),
		SendPropBool(FIELD.OF(nameof(FogRadial))),
		SendPropBool(FIELD.OF(nameof(Active))),
		SendPropBool(FIELD.OF(nameof(UseScreenAspectRatio))),
		SendPropBool(FIELD.OF(nameof(GlobalOverride))),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_PointCamera);

	[NetworkName("m_FOV")]
	public float FOV;
	[NetworkName("m_Resolution")]
	public float Resolution;
	[NetworkName("m_bFogEnable")]
	public bool FogEnable;
	[NetworkName("m_FogColor")]
	public int FogColor;
	[NetworkName("m_FogColorHDR")]
	public int FogColorHDR;
	[NetworkName("m_flFogStart")]
	public float FogStart;
	[NetworkName("m_flFogEnd")]
	public float FogEnd;
	[NetworkName("m_flFogMaxDensity")]
	public float FogMaxDensity;
	[NetworkName("m_bFogRadial")]
	public bool FogRadial;
	[NetworkName("m_bActive")]
	public bool Active;
	[NetworkName("m_bUseScreenAspectRatio")]
	public bool UseScreenAspectRatio;
	[NetworkName("m_bGlobalOverride")]
	public bool GlobalOverride;
}
