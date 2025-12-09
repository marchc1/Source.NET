using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_FireTrail>;
public class C_FireTrail : C_BaseParticleEntity
{
	public static readonly RecvTable DT_FireTrail = new(DT_BaseParticleEntity, [
		RecvPropInt(FIELD.OF(nameof(Attachment))),
		RecvPropFloat(FIELD.OF(nameof(Lifetime))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass("FireTrail", DT_FireTrail).WithManualClassID(StaticClassIndices.CFireTrail);

	public int Attachment;
	public float Lifetime;
}
