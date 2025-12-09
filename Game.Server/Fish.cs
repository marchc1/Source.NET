using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<Fish>;
public class Fish : BaseAnimating
{
	public static readonly SendTable DT_Fish = new([
		SendPropVector(FIELD.OF(nameof(PoolOrigin)), 0, PropFlags.Coord),
		SendPropFloat(FIELD.OF(nameof(Le)), 7, 0),
		SendPropFloat(FIELD.OF(nameof(X)), 7, 0),
		SendPropFloat(FIELD.OF(nameof(Y)), 7, 0),
		SendPropFloat(FIELD.OF(nameof(Z)), 0, PropFlags.Coord | PropFlags.NoScale),
		SendPropInt(FIELD.OF(nameof(ModelIndex)), 14, 0),
		SendPropInt(FIELD.OF(nameof(LifeState)), 8, 0),
		SendPropFloat(FIELD.OF(nameof(WaterLevel)), 0, PropFlags.NoScale),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass("Fish", DT_Fish).WithManualClassID(StaticClassIndices.CFish);

	public Vector3 PoolOrigin;
	public float Le;
	public float X;
	public float Y;
	public float Z;
}
