using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<VortigauntEffectDispel>;
[LinkEntityToClass("vort_effect_dispel")]
[NetworkName("CVortigauntEffectDispel")]
public class VortigauntEffectDispel : BaseEntity
{
	public static readonly SendTable DT_VortigauntEffectDispel = new(DT_BaseEntity, [
		SendPropBool(FIELD.OF(nameof(FadeOut))),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_VortigauntEffectDispel);

	[NetworkName("m_bFadeOut")]
	public bool FadeOut;
}
