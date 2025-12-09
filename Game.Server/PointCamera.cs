using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<PointCamera>;
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
	public static readonly new ServerClass ServerClass = new ServerClass("PointCamera", DT_PointCamera).WithManualClassID(StaticClassIndices.CPointCamera);

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
