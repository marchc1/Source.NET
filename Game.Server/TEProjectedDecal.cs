using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<TEProjectedDecal>;
public class TEProjectedDecal : BaseTempEntity
{
	public static readonly SendTable DT_TEProjectedDecal = new(DT_BaseTempEntity, [
		SendPropVector(FIELD.OF(nameof(Origin)), 0, PropFlags.Coord),
		SendPropVector(FIELD.OF(nameof(Rotation)), 10, PropFlags.RoundDown),
		SendPropFloat(FIELD.OF(nameof(LDistance)), 10, PropFlags.RoundUp),
		SendPropInt(FIELD.OF(nameof(Index)), 9, PropFlags.Unsigned),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass("TEProjectedDecal", DT_TEProjectedDecal).WithManualClassID(StaticClassIndices.CTEProjectedDecal);

	public Vector3 Origin;
	public Vector3 Rotation;
	public float LDistance;
	public int Index;
}
