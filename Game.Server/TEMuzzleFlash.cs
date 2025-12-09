using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<TEMuzzleFlash>;
public class TEMuzzleFlash : BaseTempEntity
{
	public static readonly SendTable DT_TEMuzzleFlash = new(DT_BaseTempEntity, [
		SendPropVector(FIELD.OF(nameof(Origin)), 0, PropFlags.Coord),
		SendPropVector(FIELD.OF(nameof(Angles)), 0, PropFlags.Coord),
		SendPropFloat(FIELD.OF(nameof(Scale)), 0, PropFlags.NoScale),
		SendPropInt(FIELD.OF(nameof(Type)), 32, PropFlags.Unsigned),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass("TEMuzzleFlash", DT_TEMuzzleFlash).WithManualClassID(StaticClassIndices.CTEMuzzleFlash);

	public Vector3 Origin;
	public Vector3 Angles;
	public float Scale;
	public int Type;
}
