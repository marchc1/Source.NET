using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_EnvParticleScript>;
public class C_EnvParticleScript : C_BaseAnimating
{
	public static readonly RecvTable DT_EnvParticleScript = new(DT_BaseAnimating, [
		RecvPropFloat(FIELD.OF(nameof(SequenceScale))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass("EnvParticleScript", DT_EnvParticleScript).WithManualClassID(StaticClassIndices.CEnvParticleScript);

	public float SequenceScale;
}
