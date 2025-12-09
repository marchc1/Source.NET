using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<TESprite>;
public class TESprite : BaseTempEntity
{
	public static readonly SendTable DT_TESprite = new(DT_BaseTempEntity, [
		SendPropVector(FIELD.OF(nameof(Origin)), 0, PropFlags.Coord),
		SendPropInt(FIELD.OF(nameof(ModelIndex)), 14, 0),
		SendPropFloat(FIELD.OF(nameof(Scale)), 8, PropFlags.RoundDown),
		SendPropInt(FIELD.OF(nameof(Brightness)), 8, PropFlags.Unsigned),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass("TESprite", DT_TESprite).WithManualClassID(StaticClassIndices.CTESprite);

	public Vector3 Origin;
	public int ModelIndex;
	public float Scale;
	public int Brightness;
}
