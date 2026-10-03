using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<VortigauntChargeToken>;
[LinkEntityToClass("vort_charge_token")]
[NetworkName("CVortigauntChargeToken")]
public class VortigauntChargeToken : BaseEntity
{
	public static readonly SendTable DT_VortigauntChargeToken = new(DT_BaseEntity, [
		SendPropBool(FIELD.OF(nameof(FadeOut))),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_VortigauntChargeToken);

	[NetworkName("m_bFadeOut")]
	public bool FadeOut;
}
