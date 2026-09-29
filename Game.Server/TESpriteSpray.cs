using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<TESpriteSpray>;
[NetworkName("CTESpriteSpray")]
public class TESpriteSpray(ReadOnlySpan<char> name) : BaseTempEntity(name)
{
	public static readonly SendTable DT_TESpriteSpray = new(DT_BaseTempEntity, [
		SendPropVector(FIELD.OF(nameof(Origin)), 0, PropFlags.Coord),
		SendPropVector(FIELD.OF(nameof(Direction)), 0, PropFlags.Coord),
		SendPropInt(FIELD.OF(nameof(ModelIndex)), 14, 0),
		SendPropFloat(FIELD.OF(nameof(Noise)), 8, PropFlags.RoundDown, 0.0f, 2.56f),
		SendPropInt(FIELD.OF(nameof(Speed)), 8, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(Count)), 8, PropFlags.Unsigned),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_TESpriteSpray);

	[NetworkName("m_vecOrigin")]
	public Vector3 Origin;
	[NetworkName("m_vecDirection")]
	public Vector3 Direction;
	[NetworkName("m_nModelIndex")]
	public int ModelIndex;
	[NetworkName("m_fNoise")]
	public float Noise;
	[NetworkName("m_nSpeed")]
	public int Speed;
	[NetworkName("m_nCount")]
	public int Count;
}
