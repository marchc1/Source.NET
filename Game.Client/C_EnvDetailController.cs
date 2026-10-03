using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_EnvDetailController>;
[LinkEntityToClass("env_detail_controller")]
[NetworkName("CEnvDetailController")]
public class C_EnvDetailController : C_BaseEntity
{
	public static readonly RecvTable DT_DetailController = new([
		RecvPropFloat(FIELD.OF(nameof(FadeStartDist))),
		RecvPropFloat(FIELD.OF(nameof(FadeEndDist))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass(DT_DetailController);

	[NetworkName("m_flFadeStartDist")]
	public float FadeStartDist;
	[NetworkName("m_flFadeEndDist")]
	public float FadeEndDist;

	static C_EnvDetailController? s_DetailController;

	public C_EnvDetailController() {
		s_DetailController = this;
	}

	public static C_EnvDetailController? GetDetailController() => s_DetailController;
}
