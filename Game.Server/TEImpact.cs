using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<TEImpact>;
public class TEImpact : BaseTempEntity
{
	public static readonly SendTable DT_TEImpact = new(DT_BaseTempEntity, [
		SendPropVector(FIELD.OF(nameof(Origin)), 0, PropFlags.Coord),
		SendPropVector(FIELD.OF(nameof(Normal)), 0, PropFlags.Coord),
		SendPropInt(FIELD.OF(nameof(Type)), 32, PropFlags.Unsigned),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass("TEImpact", DT_TEImpact).WithManualClassID(StaticClassIndices.CTEImpact);

	public Vector3 Origin;
	public Vector3 Normal;
	public int Type;
}
