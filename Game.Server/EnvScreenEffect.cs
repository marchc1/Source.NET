using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<EnvScreenEffect>;
[LinkEntityToClass("env_screeneffect")]
[NetworkName("CEnvScreenEffect")]
public class EnvScreenEffect : BaseEntity
{
	public static readonly SendTable DT_EnvScreenEffect = new(DT_BaseEntity, [
		SendPropFloat(FIELD.OF(nameof(Duration)), 0, PropFlags.NoScale),
		SendPropInt(FIELD.OF(nameof(Type)), 12, PropFlags.Unsigned),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_EnvScreenEffect);

	[NetworkName("m_flDuration")]
	public float Duration;
	[NetworkName("m_nType")]
	public int Type;
}
