using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<TEGaussExplosion>;
[NetworkName("CTEGaussExplosion")]
public class TEGaussExplosion(ReadOnlySpan<char> name) : TEParticleSystem(name)
{
	public static readonly SendTable DT_TEGaussExplosion = new(DT_TEParticleSystem, [
		SendPropInt(FIELD.OF(nameof(Type)), 2, PropFlags.Unsigned),
		SendPropVector(FIELD.OF(nameof(Direction)), 0, PropFlags.Coord),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_TEGaussExplosion);

	[NetworkName("m_nType")]
	public int Type;
	[NetworkName("m_vecDirection")]
	public Vector3 Direction;
}
