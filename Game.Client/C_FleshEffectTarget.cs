using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_FleshEffectTarget>;
public class C_FleshEffectTarget : C_BaseEntity
{
	public static readonly RecvTable DT_FleshEffectTarget = new(DT_BaseEntity, [
		RecvPropFloat(FIELD.OF(nameof(Radius))),
		RecvPropFloat(FIELD.OF(nameof(ScaleTime))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass("FleshEffectTarget", DT_FleshEffectTarget).WithManualClassID(StaticClassIndices.CFleshEffectTarget);

	public float Radius;
	public float ScaleTime;
}
