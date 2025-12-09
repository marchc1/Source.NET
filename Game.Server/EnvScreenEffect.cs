using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<EnvScreenEffect>;
public class EnvScreenEffect : BaseEntity
{
	public static readonly SendTable DT_EnvScreenEffect = new(DT_BaseEntity, [
		SendPropFloat(FIELD.OF(nameof(Duration)), 0, PropFlags.NoScale),
		SendPropInt(FIELD.OF(nameof(Type)), 32, PropFlags.Unsigned),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass("EnvScreenEffect", DT_EnvScreenEffect).WithManualClassID(StaticClassIndices.CEnvScreenEffect);

	public float Duration;
	public int Type;
}
