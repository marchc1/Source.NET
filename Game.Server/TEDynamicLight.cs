using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<TEDynamicLight>;
[NetworkName("CTEDynamicLight")]
public class TEDynamicLight(ReadOnlySpan<char> name) : BaseTempEntity(name)
{
	public static readonly SendTable DT_TEDynamicLight = new(DT_BaseTempEntity, [
		SendPropVector(FIELD.OF(nameof(Origin)), 0, PropFlags.Coord),
		SendPropInt(FIELD.OF(nameof(R)), 8, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(G)), 8, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(B)), 8, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(Exponent)), 8, 0),
		SendPropFloat(FIELD.OF(nameof(Radius)), 8, PropFlags.RoundUp, 0, 2560.0f),
		SendPropFloat(FIELD.OF(nameof(Time)), 8, PropFlags.RoundDown, 0, 25.6f),
		SendPropFloat(FIELD.OF(nameof(Decay)), 8, PropFlags.RoundDown, 0, 2560.0f),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_TEDynamicLight);

	[NetworkName("m_vecOrigin")]
	public Vector3 Origin;
	[NetworkName("r")]
	public int R;
	[NetworkName("g")]
	public int G;
	[NetworkName("b")]
	public int B;
	[NetworkName("exponent")]
	public int Exponent;
	[NetworkName("m_fRadius")]
	public float Radius;
	[NetworkName("m_fTime")]
	public float Time;
	[NetworkName("m_fDecay")]
	public float Decay;
}
