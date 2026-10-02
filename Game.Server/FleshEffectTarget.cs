using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<FleshEffectTarget>;
[LinkEntityToClass("point_flesh_effect_target")]
[NetworkName("CFleshEffectTarget")]
public class FleshEffectTarget : BaseEntity
{
	public static readonly SendTable DT_FleshEffectTarget = new(DT_BaseEntity, [
		SendPropFloat(FIELD.OF(nameof(Radius)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(ScaleTime)), 0, PropFlags.NoScale),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_FleshEffectTarget);

	[NetworkName("m_flRadius")]
	public float Radius;
	[NetworkName("m_flScaleTime")]
	public float ScaleTime;
}
