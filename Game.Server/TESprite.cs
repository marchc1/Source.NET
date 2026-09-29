using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<TESprite>;
[NetworkName("CTESprite")]
public class TESprite(ReadOnlySpan<char> name) : BaseTempEntity(name)
{
	public static readonly SendTable DT_TESprite = new(DT_BaseTempEntity, [
		SendPropVector(FIELD.OF(nameof(Origin)), 0, PropFlags.Coord),
		SendPropInt(FIELD.OF(nameof(ModelIndex)), 14, 0),
		SendPropFloat(FIELD.OF(nameof(Scale)), 8, PropFlags.RoundDown, 0.0f, 25.6f),
		SendPropInt(FIELD.OF(nameof(Brightness)), 8, PropFlags.Unsigned),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_TESprite);

	[NetworkName("m_vecOrigin")]
	public Vector3 Origin;
	[NetworkName("m_nModelIndex")]
	public int ModelIndex;
	[NetworkName("m_fScale")]
	public float Scale;
	[NetworkName("m_nBrightness")]
	public int Brightness;
}
