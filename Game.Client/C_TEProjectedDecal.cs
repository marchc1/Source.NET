using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
using Source.Common.Mathematics;
namespace Game.Client;
using FIELD = FIELD<C_TEProjectedDecal>;
public class C_TEProjectedDecal : C_BaseTempEntity
{
	public static readonly RecvTable DT_TEProjectedDecal = new(DT_BaseTempEntity, [
		RecvPropVector(FIELD.OF(nameof(Origin))),
		RecvPropVector(FIELD.OF(nameof(Rotation))),
		RecvPropFloat(FIELD.OF(nameof(LDistance))),
		RecvPropInt(FIELD.OF(nameof(Index))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass("TEProjectedDecal", DT_TEProjectedDecal).WithManualClassID(StaticClassIndices.CTEProjectedDecal);

	public Vector3 Origin;
	public Vector3 Rotation;
	public float LDistance;
	public int Index;
}

public static partial class TempEnts
{
	public static void TE_ProjectDecal(IRecipientFilter filter, float delay, in Vector3 pos, in QAngle angles, float distance, int index) {
		throw new NotImplementedException();
	}
}
