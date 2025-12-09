using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_VortigauntChargeToken>;
public class C_VortigauntChargeToken : C_BaseEntity
{
	public static readonly RecvTable DT_VortigauntChargeToken = new(DT_BaseEntity, [
		RecvPropBool(FIELD.OF(nameof(FadeOut))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass("VortigauntChargeToken", DT_VortigauntChargeToken).WithManualClassID(StaticClassIndices.CVortigauntChargeToken);

	public bool FadeOut;
}
