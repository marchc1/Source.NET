using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_VortigauntEffectDispel>;
public class C_VortigauntEffectDispel : C_BaseEntity
{
	public static readonly RecvTable DT_VortigauntEffectDispel = new(DT_BaseEntity, [
		RecvPropBool(FIELD.OF(nameof(FadeOut))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass("VortigauntEffectDispel", DT_VortigauntEffectDispel).WithManualClassID(StaticClassIndices.CVortigauntEffectDispel);

	public bool FadeOut;
}
