using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<EnvDetailController>;
[LinkEntityToClass("env_detail_controller")]
[NetworkName("CEnvDetailController")]
public class EnvDetailController : BaseEntity
{
	public static readonly SendTable DT_DetailController = new([
		SendPropFloat(FIELD.OF(nameof(FadeStartDist)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(FadeEndDist)), 0, PropFlags.NoScale),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_DetailController);

	[NetworkName("m_flFadeStartDist")]
	public float FadeStartDist;
	[NetworkName("m_flFadeEndDist")]
	public float FadeEndDist;
}
