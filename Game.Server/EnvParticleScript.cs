using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<EnvParticleScript>;
public class EnvParticleScript : BaseAnimating
{
	public static readonly SendTable DT_EnvParticleScript = new(DT_BaseAnimating, [
		SendPropFloat(FIELD.OF(nameof(SequenceScale)), 0, PropFlags.NoScale),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass("EnvParticleScript", DT_EnvParticleScript).WithManualClassID(StaticClassIndices.CEnvParticleScript);

	public float SequenceScale;
}
