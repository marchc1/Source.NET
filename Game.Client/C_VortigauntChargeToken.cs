using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_VortigauntChargeToken>;
[NetworkName("CVortigauntChargeToken")]
public class C_VortigauntChargeToken : C_BaseEntity
{
	public static readonly RecvTable DT_VortigauntChargeToken = new(DT_BaseEntity, [
		RecvPropBool(FIELD.OF(nameof(FadeOut))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass(DT_VortigauntChargeToken);

	[NetworkName("m_bFadeOut")]
	public bool FadeOut;
}
