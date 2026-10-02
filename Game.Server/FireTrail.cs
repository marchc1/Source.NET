using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<FireTrail>;
[LinkEntityToClass("env_fire_trail")]
[NetworkName("CFireTrail")]
public class FireTrail : BaseParticleEntity
{
	public static readonly SendTable DT_FireTrail = new(DT_BaseParticleEntity, [
		SendPropInt(FIELD.OF(nameof(Attachment)), 32, 0),
		SendPropFloat(FIELD.OF(nameof(Lifetime)), 0, PropFlags.NoScale),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_FireTrail);

	[NetworkName("m_nAttachment")]
	public int Attachment;
	[NetworkName("m_flLifetime")]
	public float Lifetime;
}
