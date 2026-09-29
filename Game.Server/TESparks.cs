using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<TESparks>;
[NetworkName("CTESparks")]
public class TESparks(ReadOnlySpan<char> name) : TEParticleSystem(name)
{
	public static readonly SendTable DT_TESparks = new(DT_TEParticleSystem, [
		SendPropInt(FIELD.OF(nameof(Magnitude)), 4, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(TrailLength)), 4, PropFlags.Unsigned),
		SendPropVector(FIELD.OF(nameof(Dir)), 0, PropFlags.Coord),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_TESparks);

	[NetworkName("m_nMagnitude")]
	public int Magnitude;
	[NetworkName("m_nTrailLength")]
	public int TrailLength;
	[NetworkName("m_vecDir")]
	public Vector3 Dir;
}
