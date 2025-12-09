using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<TEFootprintDecal>;
public class TEFootprintDecal : BaseTempEntity
{
	public static readonly SendTable DT_TEFootprintDecal = new(DT_BaseTempEntity, [
		SendPropVector(FIELD.OF(nameof(Origin)), 0, PropFlags.Coord),
		SendPropVector(FIELD.OF(nameof(Direction)), 0, PropFlags.Coord),
		SendPropInt(FIELD.OF(nameof(Entity)), 11, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(Index)), 8, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(ChMaterialType)), 8, PropFlags.Unsigned),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass("TEFootprintDecal", DT_TEFootprintDecal).WithManualClassID(StaticClassIndices.CTEFootprintDecal);

	public Vector3 Origin;
	public Vector3 Direction;
	public int Entity;
	public int Index;
	public int ChMaterialType;
}
