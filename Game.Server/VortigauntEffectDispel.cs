using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<VortigauntEffectDispel>;
[NetworkName("CVortigauntEffectDispel")]
public partial class VortigauntEffectDispel : BaseEntity
{
	public static readonly SendTable DT_VortigauntEffectDispel = new(DT_BaseEntity, [
		SendPropBool(NetworkVarFields.FadeOut),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_VortigauntEffectDispel);

	[NetworkName("m_bFadeOut")]
	[NetworkVar] public partial bool FadeOut { get; set; }
}
