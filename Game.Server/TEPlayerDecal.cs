using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<TEPlayerDecal>;
public class TEPlayerDecal : BaseTempEntity
{
	public static readonly SendTable DT_TEPlayerDecal = new(DT_BaseTempEntity, [
		SendPropVector(FIELD.OF(nameof(Origin)), 0, PropFlags.Coord),
		SendPropInt(FIELD.OF(nameof(Entity)), 13, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(Player)), 7, PropFlags.Unsigned),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass("TEPlayerDecal", DT_TEPlayerDecal).WithManualClassID(StaticClassIndices.CTEPlayerDecal);

	public Vector3 Origin;
	public int Entity;
	public int Player;
}
