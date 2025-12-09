using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<FireTrail>;
public class FireTrail : BaseParticleEntity
{
	public static readonly SendTable DT_FireTrail = new(DT_BaseParticleEntity, [
		SendPropInt(FIELD.OF(nameof(Attachment)), 32, 0),
		SendPropFloat(FIELD.OF(nameof(Lifetime)), 0, PropFlags.NoScale),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass("FireTrail", DT_FireTrail).WithManualClassID(StaticClassIndices.CFireTrail);

	public int Attachment;
	public float Lifetime;
}
