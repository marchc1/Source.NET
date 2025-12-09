using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<TEBubbles>;
public class TEBubbles : BaseTempEntity
{
	public static readonly SendTable DT_TEBubbles = new(DT_BaseTempEntity, [
		SendPropVector(FIELD.OF(nameof(Mins)), 0, PropFlags.Coord),
		SendPropVector(FIELD.OF(nameof(Maxs)), 0, PropFlags.Coord),
		SendPropInt(FIELD.OF(nameof(ModelIndex)), 14, 0),
		SendPropFloat(FIELD.OF(nameof(Height)), 17, 0),
		SendPropInt(FIELD.OF(nameof(Count)), 8, PropFlags.Unsigned),
		SendPropFloat(FIELD.OF(nameof(Speed)), 17, 0),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass("TEBubbles", DT_TEBubbles).WithManualClassID(StaticClassIndices.CTEBubbles);

	public Vector3 Mins;
	public Vector3 Maxs;
	public int ModelIndex;
	public float Height;
	public int Count;
	public float Speed;
}
