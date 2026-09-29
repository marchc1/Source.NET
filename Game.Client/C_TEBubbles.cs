using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_TEBubbles>;
[NetworkName("CTEBubbles")]
public class C_TEBubbles : C_BaseTempEntity
{
	public static readonly RecvTable DT_TEBubbles = new(DT_BaseTempEntity, [
		RecvPropVector(FIELD.OF(nameof(Mins))),
		RecvPropVector(FIELD.OF(nameof(Maxs))),
		RecvPropInt(FIELD.OF(nameof(ModelIndex))),
		RecvPropFloat(FIELD.OF(nameof(Height))),
		RecvPropInt(FIELD.OF(nameof(Count))),
		RecvPropFloat(FIELD.OF(nameof(Speed))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass(DT_TEBubbles);

	[NetworkName("m_vecMins")]
	public Vector3 Mins;
	[NetworkName("m_vecMaxs")]
	public Vector3 Maxs;
	[NetworkName("m_nModelIndex")]
	public int ModelIndex;
	[NetworkName("m_fHeight")]
	public float Height;
	[NetworkName("m_nCount")]
	public int Count;
	[NetworkName("m_fSpeed")]
	public float Speed;
}

public static partial class TempEnts
{
	public static void TE_Bubbles(IRecipientFilter filter, float delay, in Vector3 mins, in Vector3 maxs, float height, int modelIndex, int count, float speed) {
		throw new NotImplementedException();
	}
}
