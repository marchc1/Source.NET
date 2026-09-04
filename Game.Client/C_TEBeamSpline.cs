using Game.Shared;

using Source;
using Source.Common;

using System.Numerics;
namespace Game.Client;

using FIELD = FIELD<C_TEBeamSpline>;


public class C_TEBeamSpline : C_BaseTempEntity
{
	public const int MAX_SPLINE_POINTS = 16;
	public static readonly RecvTable DT_TEBeamSpline = new([
		RecvPropInt(FIELD.OF(nameof(NumPoints))),
		RecvPropFloat(FIELD.OF_ARRAYINDEX(nameof(Points), 0)),
		RecvPropArray(FIELD.OF_ARRAY(nameof(Points))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass("TEBeamSpline", DT_TEBeamSpline).AsEvent<C_TEBeamSpline>().WithManualClassID(StaticClassIndices.CTEBeamSpline);

	public int NumPoints;
	public InlineArrayMaxSplinePoints<Vector3> Points;

	public override void PostDataUpdate(DataUpdateType updateType) {
		DevMsg(1, $"Beam spline with {NumPoints} points received\n");
	}
}

public static partial class TempEnts
{
	public static void TE_BeamSpline(IRecipientFilter filter, float delay, int points, Span<Vector3> rgPoints) {
		throw new NotImplementedException();
	}
}
