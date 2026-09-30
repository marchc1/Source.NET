using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<VortigauntChargeToken>;
[NetworkName("CVortigauntChargeToken")]
public partial class VortigauntChargeToken : BaseEntity
{
	public static readonly SendTable DT_VortigauntChargeToken = new(DT_BaseEntity, [
		SendPropBool(NetworkVarFields.FadeOut),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_VortigauntChargeToken);

	[NetworkName("m_bFadeOut")]
	[NetworkVar] public partial bool FadeOut { get; set; }
}
