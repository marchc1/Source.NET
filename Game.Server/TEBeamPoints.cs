using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<TEBeamPoints>;
public class TEBeamPoints : BaseBeam
{
	public static readonly SendTable DT_TEBeamPoints = new(DT_BaseBeam, [
		SendPropVector(FIELD.OF(nameof(StartPoint)), 0, PropFlags.Coord),
		SendPropVector(FIELD.OF(nameof(EndPoint)), 0, PropFlags.Coord),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass("TEBeamPoints", DT_TEBeamPoints).WithManualClassID(StaticClassIndices.CTEBeamPoints);

	public Vector3 StartPoint;
	public Vector3 EndPoint;
}
