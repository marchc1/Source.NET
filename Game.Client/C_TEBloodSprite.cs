using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_TEBloodSprite>;
public class C_TEBloodSprite
{
	public static readonly RecvTable DT_TEBloodSprite = new([
		RecvPropVector(FIELD.OF(nameof(Origin))),
		RecvPropVector(FIELD.OF(nameof(Direction))),
		RecvPropInt(FIELD.OF(nameof(R))),
		RecvPropInt(FIELD.OF(nameof(G))),
		RecvPropInt(FIELD.OF(nameof(B))),
		RecvPropInt(FIELD.OF(nameof(A))),
		RecvPropInt(FIELD.OF(nameof(SprayModel))),
		RecvPropInt(FIELD.OF(nameof(DropModel))),
		RecvPropInt(FIELD.OF(nameof(Size))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass("TEBloodSprite", DT_TEBloodSprite).WithManualClassID(StaticClassIndices.CTEBloodSprite);

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
