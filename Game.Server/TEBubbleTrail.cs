using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<TEBubbleTrail>;
[NetworkName("CTEBubbleTrail")]
public class TEBubbleTrail(ReadOnlySpan<char> name) : BaseTempEntity(name)
{
	public static readonly SendTable DT_TEBubbleTrail = new(DT_BaseTempEntity, [
		SendPropVector(FIELD.OF(nameof(Mins)), 0, PropFlags.Coord),
		SendPropVector(FIELD.OF(nameof(Maxs)), 0, PropFlags.Coord),
		SendPropInt(FIELD.OF(nameof(ModelIndex)), 14, 0),
		SendPropFloat(FIELD.OF(nameof(LWaterZ)), 17, 0, MIN_COORD_INTEGER, MAX_COORD_INTEGER),
		SendPropInt(FIELD.OF(nameof(Count)), 8, PropFlags.Unsigned),
		SendPropFloat(FIELD.OF(nameof(Speed)), 17, 0, MIN_COORD_INTEGER, MAX_COORD_INTEGER),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_TEBubbleTrail);

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
