using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<TESpriteSpray>;
public class TESpriteSpray : BaseTempEntity
{
	public static readonly SendTable DT_TESpriteSpray = new(DT_BaseTempEntity, [
		SendPropVector(FIELD.OF(nameof(Origin)), 0, PropFlags.Coord),
		SendPropVector(FIELD.OF(nameof(Direction)), 0, PropFlags.Coord),
		SendPropInt(FIELD.OF(nameof(ModelIndex)), 14, 0),
		SendPropFloat(FIELD.OF(nameof(Noise)), 8, PropFlags.RoundDown),
		SendPropInt(FIELD.OF(nameof(Speed)), 8, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(Count)), 8, PropFlags.Unsigned),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass("TESpriteSpray", DT_TESpriteSpray).WithManualClassID(StaticClassIndices.CTESpriteSpray);

	public Vector3 Origin;
	public Vector3 Direction;
	public int ModelIndex;
	public float Noise;
	public int Speed;
	public int Count;
}
