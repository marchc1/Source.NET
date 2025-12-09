using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_MortarShell>;
public class C_MortarShell : C_BaseEntity
{
	public static readonly RecvTable DT_MortarShell = new(DT_BaseEntity, [
		RecvPropFloat(FIELD.OF(nameof(Lifespan))),
		RecvPropFloat(FIELD.OF(nameof(Radius))),
		RecvPropVector(FIELD.OF(nameof(SurfaceNormal))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass("MortarShell", DT_MortarShell).WithManualClassID(StaticClassIndices.CMortarShell);

	public float Lifespan;
	public float Radius;
	public Vector3 SurfaceNormal;
}
