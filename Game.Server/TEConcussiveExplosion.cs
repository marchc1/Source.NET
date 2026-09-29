using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<TEConcussiveExplosion>;
[NetworkName("CTEConcussiveExplosion")]
public class TEConcussiveExplosion(ReadOnlySpan<char> name) : TEParticleSystem(name)
{
	public static readonly SendTable DT_TEConcussiveExplosion = new(DT_TEParticleSystem, [
		SendPropVector(FIELD.OF(nameof(Normal)), 0, PropFlags.Coord),
		SendPropFloat(FIELD.OF(nameof(Scale)), 0, PropFlags.NoScale),
		SendPropInt(FIELD.OF(nameof(Radius)), 32, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(Magnitude)), 32, PropFlags.Unsigned),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_TEConcussiveExplosion);

	[NetworkName("m_vecNormal")]
	public Vector3 Normal;
	[NetworkName("m_flScale")]
	public float Scale;
	[NetworkName("m_nRadius")]
	public int Radius;
	[NetworkName("m_nMagnitude")]
	public int Magnitude;
}
