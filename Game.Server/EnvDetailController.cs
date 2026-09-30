using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<EnvDetailController>;
[NetworkName("CEnvDetailController")]
public partial class EnvDetailController : BaseEntity
{
	public static readonly SendTable DT_DetailController = new([
		SendPropFloat(NetworkVarFields.FadeStartDist, 0, PropFlags.NoScale),
		SendPropFloat(NetworkVarFields.FadeEndDist, 0, PropFlags.NoScale),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_DetailController);

	[NetworkName("m_flFadeStartDist")]
	[NetworkVar] public partial float FadeStartDist { get; set; }
	[NetworkName("m_flFadeEndDist")]
	[NetworkVar] public partial float FadeEndDist { get; set; }
}
