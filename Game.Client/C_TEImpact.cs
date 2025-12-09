using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_TEImpact>;
public class C_TEImpact : C_BaseTempEntity
{
	public static readonly RecvTable DT_TEImpact = new(DT_BaseTempEntity, [
		RecvPropVector(FIELD.OF(nameof(Origin))),
		RecvPropVector(FIELD.OF(nameof(Normal))),
		RecvPropInt(FIELD.OF(nameof(Type))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass("TEImpact", DT_TEImpact).WithManualClassID(StaticClassIndices.CTEImpact);

	public Vector3 Origin;
	public Vector3 Normal;
	public int Type;
}
