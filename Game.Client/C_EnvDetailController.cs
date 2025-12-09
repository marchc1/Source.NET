using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_EnvDetailController>;
public class C_EnvDetailController : C_BaseEntity
{
	public static readonly RecvTable DT_EnvDetailController = new([
		RecvPropFloat(FIELD.OF(nameof(FadeStartDist))),
		RecvPropFloat(FIELD.OF(nameof(FadeEndDist))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass("EnvDetailController", DT_EnvDetailController).WithManualClassID(StaticClassIndices.CEnvDetailController);

	public float FadeStartDist;
	public float FadeEndDist;
}
