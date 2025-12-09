using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<VortigauntEffectDispel>;
public class VortigauntEffectDispel : BaseEntity
{
	public static readonly SendTable DT_VortigauntEffectDispel = new(DT_BaseEntity, [
		SendPropBool(FIELD.OF(nameof(FadeOut))),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass("VortigauntEffectDispel", DT_VortigauntEffectDispel).WithManualClassID(StaticClassIndices.CVortigauntEffectDispel);

	public bool FadeOut;
}
