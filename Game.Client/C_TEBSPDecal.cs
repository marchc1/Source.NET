using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_TEBSPDecal>;
public class C_TEBSPDecal : C_BaseTempEntity
{
	public static readonly RecvTable DT_TEBSPDecal = new(DT_BaseTempEntity, [
		RecvPropVector(FIELD.OF(nameof(Origin))),
		RecvPropInt(FIELD.OF(nameof(Entity))),
		RecvPropInt(FIELD.OF(nameof(Index))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass("TEBSPDecal", DT_TEBSPDecal).WithManualClassID(StaticClassIndices.CTEBSPDecal);

	public Vector3 Origin;
	public int Entity;
	public int Index;
}
