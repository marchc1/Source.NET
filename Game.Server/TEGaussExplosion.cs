using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<TEGaussExplosion>;
public class TEGaussExplosion : TEParticleSystem
{
	public static readonly SendTable DT_TEGaussExplosion = new(DT_TEParticleSystem, [
		SendPropInt(FIELD.OF(nameof(Type)), 2, PropFlags.Unsigned),
		SendPropVector(FIELD.OF(nameof(Direction)), 0, PropFlags.Coord),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass("TEGaussExplosion", DT_TEGaussExplosion).WithManualClassID(StaticClassIndices.CTEGaussExplosion);

	public int Type;
	public Vector3 Direction;
}
