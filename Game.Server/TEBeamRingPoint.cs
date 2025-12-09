using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<TEBeamRingPoint>;
public class TEBeamRingPoint : BaseBeam
{
	public static readonly SendTable DT_TEBeamRingPoint = new(DT_BaseBeam, [
		SendPropVector(FIELD.OF(nameof(Center)), 0, PropFlags.Coord),
		SendPropFloat(FIELD.OF(nameof(LStartRadius)), 16, PropFlags.RoundUp),
		SendPropFloat(FIELD.OF(nameof(LEndRadius)), 16, PropFlags.RoundUp),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass("TEBeamRingPoint", DT_TEBeamRingPoint).WithManualClassID(StaticClassIndices.CTEBeamRingPoint);

	public Vector3 Center;
	public float LStartRadius;
	public float LEndRadius;
}
