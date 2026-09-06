using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_TEFootprintDecal>;
public class C_TEFootprintDecal : C_BaseTempEntity
{
	public static readonly RecvTable DT_TEFootprintDecal = new(DT_BaseTempEntity, [
		RecvPropVector(FIELD.OF(nameof(Origin))),
		RecvPropVector(FIELD.OF(nameof(Direction))),
		RecvPropInt(FIELD.OF(nameof(Entity))),
		RecvPropInt(FIELD.OF(nameof(Index))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass("TEFootprintDecal", DT_TEFootprintDecal).WithManualClassID(StaticClassIndices.CTEFootprintDecal);

	public Vector3 Origin;
	public Vector3 Direction;
	public int Entity;
	public int Index;
	public int ChMaterialType;
}

public static partial class TempEnts
{
	public static void TE_FootprintDecal(IRecipientFilter filter, float delay, in Vector3 origin, in Vector3 right, int entity, int index, byte materialType) {
		throw new NotImplementedException();
	}
}
