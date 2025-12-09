using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_TEClientProjectile>;
public class C_TEClientProjectile : C_BaseTempEntity
{
	public static readonly RecvTable DT_TEClientProjectile = new(DT_BaseTempEntity, [
		RecvPropVector(FIELD.OF(nameof(Origin))),
		RecvPropVector(FIELD.OF(nameof(Velocity))),
		RecvPropInt(FIELD.OF(nameof(ModelIndex))),
		RecvPropInt(FIELD.OF(nameof(LifeTime))),
		RecvPropInt(FIELD.OF(nameof(HOwner))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass("TEClientProjectile", DT_TEClientProjectile).WithManualClassID(StaticClassIndices.CTEClientProjectile);

	public Vector3 Origin;
	public Vector3 Velocity;
	public int ModelIndex;
	public int LifeTime;
	public int HOwner;
}
