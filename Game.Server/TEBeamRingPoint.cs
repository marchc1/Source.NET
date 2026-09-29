using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<TEBeamRingPoint>;
[NetworkName("CTEBeamRingPoint")]
public class TEBeamRingPoint(ReadOnlySpan<char> name) : BaseBeam(name)
{
	public static readonly SendTable DT_TEBeamRingPoint = new(DT_BaseBeam, [
		SendPropVector(FIELD.OF(nameof(Center)), 0, PropFlags.Coord),
		SendPropFloat(FIELD.OF(nameof(LStartRadius)), 16, PropFlags.RoundUp, 0.0f, 4096.0f),
		SendPropFloat(FIELD.OF(nameof(LEndRadius)), 16, PropFlags.RoundUp, 0.0f, 4096.0f),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_TEBeamRingPoint);

	[NetworkName("m_vecCenter")]
	public Vector3 Center;
	[NetworkName("m_flStartRadius")]
	public float LStartRadius;
	[NetworkName("m_flEndRadius")]
	public float LEndRadius;
}
