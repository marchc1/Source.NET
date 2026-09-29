using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<TEBloodStream>;
[NetworkName("CTEBloodStream")]
public class TEBloodStream(ReadOnlySpan<char> name) : TEParticleSystem(name)
{
	public static readonly SendTable DT_TEBloodStream = new(DT_TEParticleSystem, [
		SendPropVector(FIELD.OF(nameof(Direction)), 11, 0, -10.0f, 10.0f),
		SendPropInt(FIELD.OF(nameof(R)), 8, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(G)), 8, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(B)), 8, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(A)), 8, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(Amount)), 8, PropFlags.Unsigned),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_TEBloodStream);

	[NetworkName("m_vecDirection")]
	public Vector3 Direction;
	[NetworkName("r")]
	public int R;
	[NetworkName("g")]
	public int G;
	[NetworkName("b")]
	public int B;
	[NetworkName("a")]
	public int A;
	[NetworkName("m_nAmount")]
	public int Amount;
}
