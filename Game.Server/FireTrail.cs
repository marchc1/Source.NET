using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<FireTrail>;
[NetworkName("CFireTrail")]
public partial class FireTrail : BaseParticleEntity
{
	public static readonly SendTable DT_FireTrail = new(DT_BaseParticleEntity, [
		SendPropInt(NetworkVarFields.Attachment, 32, 0),
		SendPropFloat(NetworkVarFields.Lifetime, 0, PropFlags.NoScale),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_FireTrail);

	[NetworkName("m_nAttachment")]
	[NetworkVar] public partial int Attachment { get; set; }
	[NetworkName("m_flLifetime")]
	[NetworkVar] public partial float Lifetime { get; set; }
}
