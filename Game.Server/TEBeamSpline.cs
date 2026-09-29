using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<TEBeamSpline>;
[NetworkName("CTEBeamSpline")]
public class TEBeamSpline(ReadOnlySpan<char> name) : BaseTempEntity(name)
{
	public static readonly SendTable DT_TEBeamSpline = new([
		SendPropInt(FIELD.OF(nameof(NumPoints)), 5, PropFlags.Unsigned),
		SendPropVector(FIELD.OF_SENDINFO_ARRAY(nameof(Points)), -1, PropFlags.Coord),
		SendPropArray(FIELD.OF_ARRAY(nameof(Points))),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_TEBeamSpline);

	[NetworkName("m_nPoints")]
	public int NumPoints;
	[NetworkName("m_vecPoints")]
	public InlineArrayMaxSplinePoints<Vector3> Points;
}
