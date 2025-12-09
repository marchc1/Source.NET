using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_TEMuzzleFlash>;
public class C_TEMuzzleFlash : C_BaseTempEntity
{
	public static readonly RecvTable DT_TEMuzzleFlash = new(DT_BaseTempEntity, [
		RecvPropVector(FIELD.OF(nameof(Origin))),
		RecvPropVector(FIELD.OF(nameof(Angles))),
		RecvPropFloat(FIELD.OF(nameof(LScale))),
		RecvPropInt(FIELD.OF(nameof(Type))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass("TEMuzzleFlash", DT_TEMuzzleFlash).WithManualClassID(StaticClassIndices.CTEMuzzleFlash);

	public Vector3 Origin;
	public Vector3 Angles;
	public float LScale;
	public int Type;
}
