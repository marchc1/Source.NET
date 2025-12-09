using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<EnvDetailController>;
public class EnvDetailController : BaseEntity
{
	public static readonly SendTable DT_EnvDetailController = new([
		SendPropFloat(FIELD.OF(nameof(FadeStartDist)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(FadeEndDist)), 0, PropFlags.NoScale),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass("EnvDetailController", DT_EnvDetailController).WithManualClassID(StaticClassIndices.CEnvDetailController);

	public float FadeStartDist;
	public float FadeEndDist;
}
