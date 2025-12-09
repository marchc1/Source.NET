using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<TEBloodStream>;
public class TEBloodStream : TEParticleSystem
{
	public static readonly SendTable DT_TEBloodStream = new(DT_TEParticleSystem, [
		SendPropVector(FIELD.OF(nameof(Direction)), 11, 0),
		SendPropInt(FIELD.OF(nameof(R)), 8, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(G)), 8, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(B)), 8, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(A)), 8, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(Amount)), 8, PropFlags.Unsigned),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass("TEBloodStream", DT_TEBloodStream).WithManualClassID(StaticClassIndices.CTEBloodStream);

	public Vector3 Direction;
	public int R;
	public int G;
	public int B;
	public int A;
	public int Amount;
}
