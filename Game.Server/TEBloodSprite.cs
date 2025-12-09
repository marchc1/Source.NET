using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<TEBloodSprite>;
public class TEBloodSprite
{
	public static readonly SendTable DT_TEBloodSprite = new([
		SendPropVector(FIELD.OF(nameof(Origin)), 0, PropFlags.Coord),
		SendPropVector(FIELD.OF(nameof(Direction)), 0, PropFlags.Coord),
		SendPropInt(FIELD.OF(nameof(R)), 8, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(G)), 8, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(B)), 8, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(A)), 8, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(SprayModel)), 14, 0),
		SendPropInt(FIELD.OF(nameof(DropModel)), 14, 0),
		SendPropInt(FIELD.OF(nameof(Size)), 8, PropFlags.Unsigned),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass("TEBloodSprite", DT_TEBloodSprite).WithManualClassID(StaticClassIndices.CTEBloodSprite);

	public Vector3 Origin;
	public Vector3 Direction;
	public int R;
	public int G;
	public int B;
	public int A;
	public int SprayModel;
	public int DropModel;
	public int Size;
}
