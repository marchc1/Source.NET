using Game.Shared;

using Source;
using Source.Common;
using Source.Common.Mathematics;

using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<TEBreakModel>;
public class TEBreakModel : BaseTempEntity
{
	public static readonly SendTable DT_TEBreakModel = new(DT_BaseTempEntity, [
		SendPropVector(FIELD.OF(nameof(Origin)), 0, PropFlags.Coord),
		SendPropAngle(FIELD.OF_VECTORELEM(nameof(Rotation), 0), 13, PropFlags.RoundDown),
		SendPropAngle(FIELD.OF_VECTORELEM(nameof(Rotation), 1), 13, PropFlags.RoundDown),
		SendPropAngle(FIELD.OF_VECTORELEM(nameof(Rotation), 2), 13, PropFlags.RoundDown),
		SendPropVector(FIELD.OF(nameof(Size)), 0, PropFlags.Coord),
		SendPropVector(FIELD.OF(nameof(Velocity)), 0, PropFlags.Coord),
		SendPropInt(FIELD.OF(nameof(ModelIndex)), 14, 0),
		SendPropInt(FIELD.OF(nameof(Randomization)), 9, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(Count)), 8, PropFlags.Unsigned),
		SendPropFloat(FIELD.OF(nameof(Time)), 10, 0),
		SendPropInt(FIELD.OF(nameof(Flags)), 8, PropFlags.Unsigned),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass("TEBreakModel", DT_TEBreakModel).WithManualClassID(StaticClassIndices.CTEBreakModel);

	public Vector3 Origin;
	public QAngle Rotation;
	public Vector3 Size;
	public Vector3 Velocity;
	public int ModelIndex;
	public int Randomization;
	public int Count;
	public float Time;
	public int Flags;
}
