using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_ParticleFire>;
public class C_ParticleFire : C_BaseParticleEntity
{
	public static readonly RecvTable DT_ParticleFire = new([
		RecvPropVector(FIELD.OF(nameof(Origin))),
		RecvPropVector(FIELD.OF(nameof(Direction))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass("ParticleFire", DT_ParticleFire).WithManualClassID(StaticClassIndices.CParticleFire);

	public new Vector3 Origin;
	public Vector3 Direction;
}
