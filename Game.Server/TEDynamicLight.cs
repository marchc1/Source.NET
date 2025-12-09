using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<TEDynamicLight>;
public class TEDynamicLight : BaseTempEntity
{
	public static readonly SendTable DT_TEDynamicLight = new(DT_BaseTempEntity, [
		SendPropVector(FIELD.OF(nameof(Origin)), 0, PropFlags.Coord),
		SendPropInt(FIELD.OF(nameof(R)), 8, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(G)), 8, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(B)), 8, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(Exponent)), 8, 0),
		SendPropFloat(FIELD.OF(nameof(Radius)), 8, PropFlags.RoundUp),
		SendPropFloat(FIELD.OF(nameof(Time)), 8, PropFlags.RoundDown),
		SendPropFloat(FIELD.OF(nameof(Decay)), 8, PropFlags.RoundDown),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass("TEDynamicLight", DT_TEDynamicLight).WithManualClassID(StaticClassIndices.CTEDynamicLight);

	public Vector3 Origin;
	public int R;
	public int G;
	public int B;
	public int Exponent;
	public float Radius;
	public float Time;
	public float Decay;
}
