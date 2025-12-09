using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<ParticleFire>;
public class ParticleFire : BaseParticleEntity
{
	public static readonly SendTable DT_ParticleFire = new([
		SendPropVector(FIELD.OF(nameof(Origin)), 0, PropFlags.Coord),
		SendPropVector(FIELD.OF(nameof(Direction)), 0, PropFlags.NoScale),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass("ParticleFire", DT_ParticleFire).WithManualClassID(StaticClassIndices.CParticleFire);

	public new Vector3 Origin;
	public Vector3 Direction;
}
