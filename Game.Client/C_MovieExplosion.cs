using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;

using FIELD = FIELD<C_MovieExplosion>;
public class C_MovieExplosion : C_BaseParticleEntity
{
	public static readonly RecvTable DT_MovieExplosion = new(DT_BaseParticleEntity, []);
	public static readonly new ClientClass ClientClass = new ClientClass("MovieExplosion", DT_MovieExplosion).WithManualClassID(StaticClassIndices.MovieExplosion);
}
