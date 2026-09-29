using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<TEBloodSprite>;
[NetworkName("CTEBloodSprite")]
public class TEBloodSprite
{
	public static readonly SendTable DT_TEBloodSprite = new([
		SendPropVector(FIELD.OF(nameof(Origin)), 0, PropFlags.Coord),
		SendPropVector(FIELD.OF(nameof(Direction)), 0, PropFlags.Coord),
		SendPropInt(FIELD.OF(nameof(R)), 8, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(G)), 8, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(B)), 8, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(A)), 8, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(Size)), 8, PropFlags.Unsigned),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_TEBloodSprite);

	[NetworkName("m_vecOrigin")]
	public Vector3 Origin;
	[NetworkName("m_vecDirection")]
	public Vector3 Direction;
	[NetworkName("r")]
	public int R;
	[NetworkName("g")]
	public int G;
	[NetworkName("b")]
	public int B;
	[NetworkName("a")]
	public int A;
	public int SprayModel;
	public int DropModel;
	[NetworkName("m_nSize")]
	public int Size;
}
