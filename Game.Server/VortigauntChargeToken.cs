using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<VortigauntChargeToken>;
public class VortigauntChargeToken : BaseEntity
{
	public static readonly SendTable DT_VortigauntChargeToken = new(DT_BaseEntity, [
		SendPropBool(FIELD.OF(nameof(FadeOut))),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass("VortigauntChargeToken", DT_VortigauntChargeToken).WithManualClassID(StaticClassIndices.CVortigauntChargeToken);

	public bool FadeOut;
}
