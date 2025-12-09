using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_EnvScreenEffect>;
public class C_EnvScreenEffect : C_BaseEntity
{
	public static readonly RecvTable DT_EnvScreenEffect = new(DT_BaseEntity, [
		RecvPropFloat(FIELD.OF(nameof(Duration))),
		RecvPropInt(FIELD.OF(nameof(Type))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass("EnvScreenEffect", DT_EnvScreenEffect).WithManualClassID(StaticClassIndices.CEnvScreenEffect);

	public float Duration;
	public int Type;
}
