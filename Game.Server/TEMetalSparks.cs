using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<TEMetalSparks>;
public class TEMetalSparks : BaseTempEntity
{
	public static readonly SendTable DT_TEMetalSparks = new([
		SendPropVector(FIELD.OF(nameof(Pos)), 0, PropFlags.Coord),
		SendPropVector(FIELD.OF(nameof(Dir)), 0, PropFlags.Coord),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass("TEMetalSparks", DT_TEMetalSparks).WithManualClassID(StaticClassIndices.CTEMetalSparks);

	public Vector3 Pos;
	public Vector3 Dir;
}
