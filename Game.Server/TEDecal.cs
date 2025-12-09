using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<TEDecal>;
public class TEDecal : BaseTempEntity
{
	public static readonly SendTable DT_TEDecal = new(DT_BaseTempEntity, [
		SendPropVector(FIELD.OF(nameof(Origin)), 0, PropFlags.Coord),
		SendPropVector(FIELD.OF(nameof(Start)), 0, PropFlags.Coord),
		SendPropInt(FIELD.OF(nameof(Entity)), 13, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(Hitbox)), 12, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(Index)), 9, PropFlags.Unsigned),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass("TEDecal", DT_TEDecal).WithManualClassID(StaticClassIndices.CTEDecal);

	public Vector3 Origin;
	public Vector3 Start;
	public int Entity;
	public int Hitbox;
	public int Index;
}
