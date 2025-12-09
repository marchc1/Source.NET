using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_TEWorldDecal>;
public class C_TEWorldDecal : C_BaseTempEntity
{
	public static readonly RecvTable DT_TEWorldDecal = new(DT_BaseTempEntity, [
		RecvPropVector(FIELD.OF(nameof(Origin))),
		RecvPropInt(FIELD.OF(nameof(Index))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass("TEWorldDecal", DT_TEWorldDecal).WithManualClassID(StaticClassIndices.CTEWorldDecal);

	public Vector3 Origin;
	public int Index;
}
