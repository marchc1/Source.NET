using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<TEBeamEntPoint>;
public class TEBeamEntPoint : BaseBeam
{
	public static readonly SendTable DT_TEBeamEntPoint = new(DT_BaseBeam, [
		SendPropInt(FIELD.OF(nameof(StartEntity)), 24, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(EndEntity)), 24, PropFlags.Unsigned),
		SendPropVector(FIELD.OF(nameof(StartPoint)), 0, PropFlags.Coord),
		SendPropVector(FIELD.OF(nameof(EndPoint)), 0, PropFlags.Coord),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass("TEBeamEntPoint", DT_TEBeamEntPoint).WithManualClassID(StaticClassIndices.CTEBeamEntPoint);

	public int StartEntity;
	public int EndEntity;
	public Vector3 StartPoint;
	public Vector3 EndPoint;
}
