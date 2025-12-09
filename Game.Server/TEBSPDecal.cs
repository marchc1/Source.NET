using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<TEBSPDecal>;
public class TEBSPDecal : BaseTempEntity
{
	public static readonly SendTable DT_TEBSPDecal = new(DT_BaseTempEntity, [
		SendPropVector(FIELD.OF(nameof(Origin)), 0, PropFlags.Coord),
		SendPropInt(FIELD.OF(nameof(Entity)), 13, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(Index)), 9, PropFlags.Unsigned),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass("TEBSPDecal", DT_TEBSPDecal).WithManualClassID(StaticClassIndices.CTEBSPDecal);

	public Vector3 Origin;
	public int Entity;
	public int Index;
}
