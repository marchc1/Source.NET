using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_TEMetalSparks>;
public class C_TEMetalSparks : C_BaseTempEntity
{
	public static readonly RecvTable DT_TEMetalSparks = new([
		RecvPropVector(FIELD.OF(nameof(Pos))),
		RecvPropVector(FIELD.OF(nameof(Dir))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass("TEMetalSparks", DT_TEMetalSparks).WithManualClassID(StaticClassIndices.CTEMetalSparks);

	public Vector3 Pos;
	public Vector3 Dir;
}
