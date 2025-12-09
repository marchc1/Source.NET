using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;

using FIELD = FIELD<MovieExplosion>;
public class MovieExplosion : BaseParticleEntity
{
	public static readonly SendTable DT_MovieExplosion = new(DT_BaseParticleEntity, []);
	public static readonly new ServerClass ServerClass = new ServerClass("MovieExplosion", DT_MovieExplosion).WithManualClassID(StaticClassIndices.MovieExplosion);
}
