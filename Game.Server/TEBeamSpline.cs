using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<TEBeamSpline>;
public class TEBeamSpline : BaseTempEntity
{
	public static readonly SendTable DT_TEBeamSpline = new([
		SendPropInt(FIELD.OF(nameof(NumPoints)), 5, PropFlags.Unsigned),
		SendPropFloat(FIELD.OF_ARRAYINDEX(nameof(Points), 0), 8, 0, 0.0f, 1.0f),
		SendPropArray(FIELD.OF_ARRAY(nameof(Points))),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass("TEBeamSpline", DT_TEBeamSpline).WithManualClassID(StaticClassIndices.CTEBeamSpline);

	public int NumPoints;
	public InlineArrayMaxSplinePoints<Vector3> Points;
}
