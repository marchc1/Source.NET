using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_Fish>;
public class C_Fish : C_BaseAnimating
{
	public static readonly RecvTable DT_Fish = new([
		RecvPropVector(FIELD.OF(nameof(PoolOrigin))),
		RecvPropFloat(FIELD.OF(nameof(Le))),
		RecvPropFloat(FIELD.OF(nameof(X))),
		RecvPropFloat(FIELD.OF(nameof(Y))),
		RecvPropFloat(FIELD.OF(nameof(Z))),
		RecvPropInt(FIELD.OF(nameof(ModelIndex))),
		RecvPropInt(FIELD.OF(nameof(LifeState))),
		RecvPropFloat(FIELD.OF(nameof(WaterLevel))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass("Fish", DT_Fish).WithManualClassID(StaticClassIndices.CFish);

	public Vector3 PoolOrigin;
	public float Le;
	public float X;
	public float Y;
	public float Z;
}
