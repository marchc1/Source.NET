using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_PointCamera>;
[NetworkName("CPointCamera")]
public class C_PointCamera : C_BaseEntity
{
	public static readonly RecvTable DT_PointCamera = new(DT_BaseEntity, [
		RecvPropFloat(FIELD.OF(nameof(FOV))),
		RecvPropFloat(FIELD.OF(nameof(Resolution))),
		RecvPropBool(FIELD.OF(nameof(FogEnable))),
		RecvPropInt(FIELD.OF(nameof(FogColor))),
		RecvPropInt(FIELD.OF(nameof(FogColorHDR))),
		RecvPropFloat(FIELD.OF(nameof(FogStart))),
		RecvPropFloat(FIELD.OF(nameof(FogEnd))),
		RecvPropFloat(FIELD.OF(nameof(FogMaxDensity))),
		RecvPropBool(FIELD.OF(nameof(FogRadial))),
		RecvPropBool(FIELD.OF(nameof(Active))),
		RecvPropBool(FIELD.OF(nameof(UseScreenAspectRatio))),
		RecvPropBool(FIELD.OF(nameof(GlobalOverride))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass(DT_PointCamera);

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
