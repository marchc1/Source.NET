using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<TEDust>;
[NetworkName("CTEDust")]
public class TEDust(ReadOnlySpan<char> name) : TEParticleSystem(name)
{
	public static readonly SendTable DT_TEDust = new(DT_TEParticleSystem, [
		SendPropFloat(FIELD.OF(nameof(LSize)), 0, PropFlags.Coord | PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(LSpeed)), 0, PropFlags.Coord | PropFlags.NoScale),
		SendPropVector(FIELD.OF(nameof(Direction)), 4, 0, -1.0f, 1.0f),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_TEDust);

	[NetworkName("m_flSize")]
	public float LSize;
	[NetworkName("m_flSpeed")]
	public float LSpeed;
	[NetworkName("m_vecDirection")]
	public Vector3 Direction;
}
