using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_EnvScreenEffect>;
[NetworkName("CEnvScreenEffect")]
public class C_EnvScreenEffect : C_BaseEntity
{
	public static readonly RecvTable DT_EnvScreenEffect = new(DT_BaseEntity, [
		RecvPropFloat(FIELD.OF(nameof(Duration))),
		RecvPropInt(FIELD.OF(nameof(Type))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass(DT_EnvScreenEffect);

	[NetworkName("m_flDuration")]
	public float Duration;
	[NetworkName("m_nType")]
	public int Type;
}
