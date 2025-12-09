using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<TEClientProjectile>;
public class TEClientProjectile : BaseTempEntity
{
	public static readonly SendTable DT_TEClientProjectile = new(DT_BaseTempEntity, [
		SendPropVector(FIELD.OF(nameof(Origin)), 0, PropFlags.Coord),
		SendPropVector(FIELD.OF(nameof(Velocity)), 0, PropFlags.Coord),
		SendPropInt(FIELD.OF(nameof(ModelIndex)), 14, 0),
		SendPropInt(FIELD.OF(nameof(LifeTime)), 6, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(HOwner)), 23, PropFlags.Unsigned),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass("TEClientProjectile", DT_TEClientProjectile).WithManualClassID(StaticClassIndices.CTEClientProjectile);

	public Vector3 Origin;
	public Vector3 Velocity;
	public int ModelIndex;
	public int LifeTime;
	public int HOwner;
}
