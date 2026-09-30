using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<EnvParticleScript>;
[NetworkName("CEnvParticleScript")]
public partial class EnvParticleScript : BaseAnimating
{
	public static readonly SendTable DT_EnvParticleScript = new(DT_BaseAnimating, [
		SendPropFloat(NetworkVarFields.SequenceScale, 0, PropFlags.NoScale),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_EnvParticleScript);

	[NetworkName("m_flSequenceScale")]
	[NetworkVar] public partial float SequenceScale { get; set; }
}
