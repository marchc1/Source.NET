using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_PointCamera>;
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
	public static readonly new ClientClass ClientClass = new ClientClass("PointCamera", DT_PointCamera).WithManualClassID(StaticClassIndices.CPointCamera);

	public float FOV;
	public float Resolution;
	public bool FogEnable;
	public int FogColor;
	public int FogColorHDR;
	public float FogStart;
	public float FogEnd;
	public float FogMaxDensity;
	public bool FogRadial;
	public bool Active;
	public bool UseScreenAspectRatio;
	public bool GlobalOverride;
}
