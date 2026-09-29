using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_TEBubbleTrail>;
[NetworkName("CTEBubbleTrail")]
public class C_TEBubbleTrail : C_BaseTempEntity
{
	public static readonly RecvTable DT_TEBubbleTrail = new(DT_BaseTempEntity, [
		RecvPropVector(FIELD.OF(nameof(Mins))),
		RecvPropVector(FIELD.OF(nameof(Maxs))),
		RecvPropInt(FIELD.OF(nameof(ModelIndex))),
		RecvPropFloat(FIELD.OF(nameof(LWaterZ))),
		RecvPropInt(FIELD.OF(nameof(Count))),
		RecvPropFloat(FIELD.OF(nameof(Speed))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass(DT_TEBubbleTrail);

	[NetworkName("m_vecMins")]
	public Vector3 Mins;
	[NetworkName("m_vecMaxs")]
	public Vector3 Maxs;
	[NetworkName("m_nModelIndex")]
	public int ModelIndex;
	[NetworkName("m_flWaterZ")]
	public float LWaterZ;
	[NetworkName("m_nCount")]
	public int Count;
	[NetworkName("m_fSpeed")]
	public float Speed;
}

public static partial class TempEnts
{
	public static void TE_BubbleTrail(IRecipientFilter filter, float delay, in Vector3 mins, in Vector3 maxs, float waterZ, int modelIndex, int count, float speed) {
		throw new NotImplementedException();
	}
}
