using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<TEEnergySplash>;
public class TEEnergySplash : BaseTempEntity
{
	public static readonly SendTable DT_TEEnergySplash = new([
		SendPropVector(FIELD.OF(nameof(Pos)), 0, PropFlags.Coord),
		SendPropVector(FIELD.OF(nameof(Dir)), 0, PropFlags.Coord),
		SendPropInt(FIELD.OF(nameof(Explosive)), 1, PropFlags.Unsigned),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass("TEEnergySplash", DT_TEEnergySplash).WithManualClassID(StaticClassIndices.CTEEnergySplash);

	public Vector3 Pos;
	public Vector3 Dir;
	public int Explosive;
}
