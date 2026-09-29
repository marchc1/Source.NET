using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<TEGlowSprite>;
[NetworkName("CTEGlowSprite")]
public class TEGlowSprite(ReadOnlySpan<char> name) : BaseTempEntity(name)
{
	public static readonly SendTable DT_TEGlowSprite = new(DT_BaseTempEntity, [
		SendPropVector(FIELD.OF(nameof(Origin)), 0, PropFlags.Coord),
		SendPropInt(FIELD.OF(nameof(ModelIndex)), 14, 0),
		SendPropFloat(FIELD.OF(nameof(Scale)), 8, PropFlags.RoundDown, 0.0f, 25.6f),
		SendPropFloat(FIELD.OF(nameof(Life)), 8, PropFlags.RoundDown, 0.0f, 25.6f),
		SendPropInt(FIELD.OF(nameof(Brightness)), 8, PropFlags.Unsigned),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_TEGlowSprite);

	[NetworkName("m_vecOrigin")]
	public Vector3 Origin;
	[NetworkName("m_nModelIndex")]
	public int ModelIndex;
	[NetworkName("m_fScale")]
	public float Scale;
	[NetworkName("m_fLife")]
	public float Life;
	[NetworkName("m_nBrightness")]
	public int Brightness;
}
